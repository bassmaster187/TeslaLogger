using System;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;

namespace TeslaLogger
{
    internal static class KVS
    {

        internal const int SUCCESS = 0;
        internal const int FAILURE = 1;

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
                DBHelper.ExecuteSQLQuery("CREATE TABLE kvs");
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
                    cmd.Parameters.AddWithValue("@id", key);
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
    }
}

