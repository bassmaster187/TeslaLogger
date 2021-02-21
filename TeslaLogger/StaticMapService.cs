using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace TeslaLogger
{
    public class StaticMapService
    {

        private static StaticMapService _StaticMapService = null;

        private readonly ConcurrentQueue<List<Tuple<double, double>>> queue = new ConcurrentQueue<List<Tuple<double, double>>>();

        private StaticMapService()
        {
            Logfile.Log("StaticMapService initialized");
            List<Tuple<double, double>> testlist = new List<Tuple<double, double>>();
            testlist.Add(new Tuple<double, double>(52.446453, 13.357752));
            testlist.Add(new Tuple<double, double>(52.449295, 13.35435));
            Enqueue(testlist);
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

        public void Enqueue(List<Tuple<double, double>> list)
        {
            queue.Enqueue(list);
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
            if (queue.TryDequeue(out List<Tuple<double, double>> polyList))
            {
                Tools.DebugLog("StaticMapService:Work() polyList:" + polyList.Count);
                int padding_x = 8;
                int padding_y = 8;
                int width = 240;
                int height = (int)(width / 1.618033);
                int tileSize = 256;
                if (polyList.Count == 1)
                {
                    // park or charge
                    int zoom = 19; // max zoom
                }
                else if (polyList.Count > 1)
                {
                    // trip
                    int zoom = CalculateZoom(polyList, padding_x, padding_y, width, height, tileSize);
                    Tools.DebugLog("StaticMapService:Work() zoom:" + zoom);
                    Tuple<double, double, double, double> extent = DetermineExtent(polyList);
                    // calculate center point of map
                    double lat_center = (extent.Item1 + extent.Item3) / 2;
                    double lng_center = (extent.Item2 + extent.Item4) / 2;
                    double x_center = LonToX(lng_center, zoom);
                    double y_center = LatToY(lat_center, zoom);
                    using (Bitmap image = new Bitmap(width, height))
                    {
                        DrawMapLayer(image, width, height, x_center, y_center, tileSize, zoom);
                    }
                }
                else
                {
                    Tools.DebugLog("StaticMapService:Work() polyList is empty");
                }
            }
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
            for (int x = x_min; x <= x_max; x++)
            {
                for (int y = y_min; y <= y_max; y++)
                {
                    Tools.DebugLog($"StaticMapService:DrawMapLayer() x:{x} y:{y}");
                    // x and y may have crossed the date line
                    int max_tile = (int)Math.Pow(2, zoom);
                    int tile_x = (x + max_tile) % max_tile;
                    int tile_y = (y + max_tile) % max_tile;
                    Uri url = new Uri($"http://a.tile.osm.org/{zoom}/{tile_x}/{tile_y}.png");
                    tiles.Add(new Tuple<int, int, Uri>(x, y, url));
                }
            }
            Tools.DebugLog("StaticMapService:DrawMapLayer() tiles:" + tiles.Count);
            foreach (Tuple<int, int, Uri> tile in tiles)
            {
                Tools.DebugLog("StaticMapService:Download " + tile.Item3);
            }
        }

        private int CalculateZoom(List<Tuple<double, double>> polyList, int padding_x, int padding_y, int width, int height, int tileSize)
        {
            for (int zoom = 17; zoom > 0; zoom--)
            {
                Tuple<double, double, double, double> extent = DetermineExtent(polyList);
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

        private Tuple<double, double, double, double> DetermineExtent(List<Tuple<double, double>> polyList)
        {
            double min_lat = polyList.First().Item1;
            double min_lng = polyList.First().Item2;
            double max_lat = polyList.First().Item1;
            double max_lng = polyList.First().Item2;
            foreach (Tuple<double, double> coord in polyList)
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
