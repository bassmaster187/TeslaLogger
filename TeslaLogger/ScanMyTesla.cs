using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;
using Exceptionless;
using Newtonsoft.Json;
using System.Reflection;
using System.Net.Http.Headers;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    public class ScanMyTesla
    {
        private string token;
        private Thread thread;
        private bool fastmode; // defaults to false
        private bool run = true;
        internal HttpClient httpclient_teslalogger_de;
        Car car;

        DateTime lastScanMyTeslaActive = DateTime.MinValue;

        internal ScanMyTesla(Car c)
        {
            if (c != null)
            {
                token = c.TaskerHash;
                car = c;

                thread = new Thread(new ThreadStart(Start));
                thread.Name = "ScanMyTesla_" + car.CarInDB;
                thread.Start();
            }
        }

        public void FastMode(bool fast)
        {
            car.Log($"ScanMyTesla FastMode: {fast}");
            fastmode = fast;
        }

        private void Start()
        {
            if (!Tools.UseScanMyTesla())
            {
                return;
            }

            httpclient_teslalogger_de = new HttpClient();
            httpclient_teslalogger_de.DefaultRequestHeaders.ConnectionClose = true;

            ProductInfoHeaderValue userAgent = new ProductInfoHeaderValue("Teslalogger", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            httpclient_teslalogger_de.DefaultRequestHeaders.UserAgent.Add(userAgent);
            httpclient_teslalogger_de.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(" + car.TaskerHash + "; " + Thread.CurrentThread.ManagedThreadId + ")"));

            car.Log("Start ScanMyTesla Thread!");

            string response = "";

            while (run)
            {
                try
                {
                    System.Threading.Thread.Sleep(5000);

                    if (!fastmode && response == "not found")
                    {
                        for (int s = 0; s < 300; s++)
                        {
                            if (fastmode)
                            {
                                break;
                            }

                            System.Threading.Thread.Sleep(100);
                        }
                    }

                    response = GetDataFromWebservice().Result;
                    if (response.StartsWith("not found", StringComparison.Ordinal)
                        || response.StartsWith("ERROR:", StringComparison.Ordinal)
                        || response.Contains("Resource Limit Is Reached"))
                    {
                        System.Threading.Thread.Sleep(5000);
                    }
                    else
                    {
                        InsertData(response);
                    }
                }
                catch (Exception ex)
                {
                    car.CreateExceptionlessClient(ex).Submit();
                    car.Log("Scanmytesla: " + ex.Message);
                    Logfile.WriteException(ex.ToString());
                    System.Threading.Thread.Sleep(20000);
                }
            }
        }

        private void InsertData(string response)
        {
            car.Log("ScanMyTesla: " + response);
        }

        public async Task<string> GetDataFromWebservice()
        {
            string resultContent = "";
            try
            {
                using (FormUrlEncodedContent content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("t", token)
                }))
                {

                    DateTime start = DateTime.UtcNow;
                    HttpResponseMessage result = await httpclient_teslalogger_de.PostAsync(new Uri("http://teslalogger.de/get_scanmytesla.php"), content).ConfigureAwait(true);

                    if (result.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                    {
                        car.CreateExeptionlessLog("ScanMyTesla", "GetDataFromWebservice Error Service Unavailable (503)", Exceptionless.Logging.LogLevel.Warn).Submit();
                        car.Log("SMT: Error Service Unavailable (503)");
                        System.Threading.Thread.Sleep(25000);
                        return "ERROR: 503";
                    }

                    resultContent = await result.Content.ReadAsStringAsync().ConfigureAwait(true);

                    DBHelper.AddMothershipDataToDB("teslalogger.de/get_scanmytesla.php", start, (int)result.StatusCode);

                    if (resultContent == "not found")
                    {
                        return "not found";
                    }

                    if (resultContent.Contains("Connect failed: Too many connections"))
                    {
                        car.CreateExeptionlessLog("ScanMyTesla", "Too many connections", Exceptionless.Logging.LogLevel.Warn).Submit();

                        car.Log("SMT: Too many connections");
                        Thread.Sleep(25000);
                        return "Resource Limit Is Reached";
                    }

                    if (resultContent.Contains("Resource Limit Is Reached"))
                    {
                        car.CreateExeptionlessLog("ScanMyTesla", "Resource Limit Is Reached", Exceptionless.Logging.LogLevel.Warn).Submit();

                        car.Log("SMT: Resource Limit Is Reached");
                        Thread.Sleep(25000);
                        return "Resource Limit Is Reached";
                    }

                    var diff = DateTime.UtcNow - lastScanMyTeslaActive;
                    if (diff.TotalMinutes > 60)
                    {
                        car.CreateExeptionlessFeature("ScanMyTeslaActive").Submit();
                        lastScanMyTeslaActive = DateTime.UtcNow;
                    }

                    string temp = resultContent;
                    int i = 0;
                    i = temp.IndexOf("\r\n", StringComparison.Ordinal);
                    string id = temp.Substring(0, i);

                    temp = temp.Substring(i + 2);

                    i = temp.IndexOf("\r\n", StringComparison.Ordinal);
                    string date = temp.Substring(0, i);
                    temp = temp.Substring(i + 2);

                    dynamic j = JsonConvert.DeserializeObject(temp);
                    DateTime d = DateTime.Parse(j["d"].ToString());
                    car.CurrentJSON.lastScanMyTeslaReceived = d;
                    car.CurrentJSON.CreateCurrentJSON();

                    Dictionary<string, object> kv = j["dict"].ToObject<Dictionary<string, object>>();

                    StringBuilder sb = new StringBuilder();
                    sb.Append("INSERT INTO `can` (`datum`, `id`, `val`, CarId) VALUES ");
                    bool first = true;

                    string sqlDate = d.ToString("yyyy-MM-dd HH:mm:ss", Tools.ciEnUS);

                    foreach (KeyValuePair<string, object> line in kv)
                    {
                        if (line.Value.ToString().Contains("Infinity") || line.Value.ToString().Contains("NaN"))
                        {
                            continue;
                        }

                        switch (line.Key)
                        {
                            case "2":
                                car.CurrentJSON.SMTCellTempAvg = Convert.ToDouble(line.Value, Tools.ciEnUS);
                                break;
                            case "5":
                                car.CurrentJSON.SMTCellMinV = Convert.ToDouble(line.Value, Tools.ciEnUS);
                                break;
                            case "6":
                                car.CurrentJSON.SMTCellAvgV = Convert.ToDouble(line.Value, Tools.ciEnUS);
                                break;
                            case "7":
                                car.CurrentJSON.SMTCellMaxV = Convert.ToDouble(line.Value, Tools.ciEnUS);
                                break;
                            case "9":
                                car.CurrentJSON.SMTACChargeTotal = Convert.ToDouble(line.Value, Tools.ciEnUS);
                                break;
                            case "11":
                                car.CurrentJSON.SMTDCChargeTotal = Convert.ToDouble(line.Value, Tools.ciEnUS);
                                break;
                            case "27":
                                car.CurrentJSON.SMTCellImbalance = Convert.ToDouble(line.Value, Tools.ciEnUS);
                                break;
                            case "28":
                                car.CurrentJSON.SMTBMSmaxCharge = Convert.ToDouble(line.Value, Tools.ciEnUS);
                                break;
                            case "29":
                                car.CurrentJSON.SMTBMSmaxDischarge = Convert.ToDouble(line.Value, Tools.ciEnUS);
                                break;
                            case "442":
                                if (Convert.ToDouble(line.Value, Tools.ciEnUS) == 287.6) // SNA - Signal not Available
                                {
                                    car.CurrentJSON.SMTSpeed = 0;
                                    car.Log("SMT Speed: Signal not Available");
                                }
                                else
                                {
                                    car.CurrentJSON.SMTSpeed = Convert.ToDouble(line.Value, Tools.ciEnUS);
                                }
                                break;
                            case "43":
                                car.CurrentJSON.SMTBatteryPower = Convert.ToDouble(line.Value, Tools.ciEnUS);
                                break;
                            case "71":
                                car.CurrentJSON.SMTNominalFullPack = Convert.ToDouble(line.Value, Tools.ciEnUS);
                                break;
                            default:
                                break;
                        }


                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(",");
                        }

                        sb.Append("('");
                        sb.Append(sqlDate);
                        sb.Append("',");
                        sb.Append(line.Key);
                        sb.Append(",");
                        sb.Append(Convert.ToDouble(line.Value, Tools.ciEnUS).ToString(Tools.ciEnUS));
                        sb.Append(",");
                        sb.Append(car.CarInDB);
                        sb.Append(")");
                    }

                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();
#pragma warning disable CA2100 // SQL-Abfragen auf Sicherheitsrisiken überprüfen
                        using (MySqlCommand cmd = new MySqlCommand(sb.ToString(), con))
#pragma warning restore CA2100 // SQL-Abfragen auf Sicherheitsrisiken überprüfen
                        {
                            try
                            {
                                SQLTracer.TraceNQ(cmd);
                            }
                            catch (MySqlException ex)
                            {
                                if (ex.Message.Contains("Duplicate entry"))
                                    car.Log("Scanmytesla: " + ex.Message);
                                else
                                    throw;
                            }

                            try
                            {
                                using (MySqlConnection con2 = new MySqlConnection(DBHelper.DBConnectionstring))
                                {
                                    con2.Open();
                                    using (MySqlCommand cmd2 = new MySqlCommand("update cars set lastscanmytesla=@lastscanmytesla where id=@id", con2))
                                    {
                                        cmd2.Parameters.AddWithValue("@id", car.CarInDB);
                                        cmd2.Parameters.AddWithValue("@lastscanmytesla", DateTime.Now);
                                        SQLTracer.TraceNQ(cmd2);
                                    }
                                }
                            }
                            catch (Exception)
                            { }

                            return "insert ok [" + kv.Keys.Count + "] " + d.ToString(Tools.ciEnUS);
                        }
                    }
                }
                
            }
            catch (TaskCanceledException)
            {
                car.CreateExeptionlessLog("ScanMyTesla", "Timeout", Exceptionless.Logging.LogLevel.Warn).Submit();
                car.Log("Scanmytesla: Timeout");
                System.Threading.Thread.Sleep(60000);
            }
            catch (Exception ex)
            {
                if (!WebHelper.FilterNetworkoutage(ex))
                    car.CreateExceptionlessClient(ex).AddObject(resultContent, "ResultContent").Submit();

                Logfile.ExceptionWriter(ex, resultContent);
                Thread.Sleep(10000);
            }

            return "NULL";
        }

        public void StopThread()
        {
            run = false;
        }

        public void KillThread()
        {
            try
            {
                thread?.Abort();
                thread = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.ToString());
            }
        }
    }
}
