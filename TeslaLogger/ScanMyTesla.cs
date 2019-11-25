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
            return;
            string response = "";

            while (run)
            {
                try
                {
                    System.Threading.Thread.Sleep(1000);

                    if (!fastmode && response == "not found")
                    {
                        for (int s = 0; s < 300; s++)
                        {
                            if (fastmode)
                                break;

                            System.Threading.Thread.Sleep(10);
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
                string datum = temp.Substring(0, i);
                temp = temp.Substring(i + 2);

                dynamic j = new JavaScriptSerializer().DeserializeObject(temp);
                DateTime d = DateTime.Parse(j["d"]);
                decimal CTmi = j["CTmi"];
                decimal CTav = j["CTav"];
                decimal CTma = j["CTma"];
                decimal CTdi = j["CTdi"];
                decimal CVmi = j["CVmi"];
                decimal CVav = j["CVav"];
                decimal CVma = j["CVma"];
                decimal CVdi = j["CVdi"];

                string SQL = @"INSERT INTO candata (`datum`,`cell_temp_min`,`cell_temp_avg`,`cell_temp_max`,`cell_temp_diff`,`cell_v_min`,`cell_v_avg`,`cell_v_max`,`cell_v_diff`) VALUES
                (@datum, @cell_temp_min, @cell_temp_avg, @cell_temp_max, @cell_temp_diff, @cell_v_min, @cell_v_avg, @cell_v_max , @cell_v_diff)";

                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand(SQL, con);
                    cmd.Parameters.AddWithValue("datum", d);
                    cmd.Parameters.AddWithValue("@cell_temp_min", CTmi);
                    cmd.Parameters.AddWithValue("@cell_temp_avg", CTav);
                    cmd.Parameters.AddWithValue("@cell_temp_max", CTma);
                    cmd.Parameters.AddWithValue("@cell_temp_diff", CTdi);
                    cmd.Parameters.AddWithValue("@cell_v_min", CVmi > 0 ? (object)CVmi : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@cell_v_avg", CVav > 0 ? (object)CVav : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@cell_v_max", CVma > 0 ? (object)CVma : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@cell_v_diff", CVdi > 0 ? (object)CVdi : (object)DBNull.Value);
                    cmd.ExecuteNonQuery();

                    return "insert ok "+ d.ToString();
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
