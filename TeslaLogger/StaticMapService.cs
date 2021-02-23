using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    public class StaticMapService
    {
        public enum StaticMapType
        {
            Trip,
            Charge,
            Park
        }

        public enum StaticMapMode
        {
            Regular,
            Dark
        }

        private enum StaticMapIcon
        {
            Start,
            End,
            Park,
            Charge
        }

        private static Random random = new Random();

        private static StaticMapService _StaticMapService = null;

        private readonly ConcurrentQueue<Tuple<int, int, StaticMapType, StaticMapMode>> queue = new ConcurrentQueue<Tuple<int, int, StaticMapType, StaticMapMode>>();

        private StaticMapService()
        {
            Logfile.Log("StaticMapService initialized");
            Enqueue(new Tuple<int, int, StaticMapType, StaticMapMode>(465292, 471276, StaticMapType.Trip, StaticMapMode.Regular));
        }

        public static StaticMapService GetSingleton()
        {
            if (_StaticMapService == null)
            {
                _StaticMapService = new StaticMapService();
            }
            return _StaticMapService;
        }

        public int GetQueueLength()
        {
            return queue.Count;
        }

        public void Enqueue(Tuple<int, int, StaticMapType, StaticMapMode> request)
        {
            queue.Enqueue(request);
        }

        public void Run()
        {
            Tools.DebugLog("StaticMapService:Run()");
            try
            {
                while (true)
                {
                    if (!queue.IsEmpty)
                    {
                        Work();
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog("StaticMapService: Exception", ex);
            }
        }

        private void Work()
        {
            Tools.DebugLog("StaticMapService:Work() queue:" + queue.Count);
            if (queue.TryDequeue(out Tuple<int, int, StaticMapType, StaticMapMode> request))
            {
                Tools.DebugLog("StaticMapService:Work() request:" + request.Item3);
                int padding_x = 8;
                int padding_y = 8;
                int width = 240;
                int height = (int)(width / 1.618033);
                int tileSize = 256;
                List<Tuple<double, double>> coords = TripToCoords(request);
                if (coords.Count == 1 && (request.Item3 == StaticMapType.Park || request.Item3 == StaticMapType.Charge))
                {
                    // park or charge
                    int zoom = 19; // max zoom
                }
                else if (request.Item3 == StaticMapType.Trip && coords.Count > 1)
                {
                    // trip
                    int zoom = CalculateZoom(coords, padding_x, padding_y, width, height, tileSize);
                    Tuple<double, double, double, double> extent = DetermineExtent(coords);
                    // calculate center point of map
                    double lat_center = (extent.Item1 + extent.Item3) / 2;
                    double lng_center = (extent.Item2 + extent.Item4) / 2;
                    double x_center = LonToTileX(lng_center, zoom);
                    double y_center = LatToTileY(lat_center, zoom);
                    Tools.DebugLog($"StaticMapService:Work() zoom:{zoom} extent:{extent} lat_center:{lat_center} lng_center:{lng_center} x_center:{x_center} y_center:{y_center} width:{width} height:{height}");
                    Tools.DebugLog($"extent {extent.Item2} LonToX:{LonToTileX(extent.Item2, zoom)} intLonToX:{(int)LonToTileX(extent.Item2, zoom)} XtoPx:{XtoPx((int)LonToTileX(extent.Item2, zoom), x_center, tileSize, width)}");
                    using (Bitmap image = new Bitmap(width, height))
                    {
                        DrawMapLayer(image, width, height, x_center, y_center, tileSize, zoom);
                        if (request.Item4 == StaticMapMode.Dark)
                        {
                            ApplyDarkMode(image);
                        }
                        DrawTrip(image, coords, zoom, x_center, y_center, tileSize);
                        image.Save("/var/www/html/map.png");
                    }
                }
                else
                {
                    Tools.DebugLog("StaticMapService:Work() request unknown type: " + request.Item3);
                }
            }
        }

        private void ApplyDarkMode(Bitmap image)
        {
            AdjustBrightness(image, 0.6f);
            InvertImage(image);
            AdjustContrast(image, 1.3f);
            HueRotate(image, -170);
            AdjustSaturation(image, 0.3f);
            AdjustBrightness(image, 0.7f);
            AdjustContrast(image, 1.3f);
        }

        // https://web.archive.org/web/20140825114946/http://bobpowell.net/image_contrast.aspx
        private void AdjustContrast(Bitmap image, float contrast)
        {
            using (ImageAttributes ia = new ImageAttributes())
            {
                //create the scaling matrix
                ColorMatrix cm = new ColorMatrix(new float[][]
                {
                new float[]{contrast, 0f,0f,0f,0f},
                new float[]{0f, contrast, 0f,0f,0f},
                new float[]{0f,0f, contrast, 0f,0f},
                new float[]{0f,0f,0f,1f,0f},
                new float[]{0.001f,0.001f,0.001f,0f,1f}
                });
                ia.SetColorMatrix(cm);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ia);
                }
            }
        }

        // https://github.com/madebits/msnet-colormatrix-hue-saturation/blob/master/C%23/QColorMatrix.cs
        private void AdjustSaturation(Bitmap image, float saturation)
        {
            float satCompl = 1.0f - saturation;
            float satComplR = 0.3086f * satCompl;
            float satComplG = 0.6094f * satCompl;
            float satComplB = 0.0820f * satCompl;

            ColorMatrix cm = new ColorMatrix(new float[][]
            {
                new float[] { satComplR + saturation, satComplR, satComplR, 0, 0 },
                  new float[] { satComplG, satComplG + saturation, satComplG, 0, 0},
                  new float[] { satComplB, satComplB, satComplB + saturation, 0, 0},
                  new float[] {0, 0, 0, 1, 0},
                  new float[] {0, 0, 0, 0, 1}
            });
            using (ImageAttributes ia = new ImageAttributes())
            {
                ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ia);
                }
            }
        }

        // https://stackoverflow.com/questions/29787258/how-do-i-rotate-hue-in-a-picturebox-image
        private void HueRotate(Bitmap image, float degrees)
        {
            double r = degrees * System.Math.PI / 180; // degrees to radians
            float[][] colorMatrixElements = {
            new float[] {(float)Math.Cos(r),  (float)Math.Sin(r),  0,  0, 0},
            new float[] {(float)-Math.Sin(r),  (float)-Math.Cos(r),  0,  0, 0},
            new float[] {0,  0,  2,  0, 0},
            new float[] {0,  0,  0,  1, 0},
            new float[] {0, 0, 0, 0, 1}};

            ColorMatrix colorMatrix = new ColorMatrix(colorMatrixElements);
            using (ImageAttributes ia = new ImageAttributes())
            {
                ia.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                using (Graphics g = Graphics.FromImage(image))
                {
                    g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ia);
                }
            }
        }

        // https://mariusbancila.ro/blog/2009/11/13/using-colormatrix-for-creating-negative-image/
        private void InvertImage(Bitmap image)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                // create the negative color matrix
                ColorMatrix colorMatrix = new ColorMatrix();
                colorMatrix.Matrix00 = colorMatrix.Matrix11 = colorMatrix.Matrix22 = -1f;
                colorMatrix.Matrix33 = colorMatrix.Matrix44 = 1f;
                // create some image attributes
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);
                g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
                0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
        }

        // http://csharphelper.com/blog/2014/10/use-an-imageattributes-object-to-adjust-an-images-brightness-in-c/
        private void AdjustBrightness(Image image, float brightness)
        {
            ColorMatrix cm = new ColorMatrix(new float[][]
            {
                new float[] {brightness, 0, 0, 0, 0},
                new float[] {0, brightness, 0, 0, 0},
                new float[] {0, 0, brightness, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1},
            });
            using (ImageAttributes ia = new ImageAttributes())
            {
                ia.SetColorMatrix(cm);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ia);
                }
            }
        }

        private void DrawTrip(Bitmap image, List<Tuple<double, double>> coords, int zoom, double x_center, double y_center, int tileSize)
        {
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // draw Trip line
                using (Pen bluePen = new Pen(Color.Blue, 2))
                {
                    using (Pen whitePen = new Pen(Color.White, 4))
                    {
                        for (int index = 1; index < coords.Count; index++)
                        {
                            int x1 = XtoPx(LonToTileX(coords[index - 1].Item2, zoom), x_center, tileSize, image.Width);
                            int y1 = YtoPx(LatToTileY(coords[index - 1].Item1, zoom), y_center, tileSize, image.Height);
                            int x2 = XtoPx(LonToTileX(coords[index].Item2, zoom), x_center, tileSize, image.Width);
                            int y2 = YtoPx(LatToTileY(coords[index].Item1, zoom), y_center, tileSize, image.Height);
                            if (x1 != x2 || y1 != y2)
                            {
                                Tools.DebugLog($"line ({x1},{y1})->({x2},{y2})");
                                graphics.DrawLine(whitePen, x1, y1, x2, y2);
                                graphics.DrawLine(bluePen, x1, y1, x2, y2);
                            }
                        }
                        DrawIcon(image, coords.First(), StaticMapIcon.Start, zoom, x_center, y_center, tileSize);
                        DrawIcon(image, coords.Last(), StaticMapIcon.End, zoom, x_center, y_center, tileSize);
                    }
                }
            }
        }

        private void DrawIcon(Bitmap image, Tuple<double, double> coord, StaticMapIcon icon, int zoom, double x_center, double y_center, int tileSize)
        {
            SolidBrush brush;
            switch (icon)
            {
                case StaticMapIcon.Charge:
                    brush = new SolidBrush(Color.Yellow);
                    break;
                case StaticMapIcon.End:
                    brush = new SolidBrush(Color.Green);
                    break;
                case StaticMapIcon.Park:
                    brush = new SolidBrush(Color.Blue);
                    break;
                case StaticMapIcon.Start:
                    brush = new SolidBrush(Color.Red);
                    break;
                default:
                    brush = new SolidBrush(Color.White);
                    break;
            }
            int x = XtoPx(LonToTileX(coord.Item2, zoom), x_center, tileSize, image.Width);
            int y = YtoPx(LatToTileY(coord.Item1, zoom), y_center, tileSize, image.Height);
            Rectangle rect = new Rectangle(x - 4, y - 10, 8, 8);
            Point[] triangle = new Point[] { new Point(x - 4, y - 6), new Point(x, y), new Point(x + 4, y - 6) };
            using (Graphics g = Graphics.FromImage(image))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen whitePen = new Pen(Color.White, 1))
                {
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.FillPie(brush, rect, 180, 180);
                    g.FillPolygon(brush, triangle);
                    g.DrawArc(whitePen, rect, 180, 180);
                    g.DrawLine(whitePen, triangle[0], triangle[1]);
                    g.DrawLine(whitePen, triangle[1], triangle[2]);
                }
            }
            brush.Dispose();
        }

        private List<Tuple<double, double>> TripToCoords(Tuple<int, int, StaticMapType, StaticMapMode> request)
        {
            List<Tuple<double, double>> coords = new List<Tuple<double, double>>();
            int CarID = int.MinValue;
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  CarID
FROM
  pos
WHERE
  id = @startID ", con))
                {
                    cmd.Parameters.AddWithValue("@startID", request.Item1);
                    Tools.DebugLog(cmd);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        int.TryParse(dr[0].ToString(), out CarID);
                    }
                }
            }
            if (CarID != int.MinValue && CarID > 0)
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  lat,
  lng
