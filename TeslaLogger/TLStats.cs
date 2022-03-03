using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Exceptionless;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "brauchen wir nicht")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
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
                _ = sb.Append($"StartTime: {proc.StartTime}{Environment.NewLine}");
                _ = sb.Append($"Database sizes: DB {DBHelper.Database}{Environment.NewLine}");
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  TABLE_NAME AS `Table`,
  ROUND((DATA_LENGTH + INDEX_LENGTH) / 1024 / 1024) AS `Size (MB)`
FROM
  information_schema.TABLES
WHERE
  TABLE_SCHEMA = @dbschema
  and ROUND((DATA_LENGTH + INDEX_LENGTH) / 1024 / 1024) > 0.9
ORDER BY
  (DATA_LENGTH + INDEX_LENGTH)
DESC", con))
                    {
                        cmd.Parameters.AddWithValue("@dbschema", DBHelper.Database);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        while (dr.Read())
                        {
                            _ = sb.Append($"  table {dr[0]} has {dr[1]}mb{Environment.NewLine}");
                        }
                    }
                }
            }
            catch (Exception) { }
            return sb.ToString();
        }

    }
}
