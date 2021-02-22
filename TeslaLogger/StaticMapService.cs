using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
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

        private static Random random = new Random();

        private static StaticMapService _StaticMapService = null;

        private readonly ConcurrentQueue<Tuple<int, int, StaticMapType>> queue = new ConcurrentQueue<Tuple<int, int, StaticMapType>>();

        private StaticMapService()
        {
            Logfile.Log("StaticMapService initialized");
            Enqueue(new Tuple<int, int, StaticMapType>(465292, 471276, StaticMapType.Trip));
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

        public void Enqueue(Tuple<int, int, StaticMapType> request)
        {
            queue.Enqueue(request);
        }

        public void Run()
        {
            Tools.DebugLog("StaticMapService:Run()");
            try
            {
                while(true)
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
            if (queue.TryDequeue(out Tuple<int, int, StaticMapType> request))
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
                    Tools.DebugLog("StaticMapService:Work() zoom:" + zoom);
                    Tuple<double, double, double, double> extent = DetermineExtent(coords);
                    // calculate center point of map
                    double lat_center = (extent.Item1 + extent.Item3) / 2;
                    double lng_center = (extent.Item2 + extent.Item4) / 2;
                    double x_center = LonToX(lng_center, zoom);
                    double y_center = LatToY(lat_center, zoom);
                    using (Bitmap image = new Bitmap(width, height))
                    {
                        DrawMapLayer(image, width, height, x_center, y_center, tileSize, zoom);
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

        private void DrawTrip(Bitmap image, List<Tuple<double, double>> coords, int zoom, double x_center, double y_center, int tileSize)
        {
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Pen bluePen = new Pen(Color.Blue, 1);
                for (int index = 1; index < coords.Count; index++)
                {
                    Tools.DebugLog($"coord {coords[index - 1].Item2},{coords[index - 1].Item1}->{LonToX(coords[index - 1].Item2, zoom)}{LatToY(coords[index - 1].Item1, zoom)}");
                    graphics.DrawLine(bluePen,
                        (int)LonToX(coords[index - 1].Item2, zoom),
                        (int)LatToY(coords[index - 1].Item1, zoom),
                        (int)LonToX(coords[index].Item2, zoom),
                        (int)LatToY(coords[index].Item1, zoom)
                        );
                }
            }
        }

        private List<Tuple<double, double>> TripToCoords(Tuple<int, int, StaticMapType> request)
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

        private int YtoPx(int y, double y_center, int tileSize, int height)
        {
            double px = (y - y_center) * tileSize + height / 2;
            return (int)(Math.Round(px));
        }

        private int XtoPx(int x, double x_center, int tileSize, int width) {
            double px = (x - x_center) * tileSize + width / 2;
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
            for (int zoom = 17; zoom > 0; zoom--)
            {
                Tuple<double, double, double, double> extent = DetermineExtent(coords);
                double _width = (LonToX(extent.Item3, zoom) - LonToX(extent.Item1, zoom)) * tileSize;
                if (_width > (width - padding_x * 2)) {
                    continue;
                }

                double _height = (LatToY(extent.Item2, zoom) - LatToY(extent.Item4, zoom)) * tileSize;
                if (_height > (height - padding_y * 2)) {
                    continue;
                }

                // we found first zoom that can display entire extent
                return zoom;
            }
            return 0;
        }

        private double LonToX(double lon, int zoom)
        {
            if (lon < -180 || lon > 180)
            {
                lon = (lon + 180) % 360 - 180;
            }

            return ((lon + 180.0) / 360) * Math.Pow(2, zoom);
        }

        private double LatToY(double lat, int zoom)
        {
            if (lat < -90 || lat > 90)
            {
                lat = (lat + 90) % 180 - 90;
            }
            return (1 - Math.Log(Math.Tan(lat * Math.PI / 180) + 1 / Math.Cos(lat * Math.PI / 180)) / Math.PI) / 2 * Math.Pow(2, zoom);
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
