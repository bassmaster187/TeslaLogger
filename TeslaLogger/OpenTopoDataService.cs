using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Collections.Generic;
using Exceptionless;
using Newtonsoft.Json;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    public class OpenTopoDataService
    {

        private static OpenTopoDataService _OpenTopoDataService; // defaults to null;

        // store posID, lat, lng
        private readonly ConcurrentStack<Tuple<long, double, double>> queue = new ConcurrentStack<Tuple<long, double, double>>();

        private OpenTopoDataService()
        {
            Logfile.Log("OpenTopoDataService initialized");
        }

        public int GetQueueLength()
        {
            return queue.Count;
        }

        public static OpenTopoDataService GetSingleton()
        {
            if (_OpenTopoDataService == null)
            {
                _OpenTopoDataService = new OpenTopoDataService();
            }
            return _OpenTopoDataService;
        }

        public void Enqueue(long posID, double lat, double lng)
        {
            queue.Push(new Tuple<long, double, double>(posID, lat, lng));
        }

        public void Run()
        {
            try
            {
                while (true)
                {
                    // max 1000 requests per day
                    // 24hours divided by 1000 --> one request every 86,4 seconds, so try every 2 minutes (including safety margin)
                    if (DateTime.Now.Minute % 2 == 0)
                    {
                        Work();
                    }
                    else
                    {
                        Thread.Sleep(30000); // sleep 30 seconds
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("OpenTopoDataService: Exception", ex);
            }
        }

        private void Work()
        {
            // is there something in the queue?
            if (!queue.IsEmpty)
            {
                // queue contains coordinates
                // move coordinates from queue to items, max 100 as per terms of usage 2020-10-09 https://www.opentopodata.org/#public-api
                Tuple<long, double, double>[] items = new Tuple<long, double, double>[Math.Min(queue.Count, 100)];
                if (queue.TryPopRange(items) > 0)
                {
                    // build pipe separated query string for opentopodata.org
                    RequestLocations(items, UpdateDB);

                    Thread.Sleep(90000); // sleep 90 seconds (safety marging)
                }
            }
            else
            {
                Thread.Sleep(60000); // sleep 60 seconds
            }
        }
        public delegate void RequestLocationsResponse(long id, double elevation);
        void UpdateDB(long id, double elevation)
        {
            // finally update DB
            _ = DBHelper.ExecuteSQLQuery($"UPDATE pos SET altitude = {elevation} WHERE id = {id}");
        }

        internal static void RequestLocations(Tuple<long, double, double>[] items, RequestLocationsResponse response)
        {
            string[] latlng = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                latlng[i] = items[i].Item2.ToString(Tools.ciEnUS) + "," + items[i].Item3.ToString(Tools.ciEnUS);
            }
            string queryString = "https://api.opentopodata.org/v1/mapzen?locations=" + string.Join("|", latlng);
            // query opentopodata.org API
            string resultContent = string.Empty;
            using (HttpClient client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(11)
            })
            {
                client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                DateTime start = DateTime.UtcNow;
                HttpResponseMessage result = client.GetAsync(new Uri(queryString)).Result;
                resultContent = result.Content.ReadAsStringAsync().Result;
                DBHelper.AddMothershipDataToDB("OpenTopoData.Query", start, (int)result.StatusCode);
            }
            // parse result JSON
            if (!string.IsNullOrEmpty(resultContent))
            {
                dynamic jr = JsonConvert.DeserializeObject(resultContent);
                var jsonResult = jr.ToObject<Dictionary<string, object>>();
                if (jsonResult != null)
                {
                    if (((Dictionary<string, object>)jsonResult).ContainsKey("status")
                        && ((Dictionary<string, object>)jsonResult)["status"].Equals("OK")
                        && ((Dictionary<string, object>)jsonResult).ContainsKey("results"))
                    {
                        dynamic objects = jsonResult["results"];
                        foreach (dynamic result in objects)
                        {
                            if (result.ContainsKey("elevation")
                                && result.ContainsKey("location"))
                            {
                                if (double.TryParse(result["elevation"].ToString(Tools.ciEnUS), out double elevation)
                                    && !double.IsNaN(elevation))
                                {
                                    Dictionary<string, object> location = result["location"].ToObject<Dictionary<string, object>>();
                                    if (location.ContainsKey("lat") && location.ContainsKey("lng"))
                                    {
                                        if (double.TryParse(location["lat"].ToString(), out double lat)
                                            && double.TryParse(location["lng"].ToString(), out double lng))
                                        {
                                            // find posID(s) in items
                                            foreach (Tuple<long, double, double> item in items)
                                            {
                                                if (item.Item2 == lat && item.Item3 == lng)
                                                {
                                                    response(item.Item1, elevation);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
