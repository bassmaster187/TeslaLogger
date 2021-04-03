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
        private static Random random = new Random();
        private static int padding_x = 12;
        private static int padding_y = 12;
        private static int tileSize = 256;

        private static Font drawFont8 = new Font(FontFamily.GenericSansSerif, 8);
        private static Font drawFont12b = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold);
        private static SolidBrush fillBrush = new SolidBrush(Color.FromArgb(192, 192, 192, 128));
        private static SolidBrush blackBrush = new SolidBrush(Color.Black);
        private static SolidBrush whiteBrush = new SolidBrush(Color.White);
        private static SolidBrush orangeBrush = new SolidBrush(Color.OrangeRed);
        private static SolidBrush greenBrush = new SolidBrush(Color.Green);
        private static SolidBrush blueBrush = new SolidBrush(Color.Blue);
        private static SolidBrush redBrush = new SolidBrush(Color.Red);
        private static Pen bluePen = new Pen(Color.Blue, 2);
        private static Pen whitePen = new Pen(Color.White, 4);
        private static Pen thinWhitePen = new Pen(Color.White, 1);

        public override void CreateTripMap(DataTable coords, int width, int height, MapMode mapmode, MapSpecial special, string filename)
        {
            // workaround for linux mono libgdiplus memory leak
            Dictionary<string, object> job = new Dictionary<string, object>();
            Tuple<double, double, double, double> extent = DetermineExtent(coords);
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
                latlng.Add(Convert.ToDouble(coords.Rows[row]["lat"]));
                latlng.Add(Convert.ToDouble(coords.Rows[row]["lng"]));
            }
            job.Add("latlng", latlng.ToArray());
            string tempfile = Path.GetTempFileName();
            File.WriteAllText(tempfile, new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(job), Encoding.UTF8);

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
            using (Bitmap map = DrawMap(width, height, zoom, x_center, y_center, mapmode))
            {
                //    // map has background tiles, OSM attribution and dark mode, if enabled
                DrawTrip(map, coords, zoom, x_center, y_center);
                DrawIcon(map, Convert.ToDouble(coords.Rows[0]["lat"]), Convert.ToDouble(coords.Rows[0]["lng"]), MapIcon.Start, zoom, x_center, y_center);
                DrawIcon(map, Convert.ToDouble(coords.Rows[coords.Rows.Count - 1]["lat"]), Convert.ToDouble(coords.Rows[coords.Rows.Count - 1]["lng"]), MapIcon.End, zoom, x_center, y_center);
                SaveImage(map, filename);
                map.Dispose();
            }
            */
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

        private void CopyRegionIntoImage(Bitmap srcBitmap, Rectangle srcRegion, Bitmap destBitmap, Rectangle destRegion)
        {
            int srcX = destRegion.X < 0 ? Math.Abs(destRegion.X) : 0;
            int srcY = destRegion.Y < 0 ? Math.Abs(destRegion.Y) : 0;
            int srcW = srcRegion.Width - Math.Abs(destRegion.X);
            int srcH = srcRegion.Height - Math.Abs(destRegion.Y);
            int destX = destRegion.X < 0 ? 0 : destRegion.X;
            int destY = destRegion.Y < 0 ? 0 : destRegion.Y;
            int destW = srcW;
            int destH = srcH;
            Rectangle src = new Rectangle(srcX, srcY, srcW, srcH);
            Rectangle dest = new Rectangle(destX, destY, destW, destH);
            using (Graphics grD = Graphics.FromImage(destBitmap))
            {
                grD.DrawImage(srcBitmap, dest, src, GraphicsUnit.Pixel);
                grD.Dispose();
            }
        }

        private Bitmap DownloadTile(int zoom, int tile_x, int tile_y)
        {
            string localMapCacheFilePath = Path.Combine(FileManager.GetMapCachePath(), $"{zoom}_{tile_x}_{tile_y}.png");
            if (MapFileExistsOrIsTooOld(localMapCacheFilePath, 8))
            {
                // cached file too old or does not exist yet
                int retries = 0;
                while (!File.Exists(localMapCacheFilePath) && retries < 10)
                {
                    retries++;
                    int num = random.Next(0, 3);
                    char abc = (char)('a' + num);
                    Uri url = new Uri($"http://{abc}.tile.osm.org/{zoom}/{tile_x}/{tile_y}.png");
                    Tools.DebugLog("DownloadTile() url: " + url);
                    try
                    {
                        using (WebClient wc = new WebClient())
                        {
                            wc.Headers["User-Agent"] = this.GetType().ToString();
                            wc.DownloadFile(url, localMapCacheFilePath);
                            wc.Dispose();
                        }
                    }
                    catch (WebException)
                    {
                        Tools.DebugLog("DownloadTile() failed for url: " + url);
                    }
                }
            }
            else
            {
                Tools.DebugLog("DownloadTile() use cached local file " + localMapCacheFilePath);
            }
            try
            {
                using (Image img = Image.FromFile(localMapCacheFilePath))
                {
                    return new Bitmap(img);
                }
            }
            catch (Exception)
            {
                return new Bitmap(tileSize, tileSize);
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
            Tools.DebugLog($"DrawMapLayer() width:{width} height:{height}");
            int x_min = (int)(Math.Floor(x_center - (0.5 * width / tileSize)));
            int y_min = (int)(Math.Floor(y_center - (0.5 * height / tileSize)));
            int x_max = (int)(Math.Ceiling(x_center + (0.5 * width / tileSize)));
            int y_max = (int)(Math.Ceiling(y_center + (0.5 * height / tileSize)));
            // assemble all map tiles needed for the map
            List<Tuple<int, int, int, int, int>> tiles = new List<Tuple<int, int, int, int, int>>();
            for (int x = x_min; x < x_max; x++)
            {
                for (int y = y_min; y < y_max; y++)
                {
                    Tools.DebugLog($"DrawMapLayer() x:{x} y:{y}");
                    // x and y may have crossed the date line
                    int max_tile = (int)Math.Pow(2, zoom);
                    int tile_x = (x + max_tile) % max_tile;
                    int tile_y = (y + max_tile) % max_tile;
                    tiles.Add(new Tuple<int, int, int, int, int>(x, y, zoom, tile_x, tile_y));
                }
            }
            Tools.DebugLog("DrawMapLayer() tiles:" + tiles.Count);
            foreach (Tuple<int, int, int, int, int> tile in tiles)
            {
                using (Bitmap tileImage = DownloadTile(tile.Item3, tile.Item4, tile.Item5))
                {
                    if (tileImage != null)
                    {
                        Rectangle box = new Rectangle(XtoPx(tile.Item1, x_center, width), YtoPx(tile.Item2, y_center, height), tileSize, tileSize);
                        CopyRegionIntoImage(tileImage, new Rectangle(0, 0, tileSize, tileSize), image, box);
                    }
                }
            }
        }

        private void ApplyDarkMode(Bitmap image)
        {
            AdjustBrightness(image, 0.6f);
            InvertImage(image);
            AdjustContrast(image, 1.3f);
            HueRotate(image, 200);
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
                string attribution = "©OSM";
                SizeF size = g.MeasureString(attribution, drawFont8);
                g.FillRectangle(fillBrush, new Rectangle((int)(image.Width - size.Width - 3), (int)(image.Height - size.Height - 3), (int)(size.Width + 6), (int)(size.Height + 6)));
                g.DrawString(attribution, drawFont8, blackBrush, image.Width - size.Width - 2, image.Height - size.Height - 2);
            }
        }

        // https://github.com/JimBobSquarePants/ImageProcessor/blob/release/3.0.0/src/ImageProcessor/Processing/KnownColorMatrices.cs
        private void AdjustContrast(Bitmap image, float amount)
        {
            using (ImageAttributes ia = new ImageAttributes())
            {
                //create the scaling matrix
                float contrast = (-.5F * amount) + .5F;
                ColorMatrix cm = new ColorMatrix
                {
                    Matrix00 = amount,
                    Matrix11 = amount,
                    Matrix22 = amount,
                    Matrix33 = 1F,
                    Matrix40 = contrast,
                    Matrix41 = contrast,
                    Matrix42 = contrast
                };
                ia.SetColorMatrix(cm);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ia);
                    ia.Dispose();
                    g.Dispose();
                }
            }
        }

        // https://github.com/JimBobSquarePants/ImageProcessor/blob/release/3.0.0/src/ImageProcessor/Processing/KnownColorMatrices.cs
        private void AdjustSaturation(Bitmap image, float amount)
        {
            ColorMatrix m = new ColorMatrix();
            m.Matrix00 = .213F + (.787F * amount);
            m.Matrix10 = .715F - (.715F * amount);
            m.Matrix20 = 1F - (m.Matrix00 + m.Matrix10);

            m.Matrix01 = .213F - (.213F * amount);
            m.Matrix11 = .715F + (.285F * amount);
            m.Matrix21 = 1F - (m.Matrix01 + m.Matrix11);

            m.Matrix02 = .213F - (.213F * amount);
            m.Matrix12 = .715F - (.715F * amount);
            m.Matrix22 = 1F - (m.Matrix02 + m.Matrix12);
            m.Matrix33 = 1F;
            using (ImageAttributes ia = new ImageAttributes())
            {
                ia.SetColorMatrix(m, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ia);
                    ia.Dispose();
                    g.Dispose();
                }
            }
        }

        // https://github.com/JimBobSquarePants/ImageProcessor/blob/release/3.0.0/src/ImageProcessor/Processing/KnownColorMatrices.cs
        public static float DegreeToRadian(float degree) => degree * (float)(Math.PI / 180F);
        private void HueRotate(Bitmap image, float degrees)
        {
            degrees %= 360;
            while (degrees < 0)
            {
                degrees += 360;
            }
            float radian = DegreeToRadian(degrees);
            float cosRadian = (float)Math.Cos(radian);
            float sinRadian = (float)Math.Sin(radian);
            ColorMatrix colorMatrix = new ColorMatrix
            {
                Matrix00 = .213F + (cosRadian * .787F) - (sinRadian * .213F),
                Matrix10 = .715F - (cosRadian * .715F) - (sinRadian * .715F),
                Matrix20 = .072F - (cosRadian * .072F) + (sinRadian * .928F),

                Matrix01 = .213F - (cosRadian * .213F) + (sinRadian * .143F),
                Matrix11 = .715F + (cosRadian * .285F) + (sinRadian * .140F),
                Matrix21 = .072F - (cosRadian * .072F) - (sinRadian * .283F),

                Matrix02 = .213F - (cosRadian * .213F) - (sinRadian * .787F),
                Matrix12 = .715F - (cosRadian * .715F) + (sinRadian * .715F),
                Matrix22 = .072F + (cosRadian * .928F) + (sinRadian * .072F),
                Matrix33 = 1F
            };
            using (ImageAttributes ia = new ImageAttributes())
            {
                ia.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ia);
                    ia.Dispose();
                    g.Dispose();
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
                    g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
                    attributes.Dispose();
                    g.Dispose();
                }
            }
        }

        // https://github.com/JimBobSquarePants/ImageProcessor/blob/release/3.0.0/src/ImageProcessor/Processing/KnownColorMatrices.cs
        private void AdjustBrightness(Image image, float amount)
        {
            ColorMatrix cm = new ColorMatrix
            {
                Matrix00 = amount,
                Matrix11 = amount,
                Matrix22 = amount,
                Matrix33 = 1F
            }; 
            using (ImageAttributes ia = new ImageAttributes())
            {
                ia.SetColorMatrix(cm);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ia);
                    ia.Dispose();
                    g.Dispose();
                }
            }
        }

        private void DrawTrip(Bitmap image, DataTable coords, int zoom, double x_center, double y_center)
        {
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // draw Trip line
                for (int index = 1; index < coords.Rows.Count; index++)
                {
                    int x1 = XtoPx(LngToTileX(Convert.ToDouble(coords.Rows[index - 1]["lng"]), zoom), x_center, image.Width);
                    int y1 = YtoPx(LatToTileY(Convert.ToDouble(coords.Rows[index - 1]["lat"]), zoom), y_center, image.Height);
                    int x2 = XtoPx(LngToTileX(Convert.ToDouble(coords.Rows[index]["lng"]), zoom), x_center, image.Width);
                    int y2 = YtoPx(LatToTileY(Convert.ToDouble(coords.Rows[index]["lat"]), zoom), y_center, image.Height);
                    if (x1 != x2 || y1 != y2)
                    {
                        graphics.DrawLine(whitePen, x1, y1, x2, y2);
                    }
                }
                for (int index = 1; index < coords.Rows.Count; index++)
                {
                    int x1 = XtoPx(LngToTileX(Convert.ToDouble(coords.Rows[index - 1]["lng"]), zoom), x_center, image.Width);
                    int y1 = YtoPx(LatToTileY(Convert.ToDouble(coords.Rows[index - 1]["lat"]), zoom), y_center, image.Height);
                    int x2 = XtoPx(LngToTileX(Convert.ToDouble(coords.Rows[index]["lng"]), zoom), x_center, image.Width);
                    int y2 = YtoPx(LatToTileY(Convert.ToDouble(coords.Rows[index]["lat"]), zoom), y_center, image.Height);
                    if (x1 != x2 || y1 != y2)
                    {
                        graphics.DrawLine(bluePen, x1, y1, x2, y2);
                    }
                }
            }
        }

        private void DrawIcon(Bitmap image, double lat, double lng, MapIcon icon, int zoom, double x_center, double y_center)
        {
            int scale = 1;
            switch (icon)
            {
                case MapIcon.Charge:
                case MapIcon.Park:
                    scale = 3;
                    break;
            }
            int x = XtoPx(LngToTileX(lng, zoom), x_center, image.Width);
            int y = YtoPx(LatToTileY(lat, zoom), y_center, image.Height);
            Rectangle rect = new Rectangle(x - 4 * scale, y - 10 * scale, 8 * scale, 8 * scale);
            Point[] triangle = new Point[] { new Point(x - 4 * scale, y - 6 * scale), new Point(x, y), new Point(x + 4 * scale, y - 6 * scale) };
            using (Graphics g = Graphics.FromImage(image))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                switch (icon)
                {
                    case MapIcon.Start:
                        g.FillPie(greenBrush, rect, 180, 180);
                        g.FillPolygon(greenBrush, triangle);
                        break;
                    case MapIcon.End:
                        g.FillPie(redBrush, rect, 180, 180);
                        g.FillPolygon(redBrush, triangle);
                        break;
                    case MapIcon.Park:
                        g.FillPie(blueBrush, rect, 180, 180);
                        g.FillPolygon(blueBrush, triangle);
                        break;
                    case MapIcon.Charge:
                        g.FillPie(orangeBrush, rect, 180, 180);
                        g.FillPolygon(orangeBrush, triangle);
                        break;
                }
                g.DrawArc(thinWhitePen, rect, 180, 180);
                g.DrawLine(thinWhitePen, triangle[0], triangle[1]);
                g.DrawLine(thinWhitePen, triangle[1], triangle[2]);
                if (icon == MapIcon.Park || icon == MapIcon.Charge)
                {
                    string text = icon == MapIcon.Park ? "P" : "\u26A1";
                    SizeF size = g.MeasureString(text, drawFont12b);
                    g.DrawString(text, drawFont12b, whiteBrush, x - size.Width / 2, y - 6 * scale - size.Height / 2);
                    g.Dispose();
                }
            }
        }

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
            File.WriteAllText(tempfile, new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(job), Encoding.UTF8);

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

            File.WriteAllText(tempfile, new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(job), Encoding.UTF8);
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
