using System;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Exceptionless;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    public class MapQuestMapProvider : StaticMapProvider
    {
        HttpClient _httpClient;
        SemaphoreSlim _httpClientLock = new SemaphoreSlim(1, 1);
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
                if (!DownloadFile(url, filename))
                    return;

                Logfile.Log("Create File: " + filename);

                System.Threading.Thread.Sleep(500);
            }
            catch (Exception ex)
            {
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
                if (!DownloadFile(url, filename))
                    return;

                Logfile.Log("Create File: " + filename);

                System.Threading.Thread.Sleep(500);
            }
            catch (Exception ex)
            {
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
                if (!DownloadFile(url, filename))
                    return;

                Logfile.Log("Create File: " + filename);
                

                System.Threading.Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().AddObject(coords?.Rows?.Count,"Coords Rows Count").Submit();

                System.Diagnostics.Debug.WriteLine(ex.ToString());
                Logfile.Log("Rows count: " + coords.Rows.Count + " posquery= " + posquery + "\r\n" + ex.ToString());
            }

        }

        private bool DownloadFile(string url, string filename)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", "TeslaLogger");
                request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");

                using (HttpResponseMessage response = GetHttpClient().SendAsync(request).GetAwaiter().GetResult())
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        if (IsInvalidAppKey(response))
                            return false;

                        response.EnsureSuccessStatusCode();
                    }

                    byte[] fileBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                    File.WriteAllBytes(filename, fileBytes);
                    return true;
                }
            }
        }

        private static bool IsInvalidAppKey(HttpResponseMessage response)
        {
            try
            {
                if (response?.StatusCode == HttpStatusCode.Forbidden)
                {
                    string responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    if (responseString == "The AppKey submitted with this request is invalid.")
                    {
                        invalidAppKey = true;
                        Logfile.Log("MapQuest: " + responseString);
                        return true;
                    }
                }
            }
            catch (Exception ex2)
            {
                Logfile.Log(ex2.ToString());
            }
            return false;
        }

        HttpClient GetHttpClient()
        {
            _httpClientLock.Wait();
            try
            {
                if (_httpClient == null)
                {
                    _httpClient = new HttpClient
                    {
                        Timeout = TimeSpan.FromMinutes(3)
                    };
                }
            }
            finally
            {
                _httpClientLock.Release();
            }

            return _httpClient;
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

