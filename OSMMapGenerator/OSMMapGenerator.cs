using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;


namespace TeslaLogger
{
    public class OSMMapGenerator
    {
        public enum MapMode
        {
            Regular,
            Dark
        }

        public enum MapIcon
        {
            Start,
            End,
            Park,
            Charge
        }

        private static bool debug = false;
        private static string jobfile = string.Empty;
        private static int tileSize = 256;
        private static string MapCachePath = string.Empty;
        private static Random random = new Random();
        private static Font drawFont8 = new Font(FontFamily.GenericSansSerif, 8);
        private static SolidBrush fillBrush = new SolidBrush(Color.FromArgb(51, 51, 51));
        private static SolidBrush grayBrush = new SolidBrush(Color.FromArgb(226,226,226));
        private static Pen bluePen = new Pen(Color.FromArgb(88,130,249), 3);
        private static Pen whitePen = new Pen(Color.White, 4);
        private static SolidBrush orangeBrush = new SolidBrush(Color.OrangeRed);
        private static SolidBrush greenBrush = new SolidBrush(Color.FromArgb(126, 178, 109));
        private static SolidBrush blueBrush = new SolidBrush(Color.FromArgb(62, 114, 177));
        private static SolidBrush redBrush = new SolidBrush(Color.FromArgb(226,77,66));
        private static SolidBrush whiteBrush = new SolidBrush(Color.White);
        private static Font drawFont12b = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold);
        private static Pen thinWhitePen = new Pen(Color.White, 1);

        public static void Main(string[] args)
        {
            if (ParseCmdLineArgs(args))
            {
                if (debug) { Console.WriteLine("OSMMapGenerator - Job: " + jobfile); }
                try
                {
                    string json = File.ReadAllText(jobfile);
                    File.Delete(jobfile);
                    dynamic jsonResult = JsonConvert.DeserializeObject(json);
                    Dictionary<string, object> job = jsonResult.ToObject<Dictionary<string, object>>();
                    if (job != null)
                    {
                        int width = Convert.ToInt32(job["width"]);
                        int height = Convert.ToInt32(job["height"]);
                        int zoom = Convert.ToInt32(job["zoom"]);
                        tileSize = Convert.ToInt32(job["tileSize"]);
                        double x_center = Convert.ToDouble(job["x_center"]);
                        double y_center = Convert.ToDouble(job["y_center"]);
                        MapMode mapmode = (MapMode)Enum.ToObject(typeof(MapMode), job["mapmode"]);
                        MapCachePath = job["MapCachePath"].ToString();
                        if (debug) { Console.WriteLine($"OSMMapGenerator - DrawMap(width:{width}, height:{height}, zoom:{zoom}, x_center:{x_center}, y_center:{y_center}, mapmode:{mapmode})"); }
                        Bitmap map = DrawMap(width, height, zoom, x_center, y_center, mapmode);
                        if (job.ContainsKey("latlng"))
                        {
                            DataTable coords = new DataTable();
                            DataColumn column = new DataColumn();
                            column.DataType = System.Type.GetType("System.Double");
                            column.ColumnName = "lat";
                            coords.Columns.Add(column);
                            column = new DataColumn();
                            column.DataType = Type.GetType("System.Double");
                            column.ColumnName = "lng";
                            coords.Columns.Add(column);
                            JArray latlng = (JArray)job["latlng"];

                            if (debug) { Console.WriteLine("OSMMapGenerator - latlng: " + latlng.Count); }
                            for (int index = 0; index < latlng.Count; index += 2)
                            {
                                DataRow drow = coords.NewRow();
                                drow["lat"] = latlng[index];
                                drow["lng"] = latlng[index + 1];
                                coords.Rows.Add(drow);
                            }
                            if (debug) { Console.WriteLine("OSMMapGenerator - DrawTrip"); }
                            DrawTrip(map, coords, zoom, x_center, y_center);
                            if (debug) { Console.WriteLine("OSMMapGenerator - DrawIcon"); }
                            DrawIcon(map, Convert.ToDouble(coords.Rows[0]["lat"]), Convert.ToDouble(coords.Rows[0]["lng"]), MapIcon.Start, zoom, x_center, y_center);
                            if (debug) { Console.WriteLine("OSMMapGenerator - DrawIcon"); }
                            DrawIcon(map, Convert.ToDouble(coords.Rows[coords.Rows.Count - 1]["lat"]), Convert.ToDouble(coords.Rows[coords.Rows.Count - 1]["lng"]), MapIcon.End, zoom, x_center, y_center);
                        }
                        else if (job.ContainsKey("poi"))
                        {
                            double lat = Convert.ToDouble(job["lat"]);
                            double lng = Convert.ToDouble(job["lng"]);
                            switch (job["poi"].ToString())
                            {
                                case "park":
                                    DrawIcon(map, lat, lng, MapIcon.Park, zoom, x_center, y_center);
                                    break;
                                case "charge":
                                    DrawIcon(map, lat, lng, MapIcon.Charge, zoom, x_center, y_center);
                                    break;
                            }
                        }
                        string filename = job["filename"].ToString();
                        if (mapmode == MapMode.Regular)
                        {
                            var f = new FileInfo(filename);
                            filename = Path.Combine(f.DirectoryName, "L_" + f.Name);
                        }

                        SaveImage(map, filename);
                    }
                }
                catch (Exception ex)
                {
                    if (debug)
                    { Console.WriteLine("OSMMapGenerator - Exception: " + ex.Message + " " + ex.StackTrace); }
                }
            }
        }

