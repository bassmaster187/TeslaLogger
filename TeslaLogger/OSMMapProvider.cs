using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;

namespace TeslaLogger
{
    public class OSMMapProvider : StaticMapProvider
    {
        private OSMMapProvider()
        {
        }

        private static Random random = new Random();
        private static int padding_x = 12;
        private static int padding_y = 12;
        private static int tileSize = 256;

        public override void CreateTripMap(DataTable coords, int width, int height, MapMode mapmode, MapSpecial special, string filename)
        {
            Tuple<double, double, double, double> extent = DetermineExtent(coords);
            // calculate center point of map
            double lat_center = (extent.Item1 + extent.Item3) / 2;
            double lng_center = (extent.Item2 + extent.Item4) / 2;
            int zoom = 19; // max zoom
            double x_center = LonToTileX(lng_center, zoom);
            double y_center = LatToTileY(lat_center, zoom);
            zoom = CalculateZoom(coords, width, height);
            x_center = LonToTileX(lng_center, zoom);
            y_center = LatToTileY(lat_center, zoom);
            using (Bitmap map = DrawMap(width, height, zoom, x_center, y_center, mapmode))
            {
                // map has background tiles, OSM attribution and dark mode, if enabled
                DrawTrip(map, coords, zoom, x_center, y_center);
                DrawIcon(map, coords.Rows[0], MapIcon.Start, zoom, x_center, y_center);
                DrawIcon(map, coords.Rows[coords.Rows.Count - 1], MapIcon.End, zoom, x_center, y_center);
            }
        }

        internal override StaticMapProvider GetInstance()
        {
            return new OSMMapProvider();
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

        private int CalculateZoom(DataTable coords, int width, int height)
        {
            Tuple<double, double, double, double> extent = DetermineExtent(coords);
            for (int zoom = 18; zoom > 0; zoom--)
            {
                double _width = (LonToTileX(extent.Item4, zoom) - LonToTileX(extent.Item2, zoom)) * tileSize;
                if (_width > (width - padding_x * 2))
                {
                    continue;
                }

                double _height = (LatToTileY(extent.Item1, zoom) - LatToTileY(extent.Item3, zoom)) * tileSize;
                if (_height > (height - padding_y * 2))
                {
                    continue;
                }

                // we found first zoom that can display entire extent
                return zoom;
            }
            return 0;
        }

        private void CopyRegionIntoImage(Bitmap srcBitmap, Rectangle srcRegion, Bitmap destBitmap, Rectangle destRegion)
        {
            using (Graphics grD = Graphics.FromImage(destBitmap))
            {
                grD.DrawImage(srcBitmap, destRegion, srcRegion, GraphicsUnit.Pixel);
            }
        }

        private Bitmap DownloadTile(Uri url)
        {
            Tools.DebugLog("DownloadTile() url: " + url);
            using (WebClient wc = new WebClient())
            {
                wc.Headers["User-Agent"] = this.GetType().ToString();
                using (MemoryStream ms = new MemoryStream(wc.DownloadData(url)))
                {
                    return new Bitmap(Image.FromStream(ms));
                }
            }
        }

        // transform tile number to pixel on image canvas
        private int YtoPx(double y, double y_center, int height)
        {
            double px = (y - y_center) * tileSize + height / 2.0;
            return (int)(Math.Round(px));
        }

        // transform tile number to pixel on image canvas
        private int XtoPx(double x, double x_center, int width)
        {
            double px = (x - x_center) * tileSize + width / 2.0;
            return (int)(Math.Round(px));
        }

        private void DrawMapLayer(Bitmap image, int width, int height, double x_center, double y_center, int zoom)
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
                    Rectangle box = new Rectangle(XtoPx(tile.Item1, x_center, width), YtoPx(tile.Item2, y_center, height), tileSize, tileSize);
                    CopyRegionIntoImage(tileImage, new Rectangle(0, 0, tileSize, tileSize), image, box);
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

        private Bitmap DrawMap(int width, int height, int zoom, double x_center, double y_center, MapMode mode)
        {
            Bitmap image = new Bitmap(width, height);
            {
                DrawMapLayer(image, width, height, x_center, y_center, zoom);
                if (mode == MapMode.Dark)
                {
                    ApplyDarkMode(image);
                }
                DrawAttribution(image);
            }
            return image;
        }

        private void DrawAttribution(Bitmap image)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                string attribution = "(C) OpenStreetMap";
                using (Font drawFont = new Font(FontFamily.GenericSansSerif, 8))
                {
                    SizeF size = g.MeasureString(attribution, drawFont);
                    using (SolidBrush fillBrush = new SolidBrush(Color.FromArgb(128, 128, 128, 128)))
                    {
                        g.FillRectangle(fillBrush, new Rectangle((int)(image.Width - size.Width - 3), (int)(image.Height - size.Height - 3), (int)(size.Width + 6), (int)(size.Height + 6)));
                        using (SolidBrush textBrush = new SolidBrush(Color.Black))
                        {
                            using (StringFormat drawFormat = new StringFormat())
                            {
                                g.DrawString(attribution, drawFont, textBrush, image.Width - size.Width - 2, image.Height - size.Height - 2);
                            }
                        }
                    }
                }
            }
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
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(colorMatrix);
                    g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
                    0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
                }
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

