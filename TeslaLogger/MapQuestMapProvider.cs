using System;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using Exceptionless;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    public class MapQuestMapProvider : StaticMapProvider
    {
        WebClient _webClient;
        object _webClientLock = new object();
        static bool invalidAppKey; // defaults to false;


        public override void CreateChargingMap(double lat, double lng, int width, int height, MapMode mapmode, MapSpecial special, string filename)
        {
            if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
            {
                return;
            }

            if (invalidAppKey)
            {
                Logfile.Log("Mapquest invalidAppKey!!!");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("https://www.mapquestapi.com/staticmap/v5/map?key=");
            sb.Append(ApplicationSettings.Default.MapQuestKey);
            sb.Append("&center=");
            sb.Append(lat.ToString(Tools.ciEnUS)).Append(",");
            sb.Append(lng.ToString(Tools.ciEnUS));
            sb.Append($"&size={width},{height}&type=dark");
            sb.Append("&locations=");
            sb.Append(lat.ToString(Tools.ciEnUS)).Append(",").Append(lng.ToString(Tools.ciEnUS));
            sb.Append("|marker-E3AE32|");

            string url = sb.ToString();
            System.Diagnostics.Debug.WriteLine(url);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.Headers.Add("User-Agent: TeslaLogger");
                    webClient.Headers.Add("Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");

                    // Download the Web resource and save it into the current filesystem folder.
                    webClient.DownloadFile(url, filename);

                    Logfile.Log("Create File: " + filename);
                }

                System.Threading.Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                var wex = ex as WebException;
                if (wex != null)
                {
                    if (IsInvalidAppKey(wex))
                        return;
                }

                ex.ToExceptionless().FirstCarUserID().Submit();

                System.Diagnostics.Debug.WriteLine(ex.ToString());
                Logfile.Log(ex.ToString());
            }

        }

        public override void CreateParkingMap(double lat, double lng, int width, int height, MapMode mapmode, MapSpecial special, string filename)
        {
            if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
            {
                return;
            }

            if (invalidAppKey)
            {
                Logfile.Log("Mapquest invalidAppKey!!!");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("https://www.mapquestapi.com/staticmap/v5/map?key=");
            sb.Append(ApplicationSettings.Default.MapQuestKey);
            sb.Append("&center=");
            sb.Append(lat.ToString(Tools.ciEnUS)).Append(",");
            sb.Append(lng.ToString(Tools.ciEnUS));
            sb.Append($"&size={width},{height}&type=dark");
            sb.Append("&locations=");
            sb.Append(lat.ToString(Tools.ciEnUS)).Append(",").Append(lng.ToString(Tools.ciEnUS));
            sb.Append("|marker-3E72B1|");

            string url = sb.ToString();
            System.Diagnostics.Debug.WriteLine(url);

            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.Headers.Add("User-Agent: TeslaLogger");
                    webClient.Headers.Add("Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");

                    // Download the Web resource and save it into the current filesystem folder.
                    webClient.DownloadFile(url, filename);

                    Logfile.Log("Create File: " + filename);
                }

                System.Threading.Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                var wex = ex as WebException;
                if (wex != null)
                {
                    if (IsInvalidAppKey(wex))
                        return;
                }

                ex.ToExceptionless().FirstCarUserID().Submit();

                System.Diagnostics.Debug.WriteLine(ex.ToString());
                Logfile.Log(ex.ToString());
            }

        }

        public override void CreateTripMap(DataTable coords, int width, int height, MapMode mapmode, MapSpecial special, string filename)
        {
            if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
            {
                return;
            }
            if (coords == null)
            {
                return;
            }
            if (invalidAppKey)
            {
                Logfile.Log("Mapquest invalidAppKey!!!");
                return;
            }

            // https://open.mapquestapi.com/staticmap/v5/map?key=ulMOOlevG9FunIVobQB2BG2GA0EdCjjH&boundingBox=38.915,-77.072,38.876,-77.001&size=200,150&type=dark
            Tuple<double, double, double, double> extent = DetermineExtent(coords);
            if (extent == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            _ = sb.Append("https://www.mapquestapi.com/staticmap/v5/map?key=");
            _ = sb.Append(ApplicationSettings.Default.MapQuestKey);
            _ = sb.Append("&boundingBox=");
            _ = sb.Append(extent.Item1.ToString(Tools.ciEnUS)).Append(",");
            _ = sb.Append(extent.Item2.ToString(Tools.ciEnUS)).Append(",");
            _ = sb.Append(extent.Item3.ToString(Tools.ciEnUS)).Append(",");
            _ = sb.Append(extent.Item4.ToString(Tools.ciEnUS));
            _ = sb.Append($"&size={width},{height}&type=dark");
            _ = sb.Append("&locations=");
            _ = sb.Append(Convert.ToDouble(coords.Rows[0]["lat"], Tools.ciDeDE).ToString(Tools.ciEnUS)).Append(",").Append(Convert.ToDouble(coords.Rows[0]["lng"], Tools.ciDeDE).ToString(Tools.ciEnUS));
            _ = sb.Append("|marker-start||");
            _ = sb.Append(Convert.ToDouble(coords.Rows[coords.Rows.Count - 1]["lat"], Tools.ciDeDE).ToString(Tools.ciEnUS)).Append(",").Append(Convert.ToDouble(coords.Rows[coords.Rows.Count - 1]["lng"], Tools.ciDeDE).ToString(Tools.ciEnUS));
            _ = sb.Append("|marker-end");
            _ = sb.Append("&shape=");
            bool first = true;
            int posquery = 0;

            if (coords.Rows.Count < 4)
            {
                return;
            }

            int step = coords.Rows.Count / 200;
            if (step == 0)
            {
                step = 1;
            }

            for (int pos = 0; pos < coords.Rows.Count; pos++)
            {
                DataRow dr = coords.Rows[pos];

                if (!(pos % step == 0))
                {
                    continue;
                }


                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append("|");
                }

                sb.Append(Convert.ToDouble(dr["lat"], Tools.ciDeDE).ToString(Tools.ciEnUS)).Append(",").Append(Convert.ToDouble(dr["lng"], Tools.ciDeDE).ToString(Tools.ciEnUS));
                posquery++;
            }
            string url = sb.ToString();
            System.Diagnostics.Debug.WriteLine(url);

            try
            {

                var webClient = GetWebClient();

                // Download the Web resource and save it into the current filesystem folder.
                webClient.DownloadFile(url, filename);
                Logfile.Log("Create File: " + filename);
                

                System.Threading.Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                var wex = ex as WebException;
                if (wex != null)
                {
                    if (IsInvalidAppKey(wex))
                        return;
                }

                ex.ToExceptionless().FirstCarUserID().AddObject(coords?.Rows?.Count,"Coords Rows Count").Submit();

                System.Diagnostics.Debug.WriteLine(ex.ToString());
                Logfile.Log("Rows count: " + coords.Rows.Count + " posquery= " + posquery + "\r\n" + ex.ToString());
            }

        }

        private static bool IsInvalidAppKey(WebException ex)
        {
            try
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse exh = ex.Response as HttpWebResponse;
                    if (exh != null && exh.StatusCode == HttpStatusCode.Forbidden)
                    {
                        using (Stream stream = ex.Response.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                            String responseString = reader.ReadToEnd();

                            if (responseString == "The AppKey submitted with this request is invalid.")
                            {
                                invalidAppKey = true;
                                Logfile.Log("MapQuest: " + responseString);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex2)
            {
                Logfile.Log(ex2.ToString());
            }
            return false;
        }

        WebClient GetWebClient()
        {
            lock (_webClientLock)
            {
                if (_webClient == null)
                {
                    var webClient = new MyWebClient();
                    webClient.Headers.Add("User-Agent: TeslaLogger");
                    webClient.Headers.Add("Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");

                    _webClient = webClient;
                }
            }

            return _webClient;
        }

        public override int GetDelayMS()
        {
            return 0;
        }

        public override bool UseIt()
        {
            if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
                return false;

            return true;
        }

        private class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = (int)TimeSpan.FromMinutes(3).TotalMilliseconds;
                return w;
            }
        }
    }
}