FROM
  pos
WHERE
  CarID = @CarID
  AND id >= @startID
  AND id <= @endID
ORDER BY
  Datum", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", CarID);
                        cmd.Parameters.AddWithValue("@startID", request.Item1);
                        cmd.Parameters.AddWithValue("@endID", request.Item2);
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            if (double.TryParse(dr[0].ToString(), out double lat)
                                && double.TryParse(dr[1].ToString(), out double lng))
                            {
                                coords.Add(new Tuple<double, double>(lat, lng));
                            }
                        }
                    }
                }
            }
            else
            {
                Tools.DebugLog($"TripToCoords: could not find CarID for pos {request.Item1}");
            }
            return coords;
        }

        private void DrawMapLayer(Bitmap image, int width, int height, double x_center, double y_center, int tileSize, int zoom)
        {
            Tools.DebugLog("StaticMapService:DrawMapLayer()");
            int x_min = (int)(Math.Floor(x_center - (0.5 * width / tileSize)));
            int y_min = (int)(Math.Floor(y_center - (0.5 * height / tileSize)));
            int x_max = (int)(Math.Ceiling(x_center + (0.5 * width / tileSize)));
            int y_max = (int)(Math.Ceiling(y_center + (0.5 * height / tileSize)));
            // assemble all map tiles needed for the map
            List<Tuple<int, int, Uri>> tiles = new List<Tuple<int, int, Uri>>();
            for (int x = x_min; x < x_max; x++)
            {
                for (int y = y_min; y < y_max; y++)
                {
                    Tools.DebugLog($"StaticMapService:DrawMapLayer() x:{x} y:{y}");
                    // x and y may have crossed the date line
                    int max_tile = (int)Math.Pow(2, zoom);
                    int tile_x = (x + max_tile) % max_tile;
                    int tile_y = (y + max_tile) % max_tile;
                    int num = random.Next(0, 3);
                    char abc = (char)('a' + num);
                    Uri url = new Uri($"http://{abc}.tile.osm.org/{zoom}/{tile_x}/{tile_y}.png");
                    tiles.Add(new Tuple<int, int, Uri>(x, y, url));
                }
            }
            Tools.DebugLog("StaticMapService:DrawMapLayer() tiles:" + tiles.Count);
            foreach (Tuple<int, int, Uri> tile in tiles)
            {
                using (Bitmap tileImage = DownloadTile(tile.Item3))
                {
                    Rectangle box = new Rectangle(XtoPx(tile.Item1, x_center, tileSize, width), YtoPx(tile.Item2, y_center, tileSize, height), tileSize, tileSize);
                    CopyRegionIntoImage(tileImage, new Rectangle(0, 0, tileSize, tileSize), image, box);
                }
            }
        }

        // transform tile number to pixel on image canvas
        private int YtoPx(double y, double y_center, int tileSize, int height)
        {
            double px = (y - y_center) * tileSize + height / 2.0;
            return (int)(Math.Round(px));
        }

        // transform tile number to pixel on image canvas
        private int XtoPx(double x, double x_center, int tileSize, int width)
        {
            double px = (x - x_center) * tileSize + width / 2.0;
            return (int)(Math.Round(px));
        }

        private Bitmap DownloadTile(Uri url)
        {
            Tools.DebugLog("StaticMapService:DownloadTile() url: " + url);
            using (WebClient wc = new WebClient())
            {
                wc.Headers["User-Agent"] = "TeslaLogger StaticMapService";
                using (MemoryStream ms = new MemoryStream(wc.DownloadData(url)))
                {
                    return new Bitmap(Image.FromStream(ms));
                }
            }
        }

        private void CopyRegionIntoImage(Bitmap srcBitmap, Rectangle srcRegion, Bitmap destBitmap, Rectangle destRegion)
        {
            using (Graphics grD = Graphics.FromImage(destBitmap))
            {
                grD.DrawImage(srcBitmap, destRegion, srcRegion, GraphicsUnit.Pixel);
            }
        }

        private int CalculateZoom(List<Tuple<double, double>> coords, int padding_x, int padding_y, int width, int height, int tileSize)
        {
            Tuple<double, double, double, double> extent = DetermineExtent(coords);
            for (int zoom = 17; zoom > 0; zoom--)
            {
                double _width = (LonToTileX(extent.Item3, zoom) - LonToTileX(extent.Item1, zoom)) * tileSize;
                if (_width > (width - padding_x * 2))
                {
                    continue;
                }

                double _height = (LatToTileY(extent.Item2, zoom) - LatToTileY(extent.Item4, zoom)) * tileSize;
                if (_height > (height - padding_y * 2))
                {
                    continue;
                }

                // we found first zoom that can display entire extent
                return zoom;
            }
            return 0;
        }

        // transform longitude to tile number
        private double LonToTileX(double lon, int zoom)
        {
            return ((lon + 180.0) / 360.0) * Math.Pow(2.0, zoom);
        }

        // transform latitude to tile number
        private double LatToTileY(double lat, int zoom)
        {
            return (1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) + 1 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * Math.Pow(2.0, zoom);
        }

        private Tuple<double, double, double, double> DetermineExtent(List<Tuple<double, double>> coords)
        {
            double min_lat = coords.First().Item1;
            double min_lng = coords.First().Item2;
            double max_lat = coords.First().Item1;
            double max_lng = coords.First().Item2;
            foreach (Tuple<double, double> coord in coords)
            {
                min_lat = Math.Min(min_lat, coord.Item1);
                min_lng = Math.Min(min_lng, coord.Item2);
                max_lat = Math.Max(max_lat, coord.Item1);
                max_lng = Math.Max(max_lng, coord.Item2);
            }
            Tools.DebugLog($"StaticMapService:DetermineExtent {min_lat},{min_lng} {max_lat},{max_lng}");
            return new Tuple<double, double, double, double>(min_lat, min_lng, max_lat, max_lng);
        }
    }
}
