using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;


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
        private static SKFont drawFont8 = new SKFont(SKTypeface.FromFamilyName("SanSerif"), 10);
        /* ccc
        private static SolidBrush fillBrush = new SolidBrush(Color.FromArgb(51, 51, 51));
        private static SolidBrush grayBrush = new SolidBrush(Color.FromArgb(226,226,226));
        private static Pen bluePen = new Pen(Color.FromArgb(88,130,249), 3);
        private static Pen whitePen = new Pen(Color.White, 4);
        private static SolidBrush orangeBrush = new SolidBrush(Color.OrangeRed);
        private static SolidBrush greenBrush = new SolidBrush(Color.FromArgb(126, 178, 109));
        private static SolidBrush blueBrush = new SolidBrush(Color.FromArgb(62, 114, 177));
        private static SolidBrush redBrush = new SolidBrush(Color.FromArgb(226,77,66));
        private static SolidBrush whiteBrush = new SolidBrush(Color.White);
        */
        static SKPaint bluePen = new SKPaint {Color = new SKColor(80,130,249), Style = SKPaintStyle.Stroke, IsAntialias = true, StrokeWidth = 3};
        static SKPaint orangeBrush = new SKPaint { Color = SKColors.Orange, Style = SKPaintStyle.Fill, IsAntialias = true, StrokeWidth = 1 };
        static SKPaint greenBrush = new SKPaint { Color = new SKColor(126, 178, 109), Style = SKPaintStyle.Fill, IsAntialias = true, StrokeWidth = 1};
        static SKPaint blueBrush = new SKPaint { Color = new SKColor(62, 114, 177), Style = SKPaintStyle.Fill, IsAntialias = true, StrokeWidth = 1 };
        static SKPaint redBrush = new SKPaint { Color = new SKColor(226, 77, 66), Style = SKPaintStyle.Fill, IsAntialias = true, StrokeWidth = 1 };
        static SKPaint thinWhitePen = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Stroke, IsAntialias = true, StrokeWidth = 1 };
        static SKFont SanSerifBold12 = new SKFont
        {
            Typeface = SKTypeface.FromFamilyName("SanSerif", SKFontStyle.Bold),
            Size = 16,
            Edging = SKFontEdging.Antialias
        };
        static SKPaint drawFont12b = new SKPaint(SanSerifBold12){ 
            StrokeWidth = 1, 
            Color = SKColors.White
        };
        //private static SKFont drawFont12b = new SKFont(SKTypeface.FromFamilyName("SanSerif"), 12); // ccc FontStyle.Bold
        // ccc private static Pen thinWhitePen = new Pen(Color.White, 1);

        public static void Main(string[] args)
        {
            try
            {
                // args = new string[] { "-jobfile", @"c:\temp\jobfile", "-debug" };
                // args = new string[] { @"-jobfile c:\temp\tiles\05f1e9ff-5b20-4548-9e75-a1b6458b7d5f -debug" };
                /*
                Console.WriteLine("Wait for debugger to attach");

                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(100);
                }*/

                // Console.WriteLine("Start OSMMapGenerator args: " + args.Length);

                if (ParseCmdLineArgs(args))
                {
                    if (debug) { Console.WriteLine("OSMMapGenerator - Job: " + jobfile); }
                    try
                    {
                        string json = File.ReadAllText(jobfile);

                        if (!System.Diagnostics.Debugger.IsAttached)
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

                            if (debug) { Console.WriteLine($"width: {width} - height: {height} - zoom: {zoom} - tileSize: {tileSize}"); }

                            MapMode mapmode = (MapMode)Enum.ToObject(typeof(MapMode), job["mapmode"]);
                            MapCachePath = job["MapCachePath"].ToString();
                            if (System.Diagnostics.Debugger.IsAttached)
                                MapCachePath = "map-data";

                            if (debug) { Console.WriteLine($"OSMMapGenerator - DrawMap(width:{width}, height:{height}, zoom:{zoom}, x_center:{x_center}, y_center:{y_center}, mapmode:{mapmode})"); }
                            SKBitmap map = DrawMap(width, height, zoom, x_center, y_center, mapmode);
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
                else
                    Console.WriteLine("No jobfile args found");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

            }
        }

        public static void SaveImage(SKBitmap image, string filename)
        {
            try
            {
                // ccc image.Save(filename);

                if (System.Diagnostics.Debugger.IsAttached)
                {
                    int pos = filename.IndexOf("maps");
                    if ( pos > 0)
                        filename = filename.Substring(pos);
                }

                SKImage i = SKImage.FromBitmap(image);
                using (SKData d = i.Encode(SKEncodedImageFormat.Png, 90))
                using (Stream sw = File.OpenWrite(filename))
                { 
                    d.SaveTo(sw);
                }
                
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
                    // Console.WriteLine($"args {i}: " + args[i]);

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
            catch (Exception ex) { 
                Console.WriteLine(ex.ToString());
            }
            return jobfilefound;
        }

        private static SKBitmap DrawMap(int width, int height, int zoom, double x_center, double y_center, MapMode mode)
        {
            SKBitmap image = new SKBitmap(width, height);
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

        private static void DrawMapLayer(SKBitmap image, int width, int height, double x_center, double y_center, int zoom)
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
                using (SKBitmap tileImage = DownloadTile(tile.Item3, tile.Item4, tile.Item5))
                {
                    if (tileImage != null)
                    {

                        SKRectI box = new SKRectI(XtoPx(tile.Item1, x_center, width), YtoPx(tile.Item2, y_center, height), tileSize, tileSize);
                        CopyRegionIntoImage(tileImage, new SKRectI(0, 0, tileSize, tileSize), image, box);
                    }
                }
            }
        }

        static int debugCounter = 1;
        private static void CopyRegionIntoImage(SKBitmap srcBitmap, SKRectI srcRegion, SKBitmap destBitmap, SKRectI destRegion)
        {
            // https://github.com/mono/SkiaSharp/issues/1934

            if (debug) Console.WriteLine("Step1 - src: " + srcRegion.ToString() + " - dest: " + destRegion.ToString());

            int srcX = destRegion.Left < 0 ? Math.Abs(destRegion.Left) : 0;
            int srcY = destRegion.Top < 0 ? Math.Abs(destRegion.Top) : 0;
            int srcW = srcRegion.Width - Math.Abs(destRegion.Left);
            int srcH = srcRegion.Height - Math.Abs(destRegion.Top);
            int destX = destRegion.Left < 0 ? 0 : destRegion.Left;
            int destY = destRegion.Top < 0 ? 0 : destRegion.Top;
            int destW = srcW;
            int destH = srcH;
            SKRectI src = new SKRectI(srcX, srcY, srcX + srcW, srcY + srcH);
            SKRectI dest = new SKRectI(destX, destY, destW, destH);

            if (debug) Console.WriteLine("Step2 - src: " + src.ToString() + " - dest: " + dest.ToString());
            
            //cut
            SKBitmap cutSrcBitmap = new SKBitmap(src.Width, src.Height);
            srcBitmap.ExtractSubset(cutSrcBitmap, src);

            // if (debug) SaveImage(cutSrcBitmap, $"maps/debug-cut-{debugCounter}.png");

            using var canvas = new SKCanvas(destBitmap);
            canvas.DrawBitmap(cutSrcBitmap, (float)destX, (float)destY);
            canvas.Flush();

            // if (debug) SaveImage(destBitmap, $"maps/debug-{debugCounter}.png");
            debugCounter++;
            /*
            using (Graphics grD = Graphics.FromImage(destBitmap))
            {
                grD.DrawImage(srcBitmap, dest, src, GraphicsUnit.Pixel);
                grD.Dispose();
            }*/
        }
        



        private static void ApplyDarkMode(SKBitmap image)
        {
            bool savePic = false;
            if (savePic) SaveImage(image, "maps/1 - Original.png");
            FilterForest(image);
            if (savePic) SaveImage(image, "maps/1.1 - FilterForest.png");
            AdjustBrightness(image, 0.6f); //0.6
            if (savePic) SaveImage(image, "maps/2 - AdjustBrightness 0.6f.png");
            InvertImage(image);
            if (savePic) SaveImage(image, "maps/3 - InvertImage.png");
            AdjustContrast(image, 1.3f);
            if (savePic) SaveImage(image, "maps/4 - AdjustContrast.png");
            HueRotate(image, 200);
            if (savePic) SaveImage(image, "maps/5 - HueRotate.png");
            AdjustSaturation(image, 0.3f);
            if (savePic) SaveImage(image, "maps/6 - AdjustSaturation.png");
            AdjustBrightness(image, 0.7f);
            if (savePic) SaveImage(image, "maps/7 - AdjustBrightness.png");
            AdjustContrast(image, 1.3f);
            if (savePic) SaveImage(image, "maps/8 - AdjustContrast.png");
        }

        public static void AdjustBrightness(SKBitmap image, float amount)
        {
            SKCanvas canvas = new SKCanvas(image);
            using (SKPaint paint = new SKPaint())
            {
                var cf = SKColorFilter.CreateColorMatrix(new float[]
                {
                    amount, 0, 0, 0, 0,
                    0, amount, 0, 0, 0,
                    0, 0, amount, 0, 0,
                    0, 0, 0, 1, 0
                });
                paint.ColorFilter = cf;
                canvas.DrawBitmap(image, 0, 0, paint);
            }
        }



        /*
        // https://github.com/JimBobSquarePants/ImageProcessor/blob/release/3.0.0/src/ImageProcessor/Processing/KnownColorMatrices.cs
        private static void AdjustBrightness(SKBitmap image, float amount)
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
        } */


        static SKColor cNeutral = new SKColor(224, 223, 223);

        
        private static void FilterForest(SKBitmap image)
        {
            for (int y = 0; (y <= (image.Height - 1)); y++)
            {
                for (int x = 0; (x <= (image.Width - 1)); x++)
                {
                    SKColor inv = image.GetPixel(x, y);

                    if (
                        (inv.Red == 233 && inv.Green == 238 && inv.Blue == 214) ||
                        (inv.Red == 235 && inv.Green == 219 && inv.Blue == 232) ||
                        (inv.Red == 236 && inv.Green == 236 && inv.Blue == 228) ||
                        (inv.Red == 237 && inv.Green == 242 && inv.Blue == 219) ||
                        (inv.Red == 238 && inv.Green == 240 && inv.Blue == 213) ||
                        (inv.Red == 239 && inv.Green == 226 && inv.Blue == 237) ||
                        (inv.Red == 241 && inv.Green == 243 && inv.Blue == 221) ||
                        (inv.Red == 242 && inv.Green == 238 && inv.Blue == 233) ||
                        (inv.Red == 242 && inv.Green == 239 && inv.Blue == 233)
                        )
                        image.SetPixel(x, y, cNeutral);
                    else if ( // Dreiecke
                            (inv.Red == 208 && inv.Green == 144 && inv.Blue == 86)
                        )
                        image.SetPixel(x, y, cNeutral);
                    else if (
                        (inv.Red == 149 && inv.Green == 179 && inv.Blue == 139) ||
                        (inv.Red == 150 && inv.Green == 184 && inv.Blue == 136) ||
                        (inv.Red == 150 && inv.Green == 185 && inv.Blue == 136) ||
                        (inv.Red == 153 && inv.Green == 197 && inv.Blue == 141) ||
                        (inv.Red == 154 && inv.Green == 198 && inv.Blue == 142) ||
                        (inv.Red == 156 && inv.Green == 192 && inv.Blue == 142) ||
                        (inv.Red == 167 && inv.Green == 169 && inv.Blue == 123) ||
                        (inv.Red == 167 && inv.Green == 171 && inv.Blue == 103) ||
                        (inv.Red == 167 && inv.Green == 197 && inv.Blue == 139) ||
                        (inv.Red == 167 && inv.Green == 201 && inv.Blue == 153) ||
                        (inv.Red == 167 && inv.Green == 202 && inv.Blue == 152) ||
                        (inv.Red == 167 && inv.Green == 202 && inv.Blue == 153) ||
                        (inv.Red == 167 && inv.Green == 210 && inv.Blue == 159) ||
                        (inv.Red == 173 && inv.Green == 153 && inv.Blue == 68) ||
                        (inv.Red == 173 && inv.Green == 209 && inv.Blue == 158) ||
                        (inv.Red == 174 && inv.Green == 205 && inv.Blue == 162) ||
                        (inv.Red == 174 && inv.Green == 206 && inv.Blue == 161) ||
                        (inv.Red == 174 && inv.Green == 210 && inv.Blue == 161) ||
                        (inv.Red == 174 && inv.Green == 216 && inv.Blue == 162) ||
                        (inv.Red == 174 && inv.Green == 223 && inv.Blue == 163) ||
                        (inv.Red == 174 && inv.Green == 221 && inv.Blue == 161) ||
                        (inv.Red == 174 && inv.Green == 222 && inv.Blue == 163) ||
                        (inv.Red == 176 && inv.Green == 190 && inv.Blue == 147) ||
                        (inv.Red == 176 && inv.Green == 210 && inv.Blue == 159) ||
                        (inv.Red == 177 && inv.Green == 221 && inv.Blue == 165) ||
                        (inv.Red == 178 && inv.Green == 211 && inv.Blue == 164) ||
                        (inv.Red == 179 && inv.Green == 188 && inv.Blue == 148) ||
                        (inv.Red == 179 && inv.Green == 189 && inv.Blue == 148) ||
                        (inv.Red == 179 && inv.Green == 193 && inv.Blue == 151) ||
                        (inv.Red == 180 && inv.Green == 209 && inv.Blue == 157) ||
                        (inv.Red == 180 && inv.Green == 222 && inv.Blue == 170) ||
                        (inv.Red == 181 && inv.Green == 170 && inv.Blue == 103) ||
                        (inv.Red == 181 && inv.Green == 170 && inv.Blue == 108) ||
                        (inv.Red == 181 && inv.Green == 174 && inv.Blue == 117) ||
                        (inv.Red == 181 && inv.Green == 185 && inv.Blue == 106) ||
                        (inv.Red == 181 && inv.Green == 225 && inv.Blue == 169) ||
                        (inv.Red == 182 && inv.Green == 185 && inv.Blue == 118) ||
                        (inv.Red == 182 && inv.Green == 186 && inv.Blue == 148) ||
                        (inv.Red == 182 && inv.Green == 212 && inv.Blue == 169) ||
                        (inv.Red == 183 && inv.Green == 185 && inv.Blue == 135) ||
                        (inv.Red == 183 && inv.Green == 198 && inv.Blue == 154) ||
                        (inv.Red == 183 && inv.Green == 214 && inv.Blue == 165) ||
                        (inv.Red == 185 && inv.Green == 197 && inv.Blue == 136) ||
                        (inv.Red == 185 && inv.Green == 198 && inv.Blue == 142) ||
                        (inv.Red == 186 && inv.Green == 196 && inv.Blue == 116) ||
                        (inv.Red == 186 && inv.Green == 201 && inv.Blue == 157) ||
                        (inv.Red == 186 && inv.Green == 212 && inv.Blue == 165) ||
                        (inv.Red == 186 && inv.Green == 213 && inv.Blue == 173) ||
                        (inv.Red == 187 && inv.Green == 212 && inv.Blue == 165) ||
                        (inv.Red == 187 && inv.Green == 214 && inv.Blue == 182) ||
                        (inv.Red == 188 && inv.Green == 213 && inv.Blue == 181) ||
                        (inv.Red == 189 && inv.Green == 218 && inv.Blue == 177) ||
                        (inv.Red == 190 && inv.Green == 229 && inv.Blue == 181) ||
                        (inv.Red == 195 && inv.Green == 199 && inv.Blue == 137) ||
                        (inv.Red == 195 && inv.Green == 213 && inv.Blue == 181) ||
                        (inv.Red == 195 && inv.Green == 218 && inv.Blue == 180) ||
                        (inv.Red == 196 && inv.Green == 201 && inv.Blue == 153) ||
                        (inv.Red == 198 && inv.Green == 207 && inv.Blue == 173) ||
                        (inv.Red == 198 && inv.Green == 216 && inv.Blue == 153) ||
                        (inv.Red == 200 && inv.Green == 215 && inv.Blue == 171) ||
                        (inv.Red == 200 && inv.Green == 228 && inv.Blue == 173) ||
                        (inv.Red == 200 && inv.Green == 230 && inv.Blue == 172) ||
                        (inv.Red == 200 && inv.Green == 250 && inv.Blue == 204) ||
                        (inv.Red == 201 && inv.Green == 217 && inv.Blue == 184) ||
                        (inv.Red == 201 && inv.Green == 225 && inv.Blue == 191) ||
                        (inv.Red == 201 && inv.Green == 249 && inv.Blue == 204) ||
                        (inv.Red == 202 && inv.Green == 225 && inv.Blue == 190) ||
                        (inv.Red == 202 && inv.Green == 225 && inv.Blue == 193) ||
                        (inv.Red == 203 && inv.Green == 228 && inv.Blue == 178) ||
                        (inv.Red == 204 && inv.Green == 213 && inv.Blue == 200) ||
                        (inv.Red == 204 && inv.Green == 215 && inv.Blue == 178) ||
                        (inv.Red == 204 && inv.Green == 218 && inv.Blue == 179) ||
                        (inv.Red == 205 && inv.Green == 214 && inv.Blue == 179) ||
                        (inv.Red == 205 && inv.Green == 216 && inv.Blue == 179) ||
                        (inv.Red == 205 && inv.Green == 235 && inv.Blue == 176) ||
                        (inv.Red == 207 && inv.Green == 216 && inv.Blue == 184) ||
                        (inv.Red == 207 && inv.Green == 233 && inv.Blue == 184) ||
                        (inv.Red == 207 && inv.Green == 233 && inv.Blue == 185) ||
                        (inv.Red == 209 && inv.Green == 216 && inv.Blue == 187) ||
                        (inv.Red == 210 && inv.Green == 217 && inv.Blue == 186) ||
                        (inv.Red == 210 && inv.Green == 234 && inv.Blue == 186) ||
                        (inv.Red == 211 && inv.Green == 219 && inv.Blue == 196) ||
                        (inv.Red == 211 && inv.Green == 220 && inv.Blue == 203) ||
                        (inv.Red == 214 && inv.Green == 237 && inv.Blue == 193) ||
                        (inv.Red == 215 && inv.Green == 230 && inv.Blue == 200) ||
                        (inv.Red == 215 && inv.Green == 238 && inv.Blue == 192) ||
                        (inv.Red == 217 && inv.Green == 238 && inv.Blue == 196) ||
                        (inv.Red == 220 && inv.Green == 244 && inv.Blue == 196) ||
                        (inv.Red == 222 && inv.Green == 237 && inv.Blue == 205) ||
                        (inv.Red == 223 && inv.Green == 250 && inv.Blue == 226) ||
                        (inv.Red == 223 && inv.Green == 251 && inv.Blue == 226) ||
                        (inv.Red == 223 && inv.Green == 252 && inv.Blue == 226) ||
                        (inv.Red == 238 && inv.Green == 240 && inv.Blue == 213) 
                        )
                        image.SetPixel(x, y, cNeutral);
                }
            }
        }
        

        private static void InvertImage(SKBitmap image)
        {
            SKCanvas canvas = new SKCanvas(image);
            using (SKPaint paint = new SKPaint())
            {
                var cf = SKColorFilter.CreateColorMatrix(new float[]
                {
                    -1f,  0f,  0f, 0f, 1f,
                    0f, -1f,  0f, 0f, 1f,
                    0f,  0f, -1f, 0f, 1f,
                    0f,  0f,  0f, 1f, 0f
                });
                paint.ColorFilter = cf;
                canvas.DrawBitmap(image, 0, 0, paint);
            }
        }


        // https://mariusbancila.ro/blog/2009/11/13/using-colormatrix-for-creating-negative-image/
        /*
        private static void InvertImage(SKBitmap image)
        {
            /* ccc
            for (int y = 0; (y <= (image.Height - 1)); y++)
            {
                for (int x = 0; (x <= (image.Width - 1)); x++)
                {
                    SKColor inv = image.GetPixel(x, y);
                    inv = SKColor.FromArgb(255, (255 - inv.Red), (255 - inv.Green), (255 - inv.Blue));
                    image.SetPixel(x, y, inv);
                }
            }
            */

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

    }*/

        public static void AdjustContrast(SKBitmap image, float amount)
        {
            float contrast = (-.5F * amount) + .5F;

            SKCanvas canvas = new SKCanvas(image);
            using (SKPaint paint = new SKPaint())
            {

                float averageLuminance = 0.5f * (1 - amount);

                var cf = SKColorFilter.CreateColorMatrix(new float[]
                {
                amount, 0, 0, 0, contrast,
                0, amount, 0, 0, contrast,
                0, 0, amount, 0, contrast,
                0, 0, 0, 1, 0
                });
                paint.ColorFilter = cf;
                canvas.DrawBitmap(image, 0, 0, paint);
            }
        }

        // https://github.com/JimBobSquarePants/ImageProcessor/blob/release/3.0.0/src/ImageProcessor/Processing/KnownColorMatrices.cs
        /*
        private static void AdjustContrast(SKBitmap image, float amount)
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
        } */

        // https://github.com/JimBobSquarePants/ImageProcessor/blob/release/3.0.0/src/ImageProcessor/Processing/KnownColorMatrices.cs
        private static void AdjustSaturation(SKBitmap image, float amount)
        {
            using (var canvas = new SKCanvas(image))
            {
                var paint = new SKPaint();

                // Farbmatrix zur Anpassung der Sättigung
                float saturation = amount;
                float invSat = 1 - saturation;
                float R = 0.213f * invSat;
                float G = 0.715f * invSat;
                float B = 0.072f * invSat;

                float[] colorMatrix = new float[]
                {
                    R + saturation, G, B, 0, 0,
                    R, G + saturation, B, 0, 0,
                    R, G, B + saturation, 0, 0,
                    0, 0, 0, 1, 0
                };

                var colorFilter = SKColorFilter.CreateColorMatrix(colorMatrix);
                paint.ColorFilter = colorFilter;

                canvas.DrawBitmap(image, 0, 0, paint);
            }
        }

        private static void HueRotate(SKBitmap image, float degrees)
        {
            using (var canvas = new SKCanvas(image))
            {
                // Convert degrees to radians
                float radians = degrees * (float)Math.PI / 180f;

                // Create a color matrix for hue rotation
                float cosHue = (float)Math.Cos(radians);
                float sinHue = (float)Math.Sin(radians);
                float[] hueRotateMatrix = {
                        (float)(0.213 + cosHue * 0.787 - sinHue * 0.213),
                        (float)(0.715 - cosHue * 0.715 - sinHue * 0.715),
                        (float)(0.072 - cosHue * 0.072 + sinHue * 0.928), 0, 0,
                        (float)(0.213 - cosHue * 0.213 + sinHue * 0.143),
                        (float)(0.715 + cosHue * 0.285 + sinHue * 0.140),
                        (float)(0.072 - cosHue * 0.072 - sinHue * 0.283), 0, 0,
                        (float)(0.213 - cosHue * 0.213 - sinHue * 0.787),
                        (float)(0.715 - cosHue * 0.715 + sinHue * 0.715),
                        (float)(0.072 + cosHue * 0.928 + sinHue * 0.072), 0, 0,
                        0, 0, 0, 1, 0
                    };

                // Create a color filter with the hue rotation matrix
                using (var colorFilter = SKColorFilter.CreateColorMatrix(hueRotateMatrix))
                {
                    // Create a paint object with the color filter
                    using (var paint = new SKPaint())
                    {
                        paint.ColorFilter = colorFilter;

                        // Draw the image with the hue rotation applied
                        canvas.DrawBitmap(image, 0, 0, paint);
                    }
                }
            }
        }




        // https://github.com/JimBobSquarePants/ImageProcessor/blob/release/3.0.0/src/ImageProcessor/Processing/KnownColorMatrices.cs
        /*
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
        */

        private static SKBitmap DownloadTile(int zoom, int tile_x, int tile_y)
        {
            string localMapCacheFilePath = Path.Combine(MapCachePath, $"{zoom}_{tile_x}_{tile_y}.png");
            if (debug) Console.WriteLine("localMapCacheFilePath:" + localMapCacheFilePath);
           
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
                    if (debug) Console.WriteLine("Download:" + url);
                    try
                    {
                        using (var wc = new WebClient())
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
                using (SKImage img = SKImage.FromEncodedData(localMapCacheFilePath))
                {
                    return SKBitmap.FromImage(img);
                }
            }
            catch (Exception)
            {
                return new SKBitmap(tileSize, tileSize);
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

        private static void DrawAttribution(SKBitmap image)
        {
            /* ccc
            using (Graphics g = Graphics.FromImage(image))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                string attribution = "©OSM";
                SizeF size = g.MeasureString(attribution, drawFont8);
                g.FillRectangle(fillBrush, new Rectangle((int)(image.Width - size.Width - 3), (int)(image.Height - size.Height - 3), (int)(size.Width + 6), (int)(size.Height + 6)));
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.DrawString(attribution, drawFont8, grayBrush, image.Width - size.Width - 2, image.Height - size.Height - 2);
            }
            */
        }

        private static void DrawTrip(SKBitmap image, DataTable coords, int zoom, double x_center, double y_center)
        {
            // ccc using (Graphics graphics = Graphics.FromImage(image))
            
            using var canvas = new SKCanvas(image);
            {
                // ccc graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // draw Trip line
                
                for (int index = 1; index < coords.Rows.Count; index++)
                {
                    int x1 = XtoPx(LngToTileX(Convert.ToDouble(coords.Rows[index - 1]["lng"]), zoom), x_center, image.Width);
                    int y1 = YtoPx(LatToTileY(Convert.ToDouble(coords.Rows[index - 1]["lat"]), zoom), y_center, image.Height);
                    int x2 = XtoPx(LngToTileX(Convert.ToDouble(coords.Rows[index]["lng"]), zoom), x_center, image.Width);
                    int y2 = YtoPx(LatToTileY(Convert.ToDouble(coords.Rows[index]["lat"]), zoom), y_center, image.Height);
                    if (x1 != x2 || y1 != y2)
                    {
                        canvas.DrawLine(x1, y1, x2, y2, bluePen);
                    }
                }
            }
            canvas.Flush();
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

        private static void DrawIcon(SKBitmap image, double lat, double lng, MapIcon icon, int zoom, double x_center, double y_center)
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
            int left = x - 4 * scale;
            int top = y - 10 * scale;
            SKRectI rect = new SKRectI(left, top, left + 8 * scale, top + 8 * scale);
            SKPoint[] triangle = new SKPoint[] { new SKPoint(x - 4 * scale, y - 6 * scale), new SKPoint(x, y), new SKPoint(x + 4 * scale, y - 6 * scale) };
            //using (Graphics g = Graphics.FromImage(image))
            using var g = new SKCanvas(image);
            {
                // ccc g.SmoothingMode = SmoothingMode.AntiAlias;
                // ccc g.PixelOffsetMode = PixelOffsetMode.Half;
                 
                switch (icon)
                {
                    case MapIcon.Start:
                        using (SKPath path = new SKPath())
                        {
                            path.ArcTo(rect, -180, 180, false);
                            path.AddPoly(triangle);
                            g.DrawPath(path, greenBrush);
                        }

                        //g.FillPie(greenBrush, rect, 180, 180);
                        //g.FillPolygon(greenBrush, triangle);
                        break;
                    case MapIcon.End:
                        using (SKPath path = new SKPath())
                        {
                            path.ArcTo(rect, -180, 180, false);
                            path.AddPoly(triangle);
                            g.DrawPath(path, redBrush);
                        }

                        //g.FillPie(redBrush, rect, 180, 180);
                        //g.FillPolygon(redBrush, triangle);
                        break;
                    case MapIcon.Park:
                        using (SKPath path = new SKPath())
                        {
                            path.ArcTo(rect, -180, 180, false);
                            path.AddPoly(triangle);
                            g.DrawPath(path, blueBrush);
                        }

                        //g.FillPie(blueBrush, rect, 180, 180);
                        //g.FillPolygon(blueBrush, triangle);
                        break;
                    case MapIcon.Charge:
                        using (SKPath path = new SKPath())
                        {
                            path.ArcTo(rect, -180, 180, false);
                            path.AddPoly(triangle);
                            g.DrawPath(path, orangeBrush);
                        }

                        //g.FillPie(orangeBrush, rect, 180, 180);
                        //g.FillPolygon(orangeBrush, triangle);
                        break;
                } 
                
                g.DrawArc(rect, 180, 180, false, thinWhitePen);
                g.DrawLine( triangle[0], triangle[1], thinWhitePen);
                g.DrawLine( triangle[1], triangle[2], thinWhitePen);
                if (icon == MapIcon.Park || icon == MapIcon.Charge)
                {
                    
                    string text = icon == MapIcon.Park ? "P" : "C";
                    float size = drawFont12b.MeasureText(text);
                    // ccc g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    //g.DrawText(text, x - size / 2, y - 6 * scale - size / 2, drawFont12b);
                    g.DrawText(text, x - size / 2, y - scale - rect.Height / 2 , drawFont12b);
                    g.Dispose();
                }

                // g.Flush();
            }
        }
    }
}
