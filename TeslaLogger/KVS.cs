using System;
using Exceptionless;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    internal static class KVS
    {

        internal const int SUCCESS = 1;
        internal const int FAILURE = 0;

        internal static void CheckSchema()
        {
            try
            {
                if (!DBHelper.TableExists("kvs"))
                {
                    Logfile.Log(@"
CREATE TABLE kvs(
    id VARCHAR(64) NOT NULL,
    ivalue INT NULL,
    dvalue DOUBLE NULL,
    bvalue BOOLEAN NULL,
    ts DATE NULL,
    JSON LONGTEXT NULL,
    UNIQUE ix_key(id)
) ENGINE = InnoDB CHARSET = utf8mb4 COLLATE utf8mb4_unicode_ci;");
                    UpdateTeslalogger.AssertAlterDB();
                    DBHelper.ExecuteSQLQuery(@"
CREATE TABLE kvs(
    id VARCHAR(64) NOT NULL,
    ivalue INT NULL,
    dvalue DOUBLE NULL,
    bvalue BOOLEAN NULL,
    ts DATE NULL,
    JSON LONGTEXT NULL,
    UNIQUE ix_key(id)
) ENGINE = InnoDB CHARSET = utf8mb4 COLLATE utf8mb4_unicode_ci;");
                    Logfile.Log("CREATE TABLE OK");
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("KVS: Exception", ex);
            }
        }

        internal static int InsertOrUpdate(string key, int value)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
INSERT INTO kvs SET
    id = @key,
    ivalue = @value
ON DUPLICATE KEY UPDATE
    id = @key,
    ivalue = @value", con))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        cmd.Parameters.AddWithValue("@value", value);
                        int rowsAffected = SQLTracer.TraceNQ(cmd);
                        if (rowsAffected == 1 // INSERT
                            || rowsAffected == 2 // DELETE and INSERT
                           )
                        {
                            return SUCCESS;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("KVS: Exception", ex);
            }
            return FAILURE;
        }

        internal static int InsertOrUpdate(string key, double value)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
INSERT INTO kvs SET
    id = @key,
    dvalue = @value
ON DUPLICATE KEY UPDATE
    id = @key,
    dvalue = @value", con))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        cmd.Parameters.AddWithValue("@value", value);
                        int rowsAffected = SQLTracer.TraceNQ(cmd);
                        if (rowsAffected == 1 // INSERT
                            || rowsAffected == 2 // DELETE and INSERT
                           )
                        {
                            return SUCCESS;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("KVS: Exception", ex);
            }
            return FAILURE;
        }

        internal static int InsertOrUpdate(string key, bool value)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
INSERT INTO kvs SET
    id = @key,
    bvalue = @value
ON DUPLICATE KEY UPDATE
    id = @key,
    bvalue = @value", con))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        cmd.Parameters.AddWithValue("@value", value);
                        int rowsAffected = SQLTracer.TraceNQ(cmd);
                        if (rowsAffected == 1 // INSERT
                            || rowsAffected == 2 // DELETE and INSERT
                           )
                        {
                            return SUCCESS;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("KVS: Exception", ex);
            }
            return FAILURE;
        }

        internal static int InsertOrUpdate(string key, DateTime value)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
INSERT INTO kvs SET
    id = @key,
    ts = @value
ON DUPLICATE KEY UPDATE
    id = @key,
    ts = @value", con))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        cmd.Parameters.AddWithValue("@value", value);
                        int rowsAffected = SQLTracer.TraceNQ(cmd);
                        if (rowsAffected == 1 // INSERT
                            || rowsAffected == 2 // DELETE and INSERT
                           )
                        {
                            return SUCCESS;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("KVS: Exception", ex);
            }
            return FAILURE;
        }

        internal static int InsertOrUpdate(string key, string value)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
INSERT INTO kvs SET
    id = @key,
    JSON = @value
ON DUPLICATE KEY UPDATE
    id = @key,
    JSON = @value", con))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        cmd.Parameters.AddWithValue("@value", value);
                        int rowsAffected = SQLTracer.TraceNQ(cmd);
                        if (rowsAffected == 1 // INSERT
                            || rowsAffected == 2 // DELETE and INSERT
                           )
                        {
                            return SUCCESS;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("KVS: Exception", ex);
            }
            return FAILURE;
        }

        // defaults to false, check return code for SUCCESS
        internal static int Get(string key, out bool value)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    bvalue
FROM
    kvs
WHERE
    id = @key", con))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read() && dr[0] != DBNull.Value && Boolean.TryParse(dr[0].ToString(), out value))
                        {
                            return SUCCESS;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("KVS: Exception", ex);
            }
            value = false;
            return FAILURE;
        }

        // defaults to DateTime.MinValue, check return code for SUCCESS
        internal static int Get(string key, out DateTime value)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    ts
FROM
    kvs
WHERE
    id = @key", con))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read() && dr[0] != DBNull.Value && DateTime.TryParse(dr[0].ToString(), out value))
                        {
                            return SUCCESS;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("KVS: Exception", ex);
            }
            value = DateTime.MinValue;
            return FAILURE;
        }

        // defaults to double.NaN, check return code for SUCCESS
        internal static int Get(string key, out double value)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    dvalue
FROM
    kvs
WHERE
    id = @key", con))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read() && dr[0] != DBNull.Value && Double.TryParse(dr[0].ToString(), out value))
                        {
                            return SUCCESS;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("KVS: Exception", ex);
            }
            value = double.NaN;
            return FAILURE;
        }

        // defaults to int.MinValue, check return code for SUCCESS
        internal static int Get(string key, out int value)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    ivalue
FROM
    kvs
WHERE
    id = @key", con))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read() && dr[0] != DBNull.Value && int.TryParse(dr[0].ToString(), out value))
                        {
                            return SUCCESS;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("KVS: Exception", ex);
            }
            value = int.MinValue;
            return FAILURE;
        }

        // defaults to {} (empty JSON), check return code for SUCCESS
        internal static int Get(string key, out string value)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    JSON
FROM
    kvs
WHERE
    id = @key", con))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read() && dr[0] != DBNull.Value)
                        {
                            value = dr[0].ToString();
                            return SUCCESS;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("KVS: Exception", ex);
            }
            value = "{}";
            return FAILURE;
        }

        internal static int Remove(string key)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
DELETE FROM
    kvs
WHERE
    id = @key", con))
                    {
                        cmd.Parameters.AddWithValue("@id", key);
                        int rowsAffected = SQLTracer.TraceNQ(cmd);
                        if (rowsAffected == 1) // DELETE
                        {
                            return SUCCESS;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("KVS: Exception", ex);
            }
            return FAILURE;
        }
    }
}
