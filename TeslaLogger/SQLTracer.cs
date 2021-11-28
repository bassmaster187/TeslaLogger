using System;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    internal static class SQLTracer
    {
        internal static MySqlDataReader Trace(MySqlCommand cmd)
        {
            if (Program.SQLTRACE == false)
            {
                MySqlDataReader dr = cmd.ExecuteReader();
                return dr;
            }
            else
            {
                DateTime dtstart = DateTime.UtcNow;
                Tools.DebugLog(cmd);
                MySqlDataReader dr = cmd.ExecuteReader();
                DateTime dtend = DateTime.UtcNow;
                TimeSpan ts = dtend - dtstart;
                if (ts.TotalMilliseconds > Program.SQLTRACELIMIT)
                {
                    Tools.DebugLog($"SQLTracer.Trace ExecuteReader() took {ts.TotalMilliseconds}ms");
                    Tools.DebugLog(cmd);
                    Explain(cmd);
                }
                return dr;
            }
        }

        private static void Explain(MySqlCommand cmd)
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand ecmd = new MySqlCommand("EXPLAIN " + Tools.ExpandSQLCommand(cmd), con))
                {
                    ecmd.CommandType = System.Data.CommandType.Text;
                    MySqlDataReader dr = ecmd.ExecuteReader();
                    string msg = Environment.NewLine;
                    while (dr.Read())
                    {
                        for (int column = 0; column < dr.FieldCount; column++)
                        {
                            msg += (column == 0 ? "" : "|") + dr.GetName(column) + ":" + dr.GetValue(column);
                        }
                        msg += Environment.NewLine;
                    }
                    Tools.DebugLog("EXPLAIN: " + msg);
                }
            }
        }
    }
}
