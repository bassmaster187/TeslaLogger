using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless;
using Newtonsoft.Json;

namespace TeslaLogger
{
    internal class SuCSession
    {
        private string VIN;
        private string chargeSessionId;
        private string siteLocationName;
        private DateTime chargeStartDateTime;
        private List<Fee> fees = new List<Fee>();

        internal SuCSession(dynamic jsonSession)
        {
            if (jsonSession != null
     && jsonSession.ContainsKey("chargeSessionId")
     && jsonSession.ContainsKey("chargeStartDateTime")
     && jsonSession.ContainsKey("siteLocationName")
     && jsonSession.ContainsKey("fees")
     && jsonSession.ContainsKey("vin")
     )
            {
                this.chargeSessionId = jsonSession["chargeSessionId"];
                this.chargeStartDateTime = jsonSession["chargeStartDateTime"];
                this.VIN = jsonSession["vin"];
                this.siteLocationName = jsonSession["siteLocationName"];
            }
            else
            {
                throw new Exception($"Error parsing session: {new Tools.JsonFormatter(jsonSession.ToString()).Format()}");
            }

        }

        internal void AddFee(dynamic jsonFee)
        {
            Fee fee = new Fee(jsonFee);
            fees.Add(fee);
        }

        internal DateTime GetStart()
        {
            return chargeStartDateTime;
        }

        internal string GetVIN()
        {
            return VIN;
        }

        internal string GetSanitizedSuCName()
        {
            if (siteLocationName.Contains(","))
            {
                return siteLocationName.Split(',')[0];
            }
            return siteLocationName;
        }


        internal Tuple<double, string> GetChargingCosts()
        {
            double chargingCosts = double.NaN;
            string currency = "n/a";
            foreach (Fee fee in fees)
            {
                if (fee.GetFeeType().Equals("CHARGING") && fee.GetPricingType().Equals("PAYMENT"))
                {
                    if (double.IsNaN(chargingCosts)) { chargingCosts = 0.0; }
                    chargingCosts += fee.GetTotalDue();
                    currency = fee.GetCurrency();
                }
            }
            return new Tuple<double, string>(chargingCosts, currency);
        }

        internal Tuple<double, string> GetFreeSuCSavings()
        {
            double chargingCosts = double.NaN;
            string currency = "n/a";
            foreach (Fee fee in fees)
            {
                if (fee.GetFeeType().Equals("CHARGING") && fee.GetPricingType().Equals("NO_CHARGE"))
                {
                    if (double.IsNaN(chargingCosts)) { chargingCosts = 0.0; }
                    chargingCosts += fee.GetRate() * fee.GetUsage();
                    currency = fee.GetCurrency();
                }
            }
            return new Tuple<double, string>(chargingCosts, currency);
        }

        internal Tuple<double, string> GetParkingCosts()
        {
            double parkingCosts = double.NaN;
            string currency = "n/a";
            foreach (Fee fee in fees)
            {
                if (fee.GetFeeType().Equals("PARKING"))
                {
                    if (double.IsNaN(parkingCosts)) { parkingCosts = 0.0; }
                    parkingCosts += fee.GetTotalDue();
                    currency = fee.GetCurrency();
                }
            }
            return new Tuple<double, string>(parkingCosts, currency);
        }

        internal string GetSessionID()
        {
            return chargeSessionId;
        }
    }

    internal class Fee
    {
        private string feeType;
        private string pricingType;
        private double totalDue;
        private string currency;
        private int usageBase;
        private double rateBase;

        internal Fee(dynamic jsonFee)
        {
            if (jsonFee != null
                && jsonFee.ContainsKey("feeType")
                && jsonFee.ContainsKey("currencyCode")
                && jsonFee.ContainsKey("pricingType")
                && jsonFee.ContainsKey("totalDue")
                && jsonFee.ContainsKey("usageBase")
                && jsonFee.ContainsKey("rateBase")
                )
            {
                this.totalDue = jsonFee["totalDue"];
                this.feeType = jsonFee["feeType"];
                this.pricingType = jsonFee["pricingType"];
                this.currency = jsonFee["currencyCode"];
                this.usageBase = jsonFee["usageBase"];
                this.rateBase = jsonFee["rateBase"];
            }
            else
            {
                throw new Exception($"Error parsing fee: {new Tools.JsonFormatter(jsonFee.ToString()).Format()}");
            }
        }

