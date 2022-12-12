using System;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    internal static class SQLTracer
    {
        private static int ID;

        internal static MySqlDataReader TraceDR(MySqlCommand cmd, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0)
        {
            string prefix = "(SQL" + ++ID + ") ";
            if (Program.SQLTRACE == false)
            {
                return cmd.ExecuteReader();
            }
            else
            {
                DateTime dtstart = DateTime.UtcNow;
                if (Program.SQLFULLTRACE)
                {
                    Tools.DebugLog(cmd, prefix);
                }
                MySqlDataReader dr = cmd.ExecuteReader();
                DateTime dtend = DateTime.UtcNow;
                TimeSpan ts = dtend - dtstart;
                if (ts.TotalMilliseconds > Program.SQLTRACELIMIT)
                {
                    _ = Task.Factory.StartNew(() =>
                    {
                        Tools.DebugLog($"SQLTracer.Trace ExecuteReader() took {ts.TotalMilliseconds}ms" + " (" + Path.GetFileName(callerFilePath) + ":" + callerLineNumber + ")", null, prefix);
                        if (!Program.SQLFULLTRACE)
                        {
                            Tools.DebugLog(cmd, prefix);
                        }
                        Analyze(cmd, prefix, ts);
                    }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                }
                return dr;
            }
        }

        internal static int TraceNQ(MySqlCommand cmd, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0)
        {
            string prefix = "(SQL" + ++ID + ") ";
            if (Program.SQLTRACE == false)
            {
                return cmd.ExecuteNonQuery();
            }
            else
            {
                DateTime dtstart = DateTime.UtcNow;
                if (Program.SQLFULLTRACE)
                {
                    Tools.DebugLog(cmd, prefix);
                }
                int i = cmd.ExecuteNonQuery();
                DateTime dtend = DateTime.UtcNow;
                TimeSpan ts = dtend - dtstart;
                if (ts.TotalMilliseconds > Program.SQLTRACELIMIT)
                {
                    _ = Task.Factory.StartNew(() =>
                    {
                        Tools.DebugLog($"SQLTracer.Trace ExecuteNonQuery() took {ts.TotalMilliseconds}ms" + " (" + Path.GetFileName(callerFilePath) + ":" + callerLineNumber + ")", null, prefix);
                        if (!Program.SQLFULLTRACE)
                        {
                            Tools.DebugLog(cmd, prefix);
                        }
                        Analyze(cmd, prefix, ts);
                    }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                }
                return i;
            }
        }

        internal static object TraceSc(MySqlCommand cmd, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0)
        {
            string prefix = "(SQL" + ++ID + ") ";
            if (Program.SQLTRACE == false)
            {
                return cmd.ExecuteScalar();
            }
            else
            {
                DateTime dtstart = DateTime.UtcNow;
                if (Program.SQLFULLTRACE)
                {
                    Tools.DebugLog(cmd, prefix);
                }
                object o = cmd.ExecuteScalar();
                DateTime dtend = DateTime.UtcNow;
                TimeSpan ts = dtend - dtstart;
                if (ts.TotalMilliseconds > Program.SQLTRACELIMIT)
                {
                    _ = Task.Factory.StartNew(() =>
                    {
                        Tools.DebugLog($"SQLTracer.Trace ExecuteScalar() took {ts.TotalMilliseconds}ms" + " (" + Path.GetFileName(callerFilePath) + ":" + callerLineNumber + ")", null, prefix);
                        if (!Program.SQLFULLTRACE)
                        {
                            Tools.DebugLog(cmd, prefix);
                        }
                        Analyze(cmd, prefix, ts);
                    }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                }
                return o;
            }
        }

        internal static int TraceDA(DataTable dt, MySqlDataAdapter da, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0)
        {
            string prefix = "(SQL" + ++ID + ") ";
            if (Program.SQLTRACE == false)
            {
                return da.Fill(dt);
            }
            else
            {
                DateTime dtstart = DateTime.UtcNow;
                MySqlCommand cmd = da.SelectCommand;
                if (Program.SQLFULLTRACE)
                {
                    Tools.DebugLog(cmd, null, prefix);
                }
                int i = da.Fill(dt);
                DateTime dtend = DateTime.UtcNow;
                TimeSpan ts = dtend - dtstart;
                if (ts.TotalMilliseconds > Program.SQLTRACELIMIT)
                {
                    _ = Task.Factory.StartNew(() =>
                    {
                        Tools.DebugLog($"SQLTracer.Trace DataAdapter.Fill() took {ts.TotalMilliseconds}ms" + " (" + Path.GetFileName(callerFilePath) + ":" + callerLineNumber + ")", null, prefix);
                        if (!Program.SQLFULLTRACE)
                        {
                            Tools.DebugLog(cmd, prefix);
                        }
                        Analyze(cmd, prefix, ts);
                    }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                }
                return i;
            }
        }

        private static void Analyze(MySqlCommand cmd, string prefix, TimeSpan ts)
        {
            if (cmd.CommandText.Trim().ToUpper(Tools.ciEnUS).Substring(0, 12).Contains("SELECT"))
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand ecmd = new MySqlCommand("ANALYZE " + Tools.ExpandSQLCommand(cmd), con))
                    {
                        ecmd.CommandType = CommandType.Text;
                        ecmd.CommandTimeout = (int)Math.Max(ts.TotalSeconds * 5, 60);
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
                        Tools.DebugLog("ANALYZE: " + msg, null, prefix);
                    }
                }
            }
        }
    }
}