        private void DrawTrip(Bitmap image, DataTable coords, int zoom, double x_center, double y_center)
        {
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // draw Trip line
                using (Pen bluePen = new Pen(Color.Blue, 2))
                {
                    using (Pen whitePen = new Pen(Color.White, 4))
                    {
                        for (int index = 1; index < coords.Rows.Count; index++)
                        {
                            int x1 = XtoPx(LonToTileX((double)coords.Rows[index - 1]["lng"], zoom), x_center, image.Width);
                            int y1 = YtoPx(LatToTileY((double)coords.Rows[index - 1]["lat"], zoom), y_center, image.Height);
                            int x2 = XtoPx(LonToTileX((double)coords.Rows[index]["lng"], zoom), x_center, image.Width);
                            int y2 = YtoPx(LatToTileY((double)coords.Rows[index]["lat"], zoom), y_center, image.Height);
                            if (x1 != x2 || y1 != y2)
                            {
                                // Tools.DebugLog($"line ({x1},{y1})->({x2},{y2})");
                                graphics.DrawLine(whitePen, x1, y1, x2, y2);
                                graphics.DrawLine(bluePen, x1, y1, x2, y2);
                            }
                        }
                        // start and end icon
                        DrawIcon(image, coords.Rows[0], MapIcon.Start, zoom, x_center, y_center);
                        DrawIcon(image, coords.Rows[coords.Rows.Count - 1], MapIcon.End, zoom, x_center, y_center);
                    }
                }
            }
        }

        private void DrawIcon(Bitmap image, DataRow coord, MapIcon icon, int zoom, double x_center, double y_center)
        {
            SolidBrush brush;
            int scale = 1;
            switch (icon)
            {
                case MapIcon.Charge:
                    brush = new SolidBrush(Color.OrangeRed);
                    scale = 3;
                    break;
                case MapIcon.End:
                    brush = new SolidBrush(Color.Green);
                    break;
                case MapIcon.Park:
                    brush = new SolidBrush(Color.Blue);
                    scale = 3;
                    break;
                case MapIcon.Start:
                    brush = new SolidBrush(Color.Red);
                    break;
                default:
                    brush = new SolidBrush(Color.White);
                    break;
            }
            int x = XtoPx(LonToTileX((double)coord["lng"], zoom), x_center, image.Width);
            int y = YtoPx(LatToTileY((double)coord["lat"], zoom), y_center, image.Height);
            Rectangle rect = new Rectangle(x - 4 * scale, y - 10 * scale, 8 * scale, 8 * scale);
            Point[] triangle = new Point[] { new Point(x - 4 * scale, y - 6 * scale), new Point(x, y), new Point(x + 4 * scale, y - 6 * scale) };
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
                if (icon == MapIcon.Park || icon == MapIcon.Charge)
                {
                    string text = icon == MapIcon.Park ? "P" : "\u26A1";
                    using (Font drawFont = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold))
                    {
                        SizeF size = g.MeasureString(text, drawFont);
                        using (SolidBrush textBrush = new SolidBrush(Color.White))
                        {
                            using (StringFormat drawFormat = new StringFormat())
                            {
                                g.DrawString(text, drawFont, textBrush, x - size.Width / 2, y - 6 * scale - size.Height / 2);
                            }
                        }
                    }
                }
            }
            brush.Dispose();
        }

        public override void CreateChargingMap(DataRow coords, int width, int height, MapMode mapmode, MapSpecial special, string filename)
        {
            throw new NotImplementedException();
        }

        public override void CreateParkingMap(DataRow coords, int width, int height, MapMode mapmode, MapSpecial special, string filename)
        {
            throw new NotImplementedException();
        }
    }
}
