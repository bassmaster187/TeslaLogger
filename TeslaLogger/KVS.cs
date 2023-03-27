﻿using System;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;

namespace TeslaLogger
{
    internal static class KVS
    {

        internal const int SUCCESS = 1;
        internal const int FAILURE = 0;

        internal static void CheckSchema()
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

        internal static int InsertOrUpdate(string key, int value)
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
            return FAILURE;
        }

        internal static int InsertOrUpdate(string key, double value)
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
            return FAILURE;
        }

        internal static int InsertOrUpdate(string key, bool value)
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
            return FAILURE;
        }

        internal static int InsertOrUpdate(string key, DateTime value)
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
            return FAILURE;
        }

        internal static int InsertOrUpdate(string key, string value)
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
            return FAILURE;
        }

        // defaults to false, check return code for SUCCESS
        internal static int Get(string key, out bool value) 
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
                    Tools.DebugLog(cmd);
                    MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                    if (dr.Read() && dr[0] != DBNull.Value && Boolean.TryParse(dr[0].ToString(), out value))
                    {
                        return SUCCESS;
                    }
                }
            }
            value = false;
            return FAILURE;
        }

        // defaults to DateTime.MinValue, check return code for SUCCESS
        internal static int Get(string key, out DateTime value)
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
                    Tools.DebugLog(cmd);
                    MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                    if (dr.Read() && dr[0] != DBNull.Value && DateTime.TryParse(dr[0].ToString(), out value))
                    {
                        return SUCCESS;
                    }
                }
            }
            value = DateTime.MinValue;
            return FAILURE;
        }

        // defaults to double.NaN, check return code for SUCCESS
        internal static int Get(string key, out double value)
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
                    Tools.DebugLog(cmd);
                    MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                    if (dr.Read() && dr[0] != DBNull.Value && Double.TryParse(dr[0].ToString(), out value))
                    {
                        return SUCCESS;
                    }
                }
            }
            value = double.NaN;
            return FAILURE;
        }

        // defaults to int.MinValue, check return code for SUCCESS
        internal static int Get(string key, out int value)
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
                    Tools.DebugLog(cmd);
                    MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                    if (dr.Read() && dr[0] != DBNull.Value && int.TryParse(dr[0].ToString(), out value))
                    {
                        return SUCCESS;
                    }
                }
            }
            value = int.MinValue;
            return FAILURE;
        }

        // defaults to {} (empty JSON), check return code for SUCCESS
        internal static int Get(string key, out string value)
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
                    Tools.DebugLog(cmd);
                    MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                    if (dr.Read() && dr[0] != DBNull.Value)
                    {
                        value = dr[0].ToString();
                        return SUCCESS;
                    }
                }
            }
            value = "{}";
            return FAILURE;
        }

        internal static int Remove(string key)
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
            return FAILURE;
        }
    }
}

