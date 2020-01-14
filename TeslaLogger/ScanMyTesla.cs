using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    class ScanMyTesla
    {
        string token;
        System.Threading.Thread thread;
        bool fastmode = false;
        bool run = true;

        public ScanMyTesla(string token)
        {
            this.token = token;

            thread = new System.Threading.Thread(new System.Threading.ThreadStart(Start));
            thread.Start();
        }

        public void FastMode(bool fast)
        {
            Logfile.Log("ScanMyTesla FastMode: " + fast.ToString());
            fastmode = fast;
        }

        private void Start()
        {
            if (!Tools.UseScanMyTesla())
                return;

            Logfile.Log("Start ScanMyTesla Thread!");

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
                                break;

                            System.Threading.Thread.Sleep(100);
                        }
                    }

                    response = GetDataFromWebservice().Result;
                    if (response.StartsWith("not found") || response.StartsWith("ERROR:"))
                        System.Threading.Thread.Sleep(5000);
                    else
                    {
                        InsertData(response);
                    }
                }
                catch (Exception ex)
                {
                    Logfile.Log("Scanmytesla: " + ex.Message);
                    Logfile.WriteException(ex.ToString());
                    System.Threading.Thread.Sleep(20000);
                }
            }
        }

        private void InsertData(string response)
        {
            Logfile.Log("ScanMyTesla: " + response);
        }

        public async Task<String> GetDataFromWebservice()
        {
            string resultContent = "";
            try
            {
                HttpClient client = new HttpClient();
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("t", token)
                });

                var result = await client.PostAsync("http://teslalogger.de/get_scanmytesla.php", content);
                
                resultContent = await result.Content.ReadAsStringAsync();

                if (resultContent == "not found")
                    return "not found";

                string temp = resultContent;
                int i = 0;
                i = temp.IndexOf("\r\n");
                string id = temp.Substring(0, i);

                temp = temp.Substring(i+2);

                i = temp.IndexOf("\r\n");
                string date = temp.Substring(0, i);
                temp = temp.Substring(i + 2);

                dynamic j = new JavaScriptSerializer().DeserializeObject(temp);
                DateTime d = DateTime.Parse(j["d"]);
                System.Collections.Generic.Dictionary<string, Object> kv = (System.Collections.Generic.Dictionary<string, Object>)j["dict"];

                StringBuilder sb = new StringBuilder();
                sb.Append("INSERT INTO `can` (`datum`, `id`, `val`) VALUES ");
                bool first = true;

                String sqlDate =  d.ToString("yyyy-MM-dd HH:mm:ss");

                foreach (var line in kv)
                {
                    if (line.Value == "Infinity" || line.Value == "NaN")
                        continue;

                    if (first)
                        first = false;
                    else
                        sb.Append(",");

                    sb.Append("('");
                    sb.Append(sqlDate);
                    sb.Append("',");
                    sb.Append(line.Key);
                    sb.Append(",");
                    sb.Append(Convert.ToDouble(line.Value).ToString(Tools.ciEnUS));
                    sb.Append(")");
                }



                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand(sb.ToString(), con);
                    cmd.ExecuteNonQuery();

                    try
                    {
                        string lastscanmyteslafilepaht = System.IO.Path.Combine(FileManager.GetExecutingPath(), "LASTSCANMYTESLA");
                        System.IO.File.WriteAllText(lastscanmyteslafilepaht, sqlDate);
                    }
                    catch (Exception)
                    { }

                    return "insert ok ["+ kv.Keys.Count + "] "+  d.ToString();
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }
    }
}
