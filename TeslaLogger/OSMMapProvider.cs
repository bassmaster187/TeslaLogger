using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;

namespace TeslaLogger
{
    public class OSMMapProvider : StaticMapProvider
    {
        private static readonly Random random = new Random();
        private static int padding_x = 12;
        private static int padding_y = 12;
        private const int tileSize = 256;

        public override void CreateTripMap(DataTable coords, int width, int height, MapMode mapmode, MapSpecial special, string filename)
        {
            if (coords == null)
            {
                return;
            }
            // workaround for linux mono libgdiplus memory leak
            Dictionary<string, object> job = new Dictionary<string, object>();
            Tuple<double, double, double, double> extent = DetermineExtent(coords);
            if (extent == null)
            {
                return;
            }
            // calculate center point of map
            double lat_center = (extent.Item1 + extent.Item3) / 2;
            double lng_center = (extent.Item2 + extent.Item4) / 2;
            int zoom = CalculateZoom(extent, width, height);
            job.Add("zoom", zoom);
            double x_center = LngToTileX(lng_center, zoom);
            job.Add("x_center", x_center);
            double y_center = LatToTileY(lat_center, zoom);
            job.Add("y_center", y_center);
            job.Add("filename", filename);
            job.Add("width", width);
            job.Add("height", height);
            job.Add("mapmode", mapmode);
            job.Add("tileSize", tileSize);
            job.Add("MapCachePath", FileManager.GetMapCachePath());
            List<double> latlng = new List<double>();
            for (int row = 0; row < coords.Rows.Count; row++)
            {
                latlng.Add(Convert.ToDouble(coords.Rows[row]["lat"], Tools.ciDeDE));
                latlng.Add(Convert.ToDouble(coords.Rows[row]["lng"], Tools.ciDeDE));
            }
            job.Add("latlng", latlng.ToArray());
            string tempfile = Path.GetTempFileName();
            File.WriteAllText(tempfile, JsonConvert.SerializeObject(job), Encoding.UTF8);

            GetOSMMapGeneratorFilename(out string fileName, out string arguments);
            arguments += "-jobfile " + tempfile + (Program.VERBOSE ? " - debug" : "");

            using (Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            })
            {
                process.Start();
                while (!process.StandardOutput.EndOfStream)
                {
                    Logfile.Log(process.StandardOutput.ReadLine());
                }
                process.WaitForExit();
            }
            if (File.Exists(tempfile))
            {
                File.Delete(tempfile);
            }
        }

        // transform longitude to tile number
        private double LngToTileX(double lon, int zoom)
        {
            return ((lon + 180.0) / 360.0) * Math.Pow(2.0, zoom);
        }

        // transform latitude to tile number
        private double LatToTileY(double lat, int zoom)
        {
            return (1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) + 1 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * Math.Pow(2.0, zoom);
        }

        private int CalculateZoom(Tuple<double, double, double, double> extent, int width, int height)
        {
            for (int zoom = 18; zoom > 0; zoom--)
            {
                double _width = (LngToTileX(extent.Item4, zoom) - LngToTileX(extent.Item2, zoom)) * tileSize;
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

        // https://github.com/JimBobSquarePants/ImageProcessor/blob/release/3.0.0/src/ImageProcessor/Processing/KnownColorMatrices.cs
        public static float DegreeToRadian(float degree) => degree * (float)(Math.PI / 180F);

        public override void CreateChargingMap(double lat, double lng, int width, int height, MapMode mapmode, MapSpecial special, string filename)
        {
            int zoom = 16;

            // workaround for linux mono libgdiplus memory leak
            Dictionary<string, object> job = new Dictionary<string, object>();
            double x_center = LngToTileX(lng, zoom);
            double y_center = LatToTileY(lat, zoom);
            job.Add("zoom", zoom);
            job.Add("x_center", x_center);
            job.Add("y_center", y_center);
            job.Add("filename", filename);
            job.Add("width", width);
            job.Add("height", height);
            job.Add("mapmode", mapmode);
            job.Add("tileSize", tileSize);
            job.Add("poi", "charge");
            job.Add("lat", lat);
            job.Add("lng", lng);
            job.Add("MapCachePath", FileManager.GetMapCachePath());
            string tempfile = Path.GetTempFileName();
            File.WriteAllText(tempfile, JsonConvert.SerializeObject(job), Encoding.UTF8);

            GetOSMMapGeneratorFilename(out string fileName, out string arguments);
            arguments += "-jobfile " + tempfile + (Program.VERBOSE ? " - debug" : "");

            using (Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            })
            {
                process.Start();
                while (!process.StandardOutput.EndOfStream)
                {
                    Logfile.Log(process.StandardOutput.ReadLine());
                }
                process.WaitForExit();
            }
            if (File.Exists(tempfile))
            {
                File.Delete(tempfile);
            }
            /*
            using (Bitmap map = DrawMap(width, height, 19, x_center, y_center, mapmode))
            {
                // map has background tiles, OSM attribution and dark mode, if enabled
                DrawIcon(map, lat, lng, MapIcon.Charge, 19, x_center, y_center);
                SaveImage(map, filename);
                map.Dispose();
            }
            */
        }

        public override void CreateParkingMap(double lat, double lng, int width, int height, MapMode mapmode, MapSpecial special, string filename)
        {
            int zoom = 16;

            // workaround for linux mono libgdiplus memory leak
            Dictionary<string, object> job = new Dictionary<string, object>();
            double x_center = LngToTileX(lng, zoom);
            double y_center = LatToTileY(lat, zoom);
            job.Add("zoom", zoom);
            job.Add("x_center", x_center);
            job.Add("y_center", y_center);
            job.Add("filename", filename);
            job.Add("width", width);
            job.Add("height", height);
            job.Add("mapmode", mapmode);
            job.Add("tileSize", tileSize);
            job.Add("MapCachePath", FileManager.GetMapCachePath());
            job.Add("poi", "park");
            job.Add("lat", lat);
            job.Add("lng", lng);

            string tempfile = Path.GetTempFileName();
            GetOSMMapGeneratorFilename(out string fileName, out string arguments);
            arguments += "-jobfile " + tempfile + (Program.VERBOSE ? " - debug" : "");

            File.WriteAllText(tempfile, JsonConvert.SerializeObject(job), Encoding.UTF8);
            using (Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            })
            {
                process.Start();
                while (!process.StandardOutput.EndOfStream)
                {
                    Logfile.Log(process.StandardOutput.ReadLine());
                }
                process.WaitForExit();
            }
            if (File.Exists(tempfile))
            {
                File.Delete(tempfile);
            }
            /*
            using (Bitmap map = DrawMap(width, height, 19, x_center, y_center, mapmode))
            {
                // map has background tiles, OSM attribution and dark mode, if enabled
                DrawIcon(map, lat, lng, MapIcon.Park, 19, x_center, y_center);
                SaveImage(map, filename);
                map.Dispose();
            }
            */
        }

        void GetOSMMapGeneratorFilename(out string fileName, out string arguments)
        {
            fileName = "/usr/bin/mono";
            arguments = "/etc/teslalogger/OSMMapGenerator.exe ";

            if (!Tools.IsMono())
            {
                var f = new FileInfo("../../../OSMMapGenerator/bin/Debug/OSMMapGenerator.exe");
                fileName = f.FullName;
                arguments = "";
            }
        }

        public override int GetDelayMS()
        {
            return 500;
        }

        public override bool UseIt()
        {
            return true;
        }
    }
}
