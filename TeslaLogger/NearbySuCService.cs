using System;
using System.Collections.Generic;
using System.Threading;
using System.Web.Script.Serialization;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    public class NearbySuCService
    {
        private static NearbySuCService _NearbySuCService = null;

        private NearbySuCService()
        {
            Logfile.Log("NearbySuCService initialized");
        }

        public static NearbySuCService GetSingleton()
        {
            if (_NearbySuCService == null)
            {
                _NearbySuCService = new NearbySuCService();
            }
            return _NearbySuCService;
        }

        public void Run()
        {
            // initially sleep 30 seconds to let the cars get from Start to Online
            Thread.Sleep(30000);
            try
            {
                while(true)
                {
                    Work();
                    // sleep 10 Minutes
                    Thread.Sleep(300000);
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog("NearbySuCService: Exception", ex);
            }
        }

        private void Work()
        {
            Tools.DebugLog("NearbySuCService: Work");
            // nearby_charging_sites
            foreach (Car car in Car.allcars)
            {
                if (car.IsInService())
                    continue;

                if ((car.GetCurrentState() == Car.TeslaState.Charge
                    || car.GetCurrentState() == Car.TeslaState.Drive
                    || car.GetCurrentState() == Car.TeslaState.Online)
                    && car.currentJSON.current_falling_asleep == false) {
                    string result = string.Empty;
                    try
                    {
                        result = car.webhelper.GetNearbyChargingSites().Result;
                        if (result.Contains("Retry later"))
                        {
                            Tools.DebugLog("NearbySuCService: Retry later");
                            return;
                        }
                        object jsonResult = new JavaScriptSerializer().DeserializeObject(result);
                        Dictionary<string, object>response = (Dictionary<string, object>)((Dictionary<string, object>)jsonResult)["response"];
                        if (response.ContainsKey("superchargers"))
                        {
                            System.Object[] superchargers = (System.Object[])response["superchargers"];
                            foreach (object supercharger in superchargers)
                            {
                                /*
      {
        "location": { "lat": 33.848756, "long": -84.36434 },
        "name": "Atlanta, GA - Peachtree Road",
        "type": "supercharger",
        "distance_miles": 10.868304,
        "available_stalls": 4,
        "total_stalls": 5,
        "site_closed": false
      }
                                 */
                                Dictionary<string, object> suc = (Dictionary<string, object>)supercharger;
                                try
                                {
                                    AddSuperchargerState(suc);
                                }
                                catch (Exception ex)
                                {
                                    Logfile.Log(ex.ToString());
                                }
                            }
                        }
                        Thread.Sleep(30000);
                    }
                    catch (Exception ex)
                    {
                        Tools.DebugLog($"NearbySuCService.Work: result {new Tools.JsonFormatter(result).Format()}");
                        Tools.DebugLog("NearbySuCService.Work: Exception", ex);
                    }
                }
            }
        }

        private void AddSuperchargerState(Dictionary<string, object> suc)
        {
            int sucID = int.MinValue;
            bool SuCfound = GetSuperchargerByName(suc["name"].ToString(), out sucID);
            if (!SuCfound)
            {
                // add new entry to supercharger list in DB
                Dictionary<string, object> location = (Dictionary<string, object>)suc["location"];
                sucID = AddNewSupercharger(suc["name"].ToString(), double.Parse(location["lat"].ToString()), double.Parse(location["long"].ToString()));
            }
            else
            {
                if (suc.ContainsKey("available_stalls")
                    && suc.ContainsKey("total_stalls")
                    && suc.ContainsKey("site_closed")
                    && bool.TryParse(suc["site_closed"].ToString(), out bool site_closed)
                    && site_closed == false)
                {
                    Tools.DebugLog($"SuC: <{suc["name"]}> <{suc["available_stalls"]}> <{suc["total_stalls"]}>");
                    if (int.TryParse(suc["available_stalls"].ToString(), out int available_stalls)
                        && int.TryParse(suc["total_stalls"].ToString(), out int total_stalls)) {
                        if (total_stalls > 0)
                        {
                            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                            {
                                con.Open();
                                // find internal ID of supercharger by name
                                using (MySqlCommand cmd = new MySqlCommand("INSERT superchargerstate (nameid, ts, available_stalls, total_stalls) values (@nameid, @ts, @available_stalls, @total_stalls) ", con))
                                {
                                    cmd.Parameters.AddWithValue("@nameid", sucID);
                                    cmd.Parameters.AddWithValue("@ts", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@available_stalls", available_stalls);
                                    cmd.Parameters.AddWithValue("@total_stalls", total_stalls);
                                    Tools.DebugLog(cmd);
                                    cmd.ExecuteNonQuery();
                                }
                                con.Close();
                            }
                        }
                        else
                        {
                            // TODO how do we handle total_stalls == 0 ?
                        }
                    }
                }
                else if (suc.ContainsKey("site_closed")
                    && bool.TryParse(suc["site_closed"].ToString(), out site_closed)
                    && site_closed)
                {
                    Tools.DebugLog($"SuC: <{suc["name"]}> site_closed");
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();
                        // find internal ID of supercharger by name
                        using (MySqlCommand cmd = new MySqlCommand("INSERT superchargerstate (nameid, ts, available_stalls, total_stalls) values (@nameid, @ts, @available_stalls, @total_stalls) ", con))
                        {
                            cmd.Parameters.AddWithValue("@nameid", sucID);
                            cmd.Parameters.AddWithValue("@ts", DateTime.Now);
                            cmd.Parameters.AddWithValue("@available_stalls", -1);
                            cmd.Parameters.AddWithValue("@total_stalls", -1);
                            Tools.DebugLog(cmd);
                            cmd.ExecuteNonQuery();
                        }
                        con.Close();
                    }
                }
                else if (suc.ContainsKey("site_closed")
                    && bool.TryParse(suc["site_closed"].ToString(), out site_closed)
                    && !site_closed)
                {
                    Tools.DebugLog($"SuC: <{suc["name"]}> no info (fields available: available_stalls {suc.ContainsKey("available_stalls")} total_stalls {suc.ContainsKey("available_stalls")})");
                    Tools.DebugLog(new Tools.JsonFormatter(new JavaScriptSerializer().Serialize(suc)).Format());
                }
                else
                {
                    Tools.DebugLog($"suc ContainsKey available_stalls {suc.ContainsKey("available_stalls")} total_stalls {suc.ContainsKey("available_stalls")} site_closed {suc.ContainsKey("site_closed")}");
                }
            }
        }

        private int AddNewSupercharger(string name, double lat, double lng)
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                // find internal ID of supercharger by name
                using (MySqlCommand cmd = new MySqlCommand("INSERT superchargers (name, lat, lng) values (@name, @lat, @lng) ", con))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@lat", lat);
                    cmd.Parameters.AddWithValue("@lng", lng);
                    Tools.DebugLog(cmd);
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
            GetSuperchargerByName(name, out int sucID);
            return sucID;
        }

        private static bool GetSuperchargerByName(string suc, out int sucID)
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                // find internal ID of supercharger by name
                using (MySqlCommand cmd = new MySqlCommand("SELECT id from superchargers where name = @name", con))
                {
                    cmd.Parameters.AddWithValue("@name", suc);
                    Tools.DebugLog(cmd);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read() && dr[0] != DBNull.Value)
                    {
                        if (int.TryParse(dr[0].ToString(), out sucID))
                        {
                            Tools.DebugLog($"GetSuperchargerByName: sucID {sucID} true");
                            return true;
                        }
                    }
                }
            }
            sucID = int.MinValue;
            Tools.DebugLog($"GetSuperchargerByName: sucID {sucID} false");
            return false;
        }
    }
}
