using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.Net;

namespace TeslaLogger
{
    class MapQuest
    {

        static string mapdir = "/var/lib/grafana/plugins/teslalogger-timeline-panel/dist/maps";
        const string addressfilter = "replace(replace(replace(replace(replace(convert(address USING ascii), '?',''),' ',''),'/',''),'&',''),',','') as name";

        static MapQuest()
        {
            try
            {
                if (!System.IO.Directory.Exists(mapdir))
                    System.IO.Directory.CreateDirectory(mapdir);
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        public static void CreateTripMap(int startpos, int endpos, int Carid)
        {
            if (startpos == 0)
                return;

            // https://open.mapquestapi.com/staticmap/v5/map?key=ulMOOlevG9FunIVobQB2BG2GA0EdCjjH&boundingBox=38.915,-77.072,38.876,-77.001&size=200,150&type=dark
            try
            {
                if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
                    return;

                string fn = "T" + Carid + "-" + startpos + "-" + endpos + ".jpg";
                string filename = System.IO.Path.Combine(mapdir, fn);

                if (System.IO.File.Exists(filename))
                    return;

                using (DataTable dt = new DataTable())
                {
                    using (MySqlDataAdapter da = new MySqlDataAdapter("SELECT lat,lng FROM pos where id between @start and @end and carid = @carid", DBHelper.DBConnectionstring))
                    {
                        da.SelectCommand.Parameters.AddWithValue("@start", startpos);
                        da.SelectCommand.Parameters.AddWithValue("@end", endpos);
                        da.SelectCommand.Parameters.AddWithValue("@carid", Carid);

                        da.Fill(dt);

                        double latmin = Convert.ToDouble(dt.Compute("min(lat)", String.Empty));
                        double latmax = Convert.ToDouble(dt.Compute("max(lat)", String.Empty));
                        double lngmin = Convert.ToDouble(dt.Compute("min(lng)", String.Empty));
                        double lngmax = Convert.ToDouble(dt.Compute("max(lng)", String.Empty));

                        StringBuilder sb = new StringBuilder();
                        sb.Append("http://open.mapquestapi.com/staticmap/v5/map?key=");
                        sb.Append(ApplicationSettings.Default.MapQuestKey);
                        sb.Append("&boundingBox=");
                        sb.Append(latmin.ToString(Tools.ciEnUS)).Append(",");
                        sb.Append(lngmin.ToString(Tools.ciEnUS)).Append(",");
                        sb.Append(latmax.ToString(Tools.ciEnUS)).Append(",");
                        sb.Append(lngmax.ToString(Tools.ciEnUS));
                        sb.Append("&size=200,150&type=dark");
                        sb.Append("&locations=");
                        sb.Append(Convert.ToDouble(dt.Rows[0]["lat"]).ToString(Tools.ciEnUS)).Append(",").Append(Convert.ToDouble(dt.Rows[0]["lng"]).ToString(Tools.ciEnUS));
                        sb.Append("|marker-start||");
                        sb.Append(Convert.ToDouble(dt.Rows[dt.Rows.Count - 1]["lat"]).ToString(Tools.ciEnUS)).Append(",").Append(Convert.ToDouble(dt.Rows[dt.Rows.Count - 1]["lng"]).ToString(Tools.ciEnUS));
                        sb.Append("|marker-end");
                        sb.Append("&shape=");

                        bool first = true;
                        int posquery = 0;

                        if (dt.Rows.Count < 4)
                            return;

                        int step = dt.Rows.Count / 200;
                        if (step == 0)
                            step = 1;

                        for (int pos = 0; pos < dt.Rows.Count; pos++)
                        {
                            DataRow dr = dt.Rows[pos];

                            if (!(pos % step == 0))
                            {
                                continue;
                            }


                            if (first)
                                first = false;
                            else
                                sb.Append("|");

                            sb.Append(Convert.ToDouble(dr["lat"]).ToString(Tools.ciEnUS)).Append(",").Append(Convert.ToDouble(dr["lng"]).ToString(Tools.ciEnUS));
                            posquery++;
                        }
                        string url = sb.ToString();
                        System.Diagnostics.Debug.WriteLine(url);

                        try
                        {
                            using (WebClient webClient = new WebClient())
                            {
                                webClient.Headers.Add("User-Agent: TeslaLogger");
                                webClient.Headers.Add("Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");

                                // Download the Web resource and save it into the current filesystem folder.
                                webClient.DownloadFile(url, filename);
                                Logfile.Log("Create File: " + fn);
                            }

                            System.Threading.Thread.Sleep(1000);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.ToString());
                            Logfile.Log("Rows count: " + dt.Rows.Count + " posquery= " + posquery + "\r\n" + ex.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                Logfile.Log(ex.ToString());
            }
        }

        public static void CreateChargingMap(double lat, double lng, string name)
        {
            try
            {
                if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
                    return;

                string fn = "C-" + name + ".jpg";
                string filename = System.IO.Path.Combine(mapdir, fn);

                if (System.IO.File.Exists(filename))
                    return;

                StringBuilder sb = new StringBuilder();
                sb.Append("http://open.mapquestapi.com/staticmap/v5/map?key=");
                sb.Append(ApplicationSettings.Default.MapQuestKey);
                sb.Append("&center=");
                sb.Append(lat.ToString(Tools.ciEnUS)).Append(",");
                sb.Append(lng.ToString(Tools.ciEnUS));
                sb.Append("&size=200,150&type=dark");
                sb.Append("&locations=");
                sb.Append(lat.ToString(Tools.ciEnUS)).Append(",").Append(lng.ToString(Tools.ciEnUS));
                sb.Append("|marker-E3AE32|");

                string url = sb.ToString();
                System.Diagnostics.Debug.WriteLine(url);

                try
                {
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.Headers.Add("User-Agent: TeslaLogger");
                        webClient.Headers.Add("Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");

                        // Download the Web resource and save it into the current filesystem folder.
                        webClient.DownloadFile(url, filename);

                        Logfile.Log("Create File: " + fn);
                    }

                    System.Threading.Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    Logfile.Log(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        public static void CreateParkingMap(double lat, double lng, string name)
        {
            try
            {

                if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
                    return;

                string fn = "P-" + name + ".jpg";
                string filename = System.IO.Path.Combine(mapdir, fn);

                if (System.IO.File.Exists(filename))
                    return;

                StringBuilder sb = new StringBuilder();
                sb.Append("http://open.mapquestapi.com/staticmap/v5/map?key=");
                sb.Append(ApplicationSettings.Default.MapQuestKey);
                sb.Append("&center=");
                sb.Append(lat.ToString(Tools.ciEnUS)).Append(",");
                sb.Append(lng.ToString(Tools.ciEnUS));
                sb.Append("&size=200,150&type=dark");
                sb.Append("&locations=");
                sb.Append(lat.ToString(Tools.ciEnUS)).Append(",").Append(lng.ToString(Tools.ciEnUS));
                sb.Append("|marker-3E72B1|");

                string url = sb.ToString();
                System.Diagnostics.Debug.WriteLine(url);

                try
                {
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.Headers.Add("User-Agent: TeslaLogger");
                        webClient.Headers.Add("Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");

                        // Download the Web resource and save it into the current filesystem folder.
                        webClient.DownloadFile(url, filename);

                        Logfile.Log("Create File: " + fn);
                    }

                    System.Threading.Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    Logfile.Log(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        public static void createAllTripMaps()
        {
            try
            {
                if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
                    return;

                Logfile.Log("createAllTripMaps");

                using (DataTable dt = new DataTable())
                {
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();

                        using (MySqlCommand cmd = new MySqlCommand("SELECT startposid, endposid, carid FROM teslalogger.trip order by startdate desc ", con))
                        {
                            MySqlDataReader dr = cmd.ExecuteReader();

                            try
                            {
                                while (dr.Read())
                                {
                                    CreateTripMap(Convert.ToInt32(dr["startposid"]), Convert.ToInt32(dr["endposid"]), Convert.ToInt32(dr["carid"]));
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(ex.ToString());
                                Logfile.Log(ex.ToString());
                            }
                        }
                    }
                }

                Logfile.Log("createAllTripMaps finish");

            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        public static void createAllChargigMaps()
        {
            if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
                return;

            try
            {
                Logfile.Log("createAllChargigMaps");

                using (DataTable dt = new DataTable())
                {
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();

                        using (MySqlCommand cmd = new MySqlCommand($@"SELECT avg(lat) as lat, avg(lng) as lng, {addressfilter} 
                    FROM chargingstate join pos on chargingstate.pos = pos.id
                    group by address", con))
                        {
                            MySqlDataReader dr = cmd.ExecuteReader();

                            try
                            {
                                while (dr.Read())
                                {
                                    CreateChargingMap(Convert.ToDouble(dr["lat"]), Convert.ToDouble(dr["lng"]), dr["name"].ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(ex.ToString());
                                Logfile.Log(ex.ToString());
                            }
                        }
                    }
                }
                Logfile.Log("createAllChargigMaps finish");
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        public static void createAllParkingMaps()
        {
            if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
                return;

            try
            {
                Logfile.Log("createAllParkingMaps");

                using (DataTable dt = new DataTable())
                {
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();

                        using (MySqlCommand cmd = new MySqlCommand($@"Select avg(lat) as lat, avg(lng) as lng, {addressfilter} 
                        from pos    
                        left join chargingstate on pos.id = chargingstate.pos
                        where pos.id in (SELECT Pos FROM chargingstate) or pos.id in (SELECT StartPos FROM drivestate) or pos.id in (SELECT EndPos FROM drivestate)
                        group by address", con))
                        {
                            MySqlDataReader dr = cmd.ExecuteReader();

                            try
                            {
                                while (dr.Read())
                                {
                                    CreateParkingMap(Convert.ToDouble(dr["lat"]), Convert.ToDouble(dr["lng"]), dr["name"].ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(ex.ToString());
                                Logfile.Log(ex.ToString());
                            }
                        }
                    }
                }
                Logfile.Log("createAllParkingMaps finish");
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        public static void CreateParkingMapFromPosid(int Posid)
        {
            if (Posid == 0)
                return;

            if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
                return;

            try
            {
                Logfile.Log("CreateParkingMapFromPosid: " + Posid);

                using (DataTable dt = new DataTable())
                {
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();

                        using (MySqlCommand cmd = new MySqlCommand($"select lat, lng, {addressfilter} from pos where id = @id", con))
                        {
                            cmd.Parameters.AddWithValue("@id", Posid);
                            MySqlDataReader dr = cmd.ExecuteReader();

                            try
                            {
                                while (dr.Read())
                                {
                                    CreateParkingMap(Convert.ToDouble(dr["lat"]), Convert.ToDouble(dr["lng"]), dr["name"].ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(ex.ToString());
                                Logfile.Log(ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        public static void CreateChargingMapOnChargingCompleted(int carid)
        {
            string sql = $"select {addressfilter} , lat, lng from chargingstate join pos on chargingstate.pos = pos.id where EndDate is null and chargingstate.CarID=@CarID";

            if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
                return;

            try
            {
                Logfile.Log("CreateChargingMapOnChargingCompleted");

                using (DataTable dt = new DataTable())
                {
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();

                        using (MySqlCommand cmd = new MySqlCommand(sql, con))
                        {
                            cmd.Parameters.AddWithValue("@CarID", carid);

                            MySqlDataReader dr = cmd.ExecuteReader();

                            try
                            {
                                while (dr.Read())
                                {
                                    CreateChargingMap(Convert.ToDouble(dr["lat"]), Convert.ToDouble(dr["lng"]), dr["name"].ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(ex.ToString());
                                Logfile.Log(ex.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }
    }
}
