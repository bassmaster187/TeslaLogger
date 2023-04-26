using System;
using System.Threading;
using Exceptionless;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace TeslaLogger
{
    internal class SuCSession
    {
        internal SuCSession(dynamic jsonSession)
        {
            string VIN;
            string chargeSessionId;
            string siteLocationName;
            DateTime chargeStartDateTime;
            if (jsonSession != null
     && jsonSession.ContainsKey("chargeSessionId")
     && jsonSession.ContainsKey("chargeStartDateTime")
     && jsonSession.ContainsKey("siteLocationName")
     && jsonSession.ContainsKey("fees")
     && jsonSession.ContainsKey("vin")
     )
            {
                VIN = jsonSession["vin"];
                chargeSessionId = jsonSession["chargeSessionId"];
                siteLocationName = jsonSession["siteLocationName"];
                chargeStartDateTime = jsonSession["chargeStartDateTime"];
                if (DateTime.TryParse(jsonSession["chargeStartDateTime"].ToString("yyyy-MM-dd HH:mm:ss"), out DateTime isochargeStartDateTime))
                {
                    chargeStartDateTime = isochargeStartDateTime;
                }
                Tools.DebugLog($"new SuCSession: <{VIN}> <{chargeSessionId}> <{siteLocationName}> <{chargeStartDateTime}>");
            }
            else
            {
                throw new Exception($"Error parsing session: {new Tools.JsonFormatter(jsonSession.ToString()).Format()}");
            }
            // no try/catch, the calling function has to do that
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"
INSERT IGNORE INTO teslacharging SET
        chargeSessionId = @chargeSessionId,
        chargeStartDateTime = @chargeStartDateTime,
        siteLocationName = @siteLocationName,
        VIN = @VIN,
        json = @json", con))
                {
                    cmd.Parameters.AddWithValue("@chargeSessionId", chargeSessionId);
                    cmd.Parameters.AddWithValue("@chargeStartDateTime", chargeStartDateTime);
                    cmd.Parameters.AddWithValue("@siteLocationName", siteLocationName);
                    cmd.Parameters.AddWithValue("@VIN", VIN);
                    cmd.Parameters.AddWithValue("@json", jsonSession.ToString());
                    SQLTracer.TraceNQ(cmd);
                }
            }
        }
    }

    internal static class GetChargingHistoryV2Service
    {
        private static bool ParseJSON(string sjson)
        {
            bool nextpage = false;
            dynamic json = JsonConvert.DeserializeObject(sjson);
            if (json == null)
            {
                Tools.DebugLog("ParseJSON: json == null");
                return nextpage;
            }
            if (json.ContainsKey("data"))
            {
                dynamic data = json["data"];
                if (data.ContainsKey("me"))
                {
                    dynamic me = data["me"];
                    if (me.ContainsKey("charging"))
                    {
                        dynamic charging = me["charging"];
                        if (charging.ContainsKey("historyV2"))
                        {
                            dynamic historyV2 = charging["historyV2"];
                            if (historyV2.ContainsKey("data"))
                            {
                                dynamic historyV2data = historyV2["data"];
                                foreach (dynamic session in historyV2data)
                                {
                                    try
                                    {
                                        _ = new SuCSession(session);
                                    }
                                    catch (Exception ex)
                                    {
                                        ex.ToExceptionless().FirstCarUserID().AddObject(sjson, "ResultContent").Submit();
                                        Logfile.Log(ex.ToString());
                                    }
                                }
                            }
                            else
                            {
                                Tools.DebugLog("ParseJSON: historyV2.ContainsKey(historyV2data): false");
                            }
                            if (historyV2.ContainsKey("hasMoreData"))
                            {
                                dynamic hasMoreData = historyV2["hasMoreData"];
                                nextpage = hasMoreData == true;
                            }
                            else
                            {
                                Tools.DebugLog("ParseJSON: historyV2.ContainsKey(hasMoreData): false");
                            }
                        }
                        else
                        {
                            Tools.DebugLog("ParseJSON: charging.ContainsKey(historyV2): false");
                        }
                    }
                    else
                    {
                        Tools.DebugLog("ParseJSON: me.ContainsKey(charging): false");
                    }
                }
                else
                {
                    Tools.DebugLog("ParseJSON: data.ContainsKey(me): false");
                }
            }
            else
            {
                Tools.DebugLog("ParseJSON: json.ContainsKey(data): false");
            }
            return nextpage;
        }

        internal static void LoadAll(Car car)
        {
            Tools.DebugLog($"GetChargingHistoryV2Service.LoadAll car #{car.CarInDB}");
            int resultPage = 1;
            string result = car.webhelper.GetChargingHistoryV2(resultPage).Result;
            if (result == null || result == "{}" || string.IsNullOrEmpty(result))
            {
                Tools.DebugLog("GetChargingHistoryV2Service: result == null");
                return;
            }
            if (result.Contains("Retry later"))
            {
                Tools.DebugLog("GetChargingHistoryV2Service: Retry later");
                return;
            }
            else if (result.Contains("vehicle unavailable"))
            {
                Tools.DebugLog("GetChargingHistoryV2Service: vehicle unavailable");
                return;
            }
            else if (result.Contains("502 Bad Gateway"))
            {
                Tools.DebugLog("GetChargingHistoryV2Service: 502 Bad Gateway");
                return;
            }
            Tools.DebugLog($"GetChargingHistoryV2Service GetChargingHistoryV2 result length: {result.Length}");

            while (result != null && ParseJSON(result))
            {
                resultPage++;
                Thread.Sleep(500); // wait a bit
                Tools.DebugLog($"GetChargingHistoryV2Service.LoadAll #{car.CarInDB} resultpage {resultPage}");
                result = car.webhelper.GetChargingHistoryV2(resultPage).Result;
            }
        }

        internal static void LoadLatest(Car car)
        {
            Tools.DebugLog($"GetChargingHistoryV2Service.LoadLatest car #{car.CarInDB}");
            string result = car.webhelper.GetChargingHistoryV2(1).Result;
            if (result == null || result == "{}" || string.IsNullOrEmpty(result))
            {
                Tools.DebugLog("GetChargingHistoryV2Service: result == null");
                return;
            }
            if (result.Contains("Retry later"))
            {
                Tools.DebugLog("GetChargingHistoryV2Service: Retry later");
                return;
            }
            else if (result.Contains("vehicle unavailable"))
            {
                Tools.DebugLog("GetChargingHistoryV2Service: vehicle unavailable");
                return;
            }
            else if (result.Contains("502 Bad Gateway"))
            {
                Tools.DebugLog("GetChargingHistoryV2Service: 502 Bad Gateway");
                return;
            }
            _ = ParseJSON(result);
        }

        internal static void CheckSchema()
        {
            try
            {
                if (!DBHelper.ColumnExists("chargingstate", "cost_freesuc_savings_total"))
                {
                    Logfile.Log("ALTER TABLE chargingstate ADD Column cost_freesuc_savings_total");
                    UpdateTeslalogger.AssertAlterDB();
                    DBHelper.ExecuteSQLQuery(@"ALTER TABLE `chargingstate` 
                    ADD COLUMN `cost_freesuc_savings_total` DOUBLE NULL DEFAULT NULL", 600);
                }
                if (!DBHelper.ColumnExists("chargingstate", "chargeSessionId"))
                {
                    Logfile.Log("ALTER TABLE chargingstate ADD Column chargeSessionId");
                    UpdateTeslalogger.AssertAlterDB();
                    DBHelper.ExecuteSQLQuery(@"ALTER TABLE `chargingstate` 
                    ADD COLUMN `chargeSessionId` VARCHAR(40) NULL DEFAULT NULL", 600);
                }
                if (!DBHelper.TableExists("teslacharging"))
                {
                    string sql = @"
CREATE TABLE teslacharging (
    chargeSessionId VARCHAR(40) NOT NULL,
    chargeStartDateTime DATETIME NOT NULL,
    siteLocationName VARCHAR(128) NOT NULL,
    VIN VARCHAR(20) NOT NULL,
    json LONGTEXT NOT NULL,
    UNIQUE ix_chargeSessionId(chargeSessionId)
)";
                    Logfile.Log(sql);
                    UpdateTeslalogger.AssertAlterDB();
                    DBHelper.ExecuteSQLQuery(sql);
                    Logfile.Log("CREATE TABLE teslacharging OK");
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("GetChargingHistoryV2Service: Exception", ex);
            }
        }

        private static bool GetTeslaChargingSessionByDate(Car car, DateTime dt, out string chargeSessionId, out string siteLocationName, out DateTime chargeStartDateTime, out string VIN, out string json)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    chargeSessionId,
    siteLocationName,
    chargeStartDateTime,
    VIN,
    json
FROM
    teslacharging
WHERE
    VIN = @VIN
ORDER BY
    ABS(
        chargeStartDateTime - @startDate
    )
LIMIT 1
", con))
                    {
                        cmd.Parameters.AddWithValue("@startDate", dt);
                        cmd.Parameters.AddWithValue("@VIN", car.Vin);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read()
                            && dr[0] != DBNull.Value
                            && dr[1] != DBNull.Value
                            && dr[2] != DBNull.Value
                            && DateTime.TryParse(dr[2].ToString(), out chargeStartDateTime)
                            && dr[3] != DBNull.Value
                            && dr[4] != DBNull.Value
                            )
                        {
                            chargeSessionId = dr[0].ToString();
                            siteLocationName = dr[1].ToString();
                            VIN = dr[3].ToString();
                            json = dr[4].ToString();
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
            chargeSessionId = string.Empty;
            siteLocationName = string.Empty;
            chargeStartDateTime = DateTime.MinValue;
            VIN = string.Empty;
            json = string.Empty;
            return false;
        }

        internal static int SyncAll(Car car)
        {
            Tools.DebugLog("GetChargingHistoryV2Service SyncAll start");
            int updatedChargingStates = 0;
            foreach (int chargingstateid in car.DbHelper.GetSuCChargingStatesWithEmptyChargeSessionId())
            {
                Tools.DebugLog($"GetChargingHistoryV2Service <{chargingstateid}>");
                if (DBHelper.GetStartValuesFromChargingState(chargingstateid, out DateTime startDate, out int startdID, out int posID))
                {
                    if (GetTeslaChargingSessionByDate(car, startDate, out string chargeSessionId, out string siteLocationName, out DateTime chargeStartDateTime, out string VIN, out string json))
                    {
                        Tools.DebugLog($"SyncAll <{chargingstateid}> -> <{chargeSessionId}> timediff:{Math.Abs((chargeStartDateTime - startDate).TotalMinutes)}");
                        string tlname = DBHelper.GetSuCNameFromChargingStateID(chargingstateid);
                        // check names, time difference and VIN
                        if (siteLocationName.Contains(","))
                        {
                            siteLocationName = siteLocationName.Split(',')[0];
                        }
                        if (tlname.Contains(siteLocationName)
                            && Math.Abs((chargeStartDateTime - startDate).TotalMinutes) < 10
                            && car.Vin.Equals(VIN)
                            )
                        {
                            UpdateChargingState(chargingstateid, json, car);
                            updatedChargingStates++;
                        }
                        else if (Math.Abs((chargeStartDateTime - startDate).TotalMinutes) < 10
                            && car.Vin.Equals(VIN))
                        {
                            Tools.DebugLog($"GetChargingHistoryV2Service could not map <{tlname}> and <{siteLocationName}>");
                            (new Exception($"GetChargingHistoryV2Service could not map <{tlname}> and <{siteLocationName}>")).ToExceptionless().FirstCarUserID().Submit();
                        }
                        else if (!car.Vin.Equals(VIN))
                        {
                            Tools.DebugLog($"GetChargingHistoryV2Service {chargeSessionId} VIN does not match car:{car.Vin} session:{VIN}");
                        }
                    }
                }
                else
                {
                    Tools.DebugLog($"GetChargingHistoryV2Service GetStartValuesFromChargingState false for {chargingstateid}");
                }
            }
            Tools.DebugLog("GetChargingHistoryV2Service SyncAll finished");
            return updatedChargingStates;
        }

        private static void UpdateChargingState(int chargingstateid, string json, Car car)
        {
            dynamic session = JsonConvert.DeserializeObject(json);
            if (session != null
                && session.ContainsKey("fees")
                && session.ContainsKey("chargeSessionId")
                )
            {
                double cost_total = double.NaN;
                string cost_currency = string.Empty;
                double cost_per_kwh = double.NaN;
                double cost_for_charging = double.NaN;
                double cost_idle_fee_total = double.NaN;
                double cost_kwh_meter_invoice = double.NaN;
                double cost_freesuc_savings_total = double.NaN;
                string chargeSessionId = session["chargeSessionId"];
                bool freesuc = false;

                // parse fees
                foreach (dynamic fee in session["fees"])
                {
                    if (fee.ContainsKey("currencyCode"))
                    {
                        cost_currency = fee["currencyCode"].ToString();
                    }
                    if (fee.ContainsKey("feeType"))
                    {
                        if (fee["feeType"].ToString().Equals("CHARGING"))
                        {
                            if (fee.ContainsKey("uom") && fee["uom"].ToString().Equals("kWh"))
                            {
                                if (fee.ContainsKey("rateBase"))
                                {
                                    _ = double.TryParse(fee["rateBase"].ToString(Tools.ciEnUS), out cost_per_kwh);
                                }
                                if (fee.ContainsKey("usageBase"))
                                {
                                    _ = double.TryParse(fee["usageBase"].ToString(Tools.ciEnUS), out cost_kwh_meter_invoice);
                                }
                                if (fee.ContainsKey("pricingType")
                                    && fee["pricingType"].ToString().Equals("PAYMENT")
                                    && fee.ContainsKey("totalDue")
                                    )
                                {
                                    _ = double.TryParse(fee["totalDue"].ToString(Tools.ciEnUS), out cost_for_charging);
                                }
                                if (fee.ContainsKey("pricingType")
                                    && fee["pricingType"].ToString().Equals("NO_CHARGE")
                                    )
                                {
                                    freesuc = true;
                                }
                            }
                        }
                        else if (fee["feeType"].ToString().Equals("PARKING"))
                        {
                            if (fee.ContainsKey("totalDue"))
                            {
                                _ = double.TryParse(fee["totalDue"].ToString(Tools.ciEnUS), out cost_idle_fee_total);
                            }
                        }
                    }
                }
                if (freesuc
                    && !double.IsNaN(cost_per_kwh)
                    && !double.IsNaN(cost_kwh_meter_invoice)
                    )
                {
                    cost_freesuc_savings_total = cost_per_kwh * cost_kwh_meter_invoice;
                }
                if (!double.IsNaN(cost_for_charging))
                {
                    cost_total = cost_for_charging;
                    if (!double.IsNaN(cost_idle_fee_total))
                    {
                        cost_total += cost_idle_fee_total;
                    }
                }
                if (double.IsNaN(cost_total) && !double.IsNaN(cost_idle_fee_total))
                {
                    cost_total = cost_idle_fee_total;
                }
                Tools.DebugLog($@"UpdateChargingState:
chargingstateid:{chargingstateid}
chargeSessionId:{chargeSessionId}
cost_total:{cost_total}
cost_currency:{cost_currency}
cost_per_kwh:{cost_per_kwh}
cost_for_charging:{cost_for_charging}
cost_idle_fee_total:{cost_idle_fee_total}
cost_kwh_meter_invoice:{cost_kwh_meter_invoice}
cost_freesuc_savings_total:{cost_freesuc_savings_total}
freesuc:{freesuc}");
                try
                {
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();
                        using (MySqlCommand cmd = new MySqlCommand(@"
UPDATE
    chargingstate
SET
    chargeSessionId = @chargeSessionId
" + (!double.IsNaN(cost_total) ? ", cost_total = @cost_total" : "") + @"
" + (!string.IsNullOrEmpty(cost_currency) ? ", cost_currency = @cost_currency" : "") + @"
" + (!double.IsNaN(cost_per_kwh) ? ", cost_per_kwh = @cost_per_kwh" : "") + @"
" + (!double.IsNaN(cost_idle_fee_total) ? ", cost_idle_fee_total = @cost_idle_fee_total" : "") + @"
" + (!double.IsNaN(cost_kwh_meter_invoice) ? ", cost_kwh_meter_invoice = @cost_kwh_meter_invoice" : "") + @"
" + (!double.IsNaN(cost_freesuc_savings_total) ? ", cost_freesuc_savings_total = @cost_freesuc_savings_total" : "") + @"
WHERE
    id = @chargingstateid
", con))
                        {
                            cmd.Parameters.AddWithValue("@chargingstateid", chargingstateid);
                            cmd.Parameters.AddWithValue("@chargeSessionId", chargeSessionId);
                            if (!double.IsNaN(cost_total)) { cmd.Parameters.AddWithValue("@cost_total", cost_total); }
                            if (!string.IsNullOrEmpty(cost_currency)) { cmd.Parameters.AddWithValue("@cost_currency", cost_currency); }
                            if (!double.IsNaN(cost_per_kwh)) { cmd.Parameters.AddWithValue("@cost_per_kwh", cost_per_kwh); }
                            if (!double.IsNaN(cost_idle_fee_total)) { cmd.Parameters.AddWithValue("@cost_idle_fee_total", cost_idle_fee_total); }
                            if (!double.IsNaN(cost_kwh_meter_invoice)) { cmd.Parameters.AddWithValue("@cost_kwh_meter_invoice", cost_kwh_meter_invoice); }
                            if (!double.IsNaN(cost_freesuc_savings_total)) { cmd.Parameters.AddWithValue("@cost_freesuc_savings_total", cost_freesuc_savings_total); }
                            //Tools.DebugLog(cmd);
                            int rowsUpdated = SQLTracer.TraceNQ(cmd);
                            if (rowsUpdated == 1)
                            {
                                car.Log($"ChargingState <{chargingstateid}> updated from GetChargingHistoryV2Service");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                }

            }
            else
            {
                Tools.DebugLog("Error parsing json:" + json);
            }
        }
    }
}