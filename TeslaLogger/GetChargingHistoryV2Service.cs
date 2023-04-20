using System;
using System.Collections.Generic;

using Exceptionless;
using Newtonsoft.Json;

namespace TeslaLogger
{
    public class SuCSession
    {
        private string VIN;
        private string chargeSessionId;
        private DateTime chargeStartDateTime;
        private DateTime chargeStopDateTime;
        private List<Fee> fees = new List<Fee>();

        public SuCSession(dynamic jsonSession)
        {
            if (jsonSession != null
     && jsonSession.ContainsKey("chargeSessionId")
     && jsonSession.ContainsKey("chargeStartDateTime")
     && jsonSession.ContainsKey("chargeStopDateTime")
     && jsonSession.ContainsKey("fees")
     && jsonSession.ContainsKey("vin")
     )
            {
                this.chargeSessionId = jsonSession["chargeSessionId"];
                this.chargeStartDateTime = jsonSession["chargeStartDateTime"];
                this.chargeStopDateTime = jsonSession["chargeStopDateTime"];
                this.VIN = jsonSession["vin"];
            }
            else
            {
                throw new Exception($"Error parsing session: {new Tools.JsonFormatter(jsonSession.ToString()).Format()}");
            }

        }

        public void AddFee(dynamic jsonFee)
        {
            Fee fee = new Fee(jsonFee);
            fees.Add(fee);
        }

        public double getChargingCosts()
        {
            double chargingCosts = 0.0;
            foreach (Fee fee in fees)
            {
                if (fee.GetFeeType().Equals("CHARGING") && fee.GetPricingType().Equals("PAYMENT"))
                {
                    chargingCosts += fee.GetTotalDue();
                }
            }
            return chargingCosts;
        }

        public double getParkingCosts()
        {
            double parkingCosts = 0.0;
            foreach (Fee fee in fees)
            {
                if (fee.GetFeeType().Equals("PARKING"))
                {
                    parkingCosts += fee.GetTotalDue();
                }
            }
            return parkingCosts;
        }

        internal string GetSessionID()
        {
            return chargeSessionId;
        }
    }

    public class Fee
    {
        private string feeType;
        private string pricingType;
        private double totalDue;
        private string currency;
        private int usageBase;
        private double rateBase;

        public Fee(dynamic jsonFee)
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

        public string GetFeeType()
        {
            return feeType;
        }

        public string GetPricingType()
        {
            return pricingType;
        }

        internal double GetTotalDue()
        {
            return totalDue;
        }
    }

    public static class GetChargingHistoryV2Service
    {
        private static Dictionary<string, SuCSession> sessions = new Dictionary<string, SuCSession>();

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
                                Tools.DebugLog($"hasMoreData: {hasMoreData} compare:{hasMoreData == true}");
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
            }

        }

        internal static void LoadAll(Car car)
        {
            Tools.DebugLog($"GetChargingHistoryV2Service.SyncAll car #{car.CarInDB}");
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
                Tools.DebugLog($"GetChargingHistoryV2Service.SyncAll car #{car.CarInDB} resultpage {resultPage}");
                result = car.webhelper.GetChargingHistoryV2(resultPage).Result;
            }
        }

        internal static void LoadAll()
        {
            for (int id = 0; id < Car.Allcars.Count; id++)
            {
                Car car = Car.Allcars[id];
                LoadAll(car);
            }
        }

        internal static void LoadLatest()
        {
            for (int id = 0; id < Car.Allcars.Count; id++)
            {
                Car car = Car.Allcars[id];
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
                dynamic jsonResult = JsonConvert.DeserializeObject(result);
                if (jsonResult != null)
                {
                    _ = ParseJSON(jsonResult);
                }
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
    }
}