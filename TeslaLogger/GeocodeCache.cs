using System;
using System.Data;
using Exceptionless;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    internal class GeocodeCache
    {
        internal static void CheckSchema()
        {
            if (!DBHelper.TableExists("geocodecache"))
            {
                Logfile.Log(@"
CREATE TABLE geocodecache(
    lat DOUBLE NOT NULL,
    lng DOUBLE NOT NULL,
    lastUpdate DATE NOT NULL,
    address LONGTEXT,
    UNIQUE ix_key(lat, lng)
) ENGINE = InnoDB CHARSET = utf8mb4 COLLATE utf8mb4_unicode_ci;");
                DBHelper.ExecuteSQLQuery(@"
CREATE TABLE geocodecache(
    lat DOUBLE NOT NULL,
    lng DOUBLE NOT NULL,
    lastUpdate DATE NOT NULL,
    address LONGTEXT,
    UNIQUE ix_key(lat, lng)
) ENGINE = InnoDB CHARSET = utf8mb4 COLLATE utf8mb4_unicode_ci;");
                Logfile.Log("CREATE TABLE OK");
                KVS.InsertOrUpdate("GeocodeCacheSchemaVersion", (int)1);
                MigrateSchema0to1();
            }
            else if (KVS.Get("GeocodeCacheSchemaVersion", out int geocodeCacheSchemaVersion) == KVS.SUCCESS)
            {
                // placeholder for future schema migrations
            }
        }

        private static void MigrateSchema0to1()
        {
            Logfile.Log("GeocodeCache: migrating schema from version 0 to version 1 ...");
            DataTable dt = new DataTable("cache");
            DataColumn dtlat = dt.Columns.Add("lat", typeof(double));
            DataColumn dtlng = dt.Columns.Add("lng", typeof(double));
            dt.Columns.Add("Value");
            dt.PrimaryKey = new DataColumn[] { dtlat, dtlng };
            try
            {
                if (System.IO.File.Exists(FileManager.GetFilePath(TLFilename.GeocodeCache)))
                {
#pragma warning disable CA3075 // Unsichere DTD-Verarbeitung in XML
                    _ = dt.ReadXml(FileManager.GetFilePath(TLFilename.GeocodeCache));
#pragma warning restore CA3075 // Unsichere DTD-Verarbeitung in XML
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (Double.TryParse(dr?["lat"].ToString(), out double lat)
                            && Double.TryParse(dr?["lng"].ToString(), out double lng)
                            && dr?["value"].ToString().Length > 0)
                        {
                            Insert(lat, lng, dr?["value"].ToString());
                        }
                    }
                    System.IO.File.Delete(FileManager.GetFilePath(TLFilename.GeocodeCache));
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, "");
                Logfile.Log(ex.ToString());
            }
            dt.Dispose();
            Logfile.Log("GeocodeCache: migrating schema from version 0 to version 1 ... done");
        }

        internal static string Search(double lat, double lng)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    address
FROM
    geocodecache
WHERE
    lat = @lat
    and lng = @lng", con))
                    {
                        cmd.Parameters.AddWithValue("@lat", lat);
                        cmd.Parameters.AddWithValue("@lng", lng);

                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read())
                        {
                            return dr[0].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().Submit();
                Logfile.Log(ex.ToString());
            }
            return String.Empty;
        }


        internal static void Insert(double lat, double lng, string address)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(@"
INSERT
    geocodecache(
        lat,
        lng,
        lastUpdate,
        address
    )
VALUES(
        @lat,
        @lng,
        @lastUpdate,
        @address
)
", con))
                    {
                        cmd.Parameters.AddWithValue("@lat", lat);
                        cmd.Parameters.AddWithValue("@lng", lng);
                        cmd.Parameters.AddWithValue("@lastUpdate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@address", address);
                        SQLTracer.TraceNQ(cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().Submit();
                Logfile.Log(ex.ToString());
            }
        }

        internal static void Cleanup(int days = 90)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand(@"
DELETE
FROM
    geocodecache
WHERE
    lastUpdate < @lastUpdate
", con))
                    {
                        cmd.Parameters.AddWithValue("@lastUpdate", DateTime.Now.AddDays(-days));
                        SQLTracer.TraceNQ(cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().Submit();
                Logfile.Log(ex.ToString());
            }
        }
    }
}
