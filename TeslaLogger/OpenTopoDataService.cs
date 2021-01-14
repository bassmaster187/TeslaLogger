using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Web.Script.Serialization;
using System.Collections.Generic;

namespace TeslaLogger
{
    public class OpenTopoDataService
    {

        private static OpenTopoDataService _OpenTopoDataService = null;

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
                    string[] latlng = new string[items.Length];
                    for (int i = 0; i < items.Length; i++)
                    {
                        latlng[i] = $"{items[i].Item2},{items[i].Item3}";
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
                        object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                        if (jsonResult != null && jsonResult.GetType() == typeof(Dictionary<string, object>))
                        {
                            if (((Dictionary<string, object>)jsonResult).ContainsKey("status")
                                && ((Dictionary<string, object>)jsonResult)["status"].Equals("OK")
                                && ((Dictionary<string, object>)jsonResult).ContainsKey("results"))
                            {
                                object[] objects = (object[])((Dictionary<string, object>)jsonResult)["results"];
                                foreach (object result in objects) {
                                    if(((Dictionary<string, object>)result).ContainsKey("elevation")
                                        && ((Dictionary<string, object>)result).ContainsKey("location"))
                                    {
                                        if (double.TryParse(((Dictionary<string, object>)result)["elevation"].ToString(), out double elevation)
                                            && !double.IsNaN(elevation))
                                        {
                                            Dictionary<string, object> location = (Dictionary<string, object>)((Dictionary<string, object>)result)["location"];
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
                                                            // finally update DB
                                                            _ = DBHelper.ExecuteSQLQuery($"UPDATE pos SET altitude = {elevation} WHERE id = {item.Item1}");
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
                    Thread.Sleep(90000); // sleep 90 seconds (safety marging)
                }
            }
            else
            {
                Thread.Sleep(60000); // sleep 60 seconds
            }
        }
    }
}
