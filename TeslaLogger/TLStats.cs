using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace TeslaLogger
{
    public class TLStats
    {

        private static TLStats _tLStats = null;

        private TLStats ()
        {
            Logfile.Log("TLStats initialized");
        }

        public static TLStats GetInstance()
        {
            if (_tLStats == null)
            {
                _tLStats = new TLStats();
            }
            return _tLStats;
        }

        public void run()
        {
            try
            {
                Logfile.Log(Dump());
                while (true)
                {
                    if (DateTime.Now.Minute == 0)
                    {
                        Logfile.Log(Dump());
                        Thread.Sleep(60000); // sleep 60 seconds
                    }
                    else
                    {
                        Thread.Sleep(30000); // sleep 30 seconds
                    }
                }
            }
            catch (Exception) { }
        }

        internal string Dump()
        {
            StringBuilder sb = new StringBuilder();
            _ = sb.Append($"TeslaLogger process statistics{Environment.NewLine}");
            try
            {
                Process proc = Process.GetCurrentProcess();
                _ = sb.Append($"WorkingSet64:        {proc.WorkingSet64,12}{Environment.NewLine}");
                _ = sb.Append($"PeakWorkingSet64:    {proc.PeakWorkingSet64,12}{Environment.NewLine}");
                _ = sb.Append($"PrivateMemorySize64: {proc.PrivateMemorySize64,12}{Environment.NewLine}");
                _ = sb.Append($"VirtualMemorySize64: {proc.VirtualMemorySize64,12}{Environment.NewLine}");
                _ = sb.Append($"HandleCount:         {proc.HandleCount,12}{Environment.NewLine}");
                _ = sb.Append($"StartTime: {proc.StartTime}");
            }
            catch (Exception) { }
            return sb.ToString();
        }

    }
}