        internal string GetFeeType()
        {
            return feeType;
        }

        internal string GetPricingType()
        {
            return pricingType;
        }

        internal double GetTotalDue()
        {
            return totalDue;
        }

        internal double GetRate()
        {
            return rateBase;
        }

        internal int GetUsage()
        {
            return usageBase;
        }

        internal string GetCurrency()
        {
            return currency;
        }
    }

    internal static class GetChargingHistoryV2Service
    {
        private static Dictionary<string, SuCSession> sessions = new Dictionary<string, SuCSession>();
        internal static List<Task> GetChargingHistoryV2Tasks = new List<Task>();

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
                                        ParseAndAddSession(session);
                                    }
                                    catch (Exception ex)
                                    {
                                        ex.ToExceptionless().FirstCarUserID().Submit();
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

        private static void ParseAndAddSession(dynamic session)
        {

            SuCSession suCSession = new SuCSession(session);
            foreach (dynamic fee in session["fees"])
            {
                suCSession.AddFee(fee);
            }
            if (!sessions.ContainsKey(suCSession.GetSessionID()))
            {
                sessions.Add(suCSession.GetSessionID(), suCSession);
                Tools.DebugLog($"GetChargingHistoryV2Service add session: {suCSession.GetVIN()} {suCSession.GetStart()} charging: {suCSession.GetChargingCosts()} freeSuC: {suCSession.GetFreeSuCSavings()} parking: {suCSession.GetParkingCosts()}");
            }

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
                Tools.DebugLog($"GetChargingHistoryV2Service.LoadAll car #{car.CarInDB} resultpage {resultPage}");
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
            dynamic jsonResult = JsonConvert.DeserializeObject(result);
            if (jsonResult != null)
            {
                _ = ParseJSON(jsonResult);
            }

        }

        internal static void CheckSchema()
        {
            try
            {
                if (!DBHelper.ColumnExists("chargingstate", "freesuc_total"))
                {
                    Logfile.Log("ALTER TABLE chargingstate ADD Column freesuc_total");
                    UpdateTeslalogger.AssertAlterDB();
                    DBHelper.ExecuteSQLQuery(@"ALTER TABLE `chargingstate` 
                    ADD COLUMN `freesuc_total` DOUBLE NULL DEFAULT NULL", 600);
                }
                if (!DBHelper.ColumnExists("chargingstate", "chargeSessionId"))
                {
                    Logfile.Log("ALTER TABLE chargingstate ADD Column chargeSessionId");
                    UpdateTeslalogger.AssertAlterDB();
                    DBHelper.ExecuteSQLQuery(@"ALTER TABLE `chargingstate` 
                    ADD COLUMN `chargeSessionId` VARCHAR(40) NULL DEFAULT NULL", 600);
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("GetChargingHistoryV2Service: Exception", ex);
            }
        }

        internal static int GetSessionCount()
        {
            return sessions.Count;
        }

        internal static void SyncAll(Car car)
        {
            Tools.DebugLog("GetChargingHistoryV2Service SyncAll start");
            foreach (SuCSession session in sessions.Values)
            {
                int chargingid = car.DbHelper.FindChargingStateIDByStartDate(session.GetStart());
                // plausibility check
                string sucname = DBHelper.GetSuCNameFromChargingStateID(chargingid);
                string teslasucname = session.GetSanitizedSuCName();
                if (sucname.Contains(teslasucname))
                {
                    // TeslaLogger name matches Tesla's name
                    Tools.DebugLog($"Update chargingstate {chargingid} with charging: {session.GetChargingCosts()} freeSuC: {session.GetFreeSuCSavings()} parking: {session.GetParkingCosts()}");
                }
                else
                {
                    (new Exception($"GetChargingHistoryV2Service could not map {sucname} and {teslasucname}")).ToExceptionless().FirstCarUserID().Submit(); ;
                }
            }
        }
    }
}