        public static void SaveImage(Bitmap image, string filename)
        {
            try
            {
                image.Save(filename);
                if (debug) { Console.WriteLine("OSMMapGenerator - SaveImage: " + filename); }
                if (File.Exists("/usr/bin/optipng"))
                {
                    using (Process process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "/usr/bin/optipng",
                            Arguments = "-o4 -silent " + filename,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    })
                    {
                        process.Start();
                        process.WaitForExit();
                    }
                }
            }
            catch (Exception) { }
        }

        private static bool ParseCmdLineArgs(string[] args)
        {
            bool jobfilefound = false;
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-jobfile":
                            jobfile = args[i + 1];
                            jobfilefound = true;
                            break;
                        case "-debug":
                            debug = true;
                            break;
                    }
                }
            }
            catch (Exception) { }
            return jobfilefound;
        }

        private static Bitmap DrawMap(int width, int height, int zoom, double x_center, double y_center, MapMode mode)
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

        private static void DrawMapLayer(Bitmap image, int width, int height, double x_center, double y_center, int zoom)
        {
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
                    // x and y may have crossed the date line
                    int max_tile = (int)Math.Pow(2, zoom);
                    int tile_x = (x + max_tile) % max_tile;
                    int tile_y = (y + max_tile) % max_tile;
                    tiles.Add(new Tuple<int, int, int, int, int>(x, y, zoom, tile_x, tile_y));
                }
            }
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

        private static void CopyRegionIntoImage(Bitmap srcBitmap, Rectangle srcRegion, Bitmap destBitmap, Rectangle destRegion)
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



        private static void ApplyDarkMode(Bitmap image)
        {
            FilterForest(image);
            AdjustBrightness(image, 0.6f);
            InvertImage(image);
            AdjustContrast(image, 1.3f);
            HueRotate(image, 200);
            AdjustSaturation(image, 0.3f);
            AdjustBrightness(image, 0.7f);
            AdjustContrast(image, 1.3f);
        }

        // https://github.com/JimBobSquarePants/ImageProcessor/blob/release/3.0.0/src/ImageProcessor/Processing/KnownColorMatrices.cs
        private static void AdjustBrightness(Image image, float amount)
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

        static Color cNeutral = Color.FromArgb(224, 223, 223);

        private static void FilterForest(Bitmap image)
        {
            for (int y = 0; (y <= (image.Height - 1)); y++)
            {
                for (int x = 0; (x <= (image.Width - 1)); x++)
                {
                    Color inv = image.GetPixel(x, y);

                    if (
                        (inv.R == 233 && inv.G == 238 && inv.B == 214) ||
                        (inv.R == 235 && inv.G == 219 && inv.B == 232) ||
                        (inv.R == 236 && inv.G == 236 && inv.B == 228) ||
                        (inv.R == 237 && inv.G == 242 && inv.B == 219) ||
                        (inv.R == 238 && inv.G == 240 && inv.B == 213) ||
                        (inv.R == 239 && inv.G == 226 && inv.B == 237) ||
                        (inv.R == 241 && inv.G == 243 && inv.B == 221) ||
                        (inv.R == 242 && inv.G == 238 && inv.B == 233) ||
                        (inv.R == 242 && inv.G == 239 && inv.B == 233)
                        )
                        image.SetPixel(x, y, cNeutral);
                    else if ( // Dreiecke
                            (inv.R == 208 && inv.G == 144 && inv.B == 86)
                        )
                        image.SetPixel(x, y, cNeutral);
                    else if (
                        (inv.R == 150 && inv.G == 184 && inv.B == 136) ||
                        (inv.R == 150 && inv.G == 185 && inv.B == 136) ||
                        (inv.R == 154 && inv.G == 198 && inv.B == 142) ||
                        (inv.R == 156 && inv.G == 192 && inv.B == 142) ||
                        (inv.R == 167 && inv.G == 201 && inv.B == 153) ||
                        (inv.R == 167 && inv.G == 202 && inv.B == 153) ||
                        (inv.R == 167 && inv.G == 210 && inv.B == 159) ||
                        (inv.R == 173 && inv.G == 209 && inv.B == 158) ||
                        (inv.R == 174 && inv.G == 206 && inv.B == 161) ||
                        (inv.R == 174 && inv.G == 210 && inv.B == 161) ||
                        (inv.R == 174 && inv.G == 223 && inv.B == 163) ||
                        (inv.R == 174 && inv.G == 221 && inv.B == 161) ||
                        (inv.R == 174 && inv.G == 222 && inv.B == 163) ||
                        (inv.R == 176 && inv.G == 210 && inv.B == 159) ||
                        (inv.R == 177 && inv.G == 221 && inv.B == 165) ||
                        (inv.R == 178 && inv.G == 211 && inv.B == 164) ||
                        (inv.R == 179 && inv.G == 188 && inv.B == 148) ||
                        (inv.R == 183 && inv.G == 198 && inv.B == 154) ||
                        (inv.R == 183 && inv.G == 214 && inv.B == 165) ||
                        (inv.R == 185 && inv.G == 198 && inv.B == 142) ||
                        (inv.R == 186 && inv.G == 212 && inv.B == 165) ||
                        (inv.R == 186 && inv.G == 213 && inv.B == 173) ||
                        (inv.R == 188 && inv.G == 213 && inv.B == 181) ||
                        (inv.R == 189 && inv.G == 218 && inv.B == 177) ||
                        (inv.R == 190 && inv.G == 229 && inv.B == 181) ||
                        (inv.R == 200 && inv.G == 215 && inv.B == 171) ||
                        (inv.R == 200 && inv.G == 228 && inv.B == 173) ||
                        (inv.R == 200 && inv.G == 250 && inv.B == 204) ||
                        (inv.R == 201 && inv.G == 225 && inv.B == 191) ||
                        (inv.R == 201 && inv.G == 249 && inv.B == 204) ||
                        (inv.R == 202 && inv.G == 225 && inv.B == 190) ||
                        (inv.R == 204 && inv.G == 213 && inv.B == 200) ||
                        (inv.R == 205 && inv.G == 235 && inv.B == 176) ||
                        (inv.R == 210 && inv.G == 234 && inv.B == 186) ||
                        (inv.R == 214 && inv.G == 237 && inv.B == 193) ||
                        (inv.R == 215 && inv.G == 238 && inv.B == 192) ||
                        (inv.R == 217 && inv.G == 238 && inv.B == 196) ||
                        (inv.R == 220 && inv.G == 244 && inv.B == 196) ||
                        (inv.R == 222 && inv.G == 237 && inv.B == 205) ||
                        (inv.R == 223 && inv.G == 250 && inv.B == 226) ||
                        (inv.R == 223 && inv.G == 251 && inv.B == 226) ||
                        (inv.R == 223 && inv.G == 252 && inv.B == 226) ||
                        (inv.R == 238 && inv.G == 240 && inv.B == 213) 
                        )
                        image.SetPixel(x, y, cNeutral);

                }
            }
        }

        // https://mariusbancila.ro/blog/2009/11/13/using-colormatrix-for-creating-negative-image/
        private static void InvertImage(Bitmap image)
        {
            for (int y = 0; (y <= (image.Height - 1)); y++)
            {
                for (int x = 0; (x <= (image.Width - 1)); x++)
                {
                    Color inv = image.GetPixel(x, y);
                    inv = Color.FromArgb(255, (255 - inv.R), (255 - inv.G), (255 - inv.B));
                    image.SetPixel(x, y, inv);
                }
            }
            
            /*

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
            */
        }

        // https://github.com/JimBobSquarePants/ImageProcessor/blob/release/3.0.0/src/ImageProcessor/Processing/KnownColorMatrices.cs
        private static void AdjustContrast(Bitmap image, float amount)
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
        private static void AdjustSaturation(Bitmap image, float amount)
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
        private static void HueRotate(Bitmap image, float degrees)
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

        private static Bitmap DownloadTile(int zoom, int tile_x, int tile_y)
        {
            string localMapCacheFilePath = Path.Combine(MapCachePath, $"{zoom}_{tile_x}_{tile_y}.png");
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
                    try
                    {
                        using (WebClient wc = new WebClient())
                        {
                            wc.Headers["User-Agent"] = "TeslaLogger.OSMMapGenerator";
                            wc.DownloadFile(url, localMapCacheFilePath);
                            wc.Dispose();
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
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

        internal static bool MapFileExistsOrIsTooOld(string filename, int days = 90)
        {
            try
            {
                // check file age
                if (File.Exists(filename))
                {
                    if ((DateTime.UtcNow - File.GetCreationTimeUtc(filename)).TotalDays > days)
                    {
                        File.Delete(filename);
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception) { }
            return false;
        }

        // transform tile number to pixel on image canvas
        private static int YtoPx(double y, double y_center, int height)
        {
            double px = (y - y_center) * tileSize + height / 2.0;
            return (int)(Math.Round(px));
        }

        // transform tile number to pixel on image canvas
        private static int XtoPx(double x, double x_center, int width)
        {
            double px = (x - x_center) * tileSize + width / 2.0;
            return (int)(Math.Round(px));
        }

        private static void DrawAttribution(Bitmap image)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                string attribution = "©OSM";
                SizeF size = g.MeasureString(attribution, drawFont8);
                g.FillRectangle(fillBrush, new Rectangle((int)(image.Width - size.Width - 3), (int)(image.Height - size.Height - 3), (int)(size.Width + 6), (int)(size.Height + 6)));
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.DrawString(attribution, drawFont8, grayBrush, image.Width - size.Width - 2, image.Height - size.Height - 2);
            }
        }

        private static void DrawTrip(Bitmap image, DataTable coords, int zoom, double x_center, double y_center)
        {
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // draw Trip line
                /*
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
                */
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

        // transform longitude to tile number
        private static double LngToTileX(double lon, int zoom)
        {
            return ((lon + 180.0) / 360.0) * Math.Pow(2.0, zoom);
        }

        // transform latitude to tile number
        private static double LatToTileY(double lat, int zoom)
        {
            return (1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0) + 1 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * Math.Pow(2.0, zoom);
        }

        private static void DrawIcon(Bitmap image, double lat, double lng, MapIcon icon, int zoom, double x_center, double y_center)
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
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    g.DrawString(text, drawFont12b, whiteBrush, x - size.Width / 2, y - 6 * scale - size.Height / 2);
                    g.Dispose();
                }
            }
        }
    }
}
