using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace TeslaLogger
{
    public partial class DBHelper
    {

        private static string _DBConnectionstring = string.Empty;
        internal static string Database = "teslalogger";
        internal static string User = "root";
        internal static string Password = "teslalogger";

        internal static string DBConnectionstring => GetDBConnectionstring();

        internal static string GetDBConnectionstring(bool obfuscate = false)
        {
            if (!string.IsNullOrEmpty(_DBConnectionstring))
            {
                return _DBConnectionstring;
            }
            string DBConnectionstring = "";
            if (string.IsNullOrEmpty(ApplicationSettings.Default.DBConnectionstring))
            {
                if (Tools.IsDocker())
                    DBConnectionstring = "Server=database;Database=teslalogger;Uid=root;Password=teslalogger;CharSet=utf8mb4;";
                else
                    DBConnectionstring = "Server=127.0.0.1;Database=teslalogger;Uid=root;Password=teslalogger;CharSet=utf8mb4;";
            }
            else
            {
                DBConnectionstring = ApplicationSettings.Default.DBConnectionstring;
            }

            if (DBConnectionstring.ToLower(Tools.ciEnUS).Contains("charset="))
            {
                Match m = Regex.Match(DBConnectionstring.ToLower(Tools.ciEnUS), "charset(=.+?);");
                if (m.Success && m.Groups.Count == 2 && m.Groups[1].Captures.Count == 1)
                {
                    DBConnectionstring = DBConnectionstring.Replace(m.Groups[1].Captures[0].ToString(), "=utf8mb4");
                    _DBConnectionstring = DBConnectionstring;
                }
                else
                {
                    m = Regex.Match(DBConnectionstring.ToLower(Tools.ciEnUS), "charset(=.+)$");
                    if (m.Success && m.Groups.Count == 2 && m.Groups[1].Captures.Count == 1)
                    {
                        DBConnectionstring = DBConnectionstring.Replace(m.Groups[1].Captures[0].ToString(), "=utf8mb4");
                        _DBConnectionstring = DBConnectionstring;
                    }
                }
            }
            if (!DBConnectionstring.ToLower(Tools.ciEnUS).Contains("charset="))
            {
                if (!DBConnectionstring.EndsWith(";", StringComparison.Ordinal))
                {
                    DBConnectionstring += ";";
                }
                DBConnectionstring += "charset=utf8mb4";
            }
            DbConnectionStringBuilder dBConnectionStringBuilder = new MySqlConnectionStringBuilder(DBConnectionstring);
            Database = dBConnectionStringBuilder["database"].ToString();
            User = dBConnectionStringBuilder["uid"].ToString();
            Password = dBConnectionStringBuilder["password"].ToString();
            if (obfuscate && DBConnectionstring.ToLower(Tools.ciEnUS).Contains("password="))
            {
                Match m = Regex.Match(DBConnectionstring.ToLower(Tools.ciEnUS), "password=(.+?);");
                if (m.Success && m.Groups.Count == 2 && m.Groups[1].Captures.Count == 1)
                {
                    return DBConnectionstring.ToLower(Tools.ciEnUS).Replace(string.Concat("password=", m.Groups[1].Captures[0].ToString()), string.Concat("password=", Tools.ObfuscateString(m.Groups[1].Captures[0].ToString())));
                }
            }
            _DBConnectionstring = DBConnectionstring;
            return _DBConnectionstring;
        }
    }}