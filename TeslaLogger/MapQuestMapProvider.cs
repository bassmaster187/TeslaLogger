using System;
using System.Data;
using System.Net;
using System.Text;

namespace TeslaLogger
{
    public class MapQuestMapProvider : StaticMapProvider
    {
        public override void CreateChargingMap(double lat, double lng, int width, int height, MapMode mapmode, MapSpecial special, string filename)
        {
            if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
            {
                return;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("http://open.mapquestapi.com/staticmap/v5/map?key=");
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
            StringBuilder sb = new StringBuilder();
            sb.Append("http://open.mapquestapi.com/staticmap/v5/map?key=");
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
            // https://open.mapquestapi.com/staticmap/v5/map?key=ulMOOlevG9FunIVobQB2BG2GA0EdCjjH&boundingBox=38.915,-77.072,38.876,-77.001&size=200,150&type=dark
            Tuple<double, double, double, double> extent = DetermineExtent(coords);
            StringBuilder sb = new StringBuilder();
            sb.Append("http://open.mapquestapi.com/staticmap/v5/map?key=");
            sb.Append(ApplicationSettings.Default.MapQuestKey);
            sb.Append("&boundingBox=");
            sb.Append(extent.Item1.ToString(Tools.ciEnUS)).Append(",");
            sb.Append(extent.Item2.ToString(Tools.ciEnUS)).Append(",");
            sb.Append(extent.Item3.ToString(Tools.ciEnUS)).Append(",");
            sb.Append(extent.Item4.ToString(Tools.ciEnUS));
            sb.Append($"&size={width},{height}&type=dark");
            sb.Append("&locations=");
            sb.Append(Convert.ToDouble(coords.Rows[0]["lat"]).ToString(Tools.ciEnUS)).Append(",").Append(Convert.ToDouble(coords.Rows[0]["lng"]).ToString(Tools.ciEnUS));
            sb.Append("|marker-start||");
            sb.Append(Convert.ToDouble(coords.Rows[coords.Rows.Count - 1]["lat"]).ToString(Tools.ciEnUS)).Append(",").Append(Convert.ToDouble(coords.Rows[coords.Rows.Count - 1]["lng"]).ToString(Tools.ciEnUS));
            sb.Append("|marker-end");
            sb.Append("&shape=");
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

                sb.Append(Convert.ToDouble(dr["lat"]).ToString(Tools.ciEnUS)).Append(",").Append(Convert.ToDouble(dr["lng"]).ToString(Tools.ciEnUS));
                posquery++;
            }
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

                System.Threading.Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                Logfile.Log("Rows count: " + coords.Rows.Count + " posquery= " + posquery + "\r\n" + ex.ToString());
            }

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
    }
}
