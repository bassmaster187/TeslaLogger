using System;
using System.Threading;

using Exceptionless;

namespace TeslaLogger
{
    public class GetChargingHistoryV2Service
    {
        private static GetChargingHistoryV2Service _GetChargingHistoryV2Service;

        private GetChargingHistoryV2Service()
        {
            Logfile.Log("GetChargingHistoryV2Service initialized");
        }

        public static GetChargingHistoryV2Service GetSingleton()
        {
            if (_GetChargingHistoryV2Service == null)
            {
                _GetChargingHistoryV2Service = new GetChargingHistoryV2Service();
            }
            return _GetChargingHistoryV2Service;
        }

        public void Run()
        {
            try
            {
                // first start: get complete history and sync with DB

                while (true)
                {
                    // work() retrieves only the latest charging sessions every 6 hours
                    Work();
                    // sleep 6 Hours
                    Thread.Sleep(21600000);
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("GetChargingHistoryV2Service: Exception", ex);
            }
        }

        private void Work()
        {
        }

        internal static void CheckSchema()
        {
            try
            {

            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("GetChargingHistoryV2Service: Exception", ex);
            }


        }
    }