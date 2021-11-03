using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    public class ScanMyTesla
    {
        private string token;
        private Thread thread;
        private bool fastmode = false;
        private bool run = true;
        Car car;

        public ScanMyTesla(Car c)
        {
            if (c != null)
            {
                token = c.TaskerHash;
                car = c;

                thread = new Thread(new ThreadStart(Start));
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

            car.Log("Start ScanMyTesla Thread!");

            string response = "";

            while (run)
            {
                try
                {
                    System.Threading.Thread.Sleep(2500);

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
                using (HttpClient client = new HttpClient())
                {
                    using (FormUrlEncodedContent content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("t", token)
                    }))
                    {

                        DateTime start = DateTime.UtcNow;
                        HttpResponseMessage result = await client.PostAsync("http://teslalogger.de/get_scanmytesla.php", content);
                        resultContent = await result.Content.ReadAsStringAsync();

                        DBHelper.AddMothershipDataToDB("teslalogger.de/get_scanmytesla.php", start, (int)result.StatusCode);

                        if (resultContent == "not found")
                        {
                            return "not found";
                        }

                        if (resultContent.Contains("Resource Limit Is Reached"))
                        {
                            car.Log("SMT: Resource Limit Is Reached");
                            Thread.Sleep(25000);
                            return "Resource Limit Is Reached";
                        }

                        string temp = resultContent;
                        int i = 0;
                        i = temp.IndexOf("\r\n");
                        string id = temp.Substring(0, i);

                        temp = temp.Substring(i + 2);

                        i = temp.IndexOf("\r\n");
                        string date = temp.Substring(0, i);
                        temp = temp.Substring(i + 2);

                        dynamic j = new JavaScriptSerializer().DeserializeObject(temp);
                        DateTime d = DateTime.Parse(j["d"]);
                        car.CurrentJSON.lastScanMyTeslaReceived = d;
                        car.CurrentJSON.CreateCurrentJSON();

                        Dictionary<string, object> kv = (Dictionary<string, object>)j["dict"];

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
                                    car.CurrentJSON.SMTCellTempAvg = Convert.ToDouble(line.Value);
                                    break;
                                case "5":
                                    car.CurrentJSON.SMTCellMinV = Convert.ToDouble(line.Value);
                                    break;
                                case "6":
                                    car.CurrentJSON.SMTCellAvgV = Convert.ToDouble(line.Value);
                                    break;
                                case "7":
                                    car.CurrentJSON.SMTCellMaxV = Convert.ToDouble(line.Value);
                                    break;
                                case "9":
                                    car.CurrentJSON.SMTACChargeTotal = Convert.ToDouble(line.Value);
                                    break;
                                case "11":
                                    car.CurrentJSON.SMTDCChargeTotal = Convert.ToDouble(line.Value);
                                    break;
                                case "27":
                                    car.CurrentJSON.SMTCellImbalance = Convert.ToDouble(line.Value);
                                    break;
                                case "28":
                                    car.CurrentJSON.SMTBMSmaxCharge = Convert.ToDouble(line.Value);
                                    break;
                                case "29":
                                    car.CurrentJSON.SMTBMSmaxDischarge = Convert.ToDouble(line.Value);
                                    break;
                                case "442":
                                    if (Convert.ToDouble(line.Value) == 287.6) // SNA - Signal not Available
                                    {
                                        car.CurrentJSON.SMTSpeed = 0;
                                        car.Log("SMT Speed: Signal not Available");
                                    }
                                    else
                                    {
                                        car.CurrentJSON.SMTSpeed = Convert.ToDouble(line.Value);
                                    }
                                    break;
                                case "43":
                                    car.CurrentJSON.SMTBatteryPower = Convert.ToDouble(line.Value);
                                    break;
                                case "71":
                                    car.CurrentJSON.SMTNominalFullPack = Convert.ToDouble(line.Value);
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
                            sb.Append(Convert.ToDouble(line.Value).ToString(Tools.ciEnUS));
                            sb.Append(",");
                            sb.Append(car.CarInDB);
                            sb.Append(")");
                        }

                        using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                        {
                            con.Open();
                            using (MySqlCommand cmd = new MySqlCommand(sb.ToString(), con))
                            {
                                cmd.ExecuteNonQuery();

                                try
                                {
                                    using (MySqlConnection con2 = new MySqlConnection(DBHelper.DBConnectionstring))
                                    {
                                        con2.Open();
                                        using (MySqlCommand cmd2 = new MySqlCommand("update cars set lastscanmytesla=@lastscanmytesla where id=@id", con2))
                                        {
                                            cmd2.Parameters.AddWithValue("@id", car.CarInDB);
                                            cmd2.Parameters.AddWithValue("@lastscanmytesla", DateTime.Now);
                                            cmd2.ExecuteNonQuery();
                                        }
                                    }
                                }
                                catch (Exception)
                                { }

                                return "insert ok [" + kv.Keys.Count + "] " + d.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, resultContent);
                Thread.Sleep(10000);
            }

            return "NULL";
        }
    }
}
