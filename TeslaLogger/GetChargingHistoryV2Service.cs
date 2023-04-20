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
                while (true)
                {
                    Work();
                    // sleep 5 Minutes
                    Thread.Sleep(300000);
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

    }
}