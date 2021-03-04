using System;
using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;
using static TeslaLogger.StaticMapProvider;

namespace TeslaLogger
{
    public class StaticMapService
    {
        internal abstract class Request
        {
            private MapMode mode = MapMode.Regular;
            private MapSpecial special = MapSpecial.None;
            private MapType type = MapType.Trip;
            private int width = 0;
            private int height = 0;

            internal MapType Type { get => type; set => type = value; }
            internal int Width { get => width; set => width = value; }
            internal int Height { get => height; set => height = value; }
            internal MapMode Mode { get => mode; set => mode = value; }
            internal MapSpecial Special { get => special; set => special = value; }
        }

        internal class TripRequest : Request
        {
            public TripRequest(int startPosID, int endPosID, int width, int height, MapType type, MapMode mode, MapSpecial special)
            {
                StartPosID = startPosID;
                EndPosID = endPosID;
                Width = width;
                Height = height;
                Type = type;
                Mode = mode;
                Special = special;
            }

            public int StartPosID { get; }
            public int EndPosID { get; }
        }

        internal class POIRequest : Request
        {
            private string name = "invalid";

            public POIRequest(MapType charge, double lat, double lng, string name)
            {
                Charge = charge;
                Lat = lat;
                Lng = lng;
                Name = name;
            }

            public MapType Charge { get; }
            public string Name { get; }
            public double Lat { get; }
            public double Lng { get; }
        }

        private static StaticMapService _StaticMapService = null;
        private static StaticMapProvider _StaticMapProvider = null;

        private readonly ConcurrentQueue<Request> queue = new ConcurrentQueue<Request>();

        const string addressfilter = "replace(replace(replace(replace(replace(convert(address USING ascii), '?',''),' ',''),'/',''),'&',''),',','') AS addr";

        private StaticMapService()
        {
            Logfile.Log("StaticMapService initialized");
        }

        public static StaticMapService GetSingleton()
        {
            if (_StaticMapService == null)
            {
                _StaticMapService = new StaticMapService();
                StaticMapProvider _StaticMapProvider = StaticMapProvider.GetSingleton();
                Logfile.Log("selected MapProvider: " + _StaticMapProvider.GetType());
            }
            return _StaticMapService;
        }

        public int GetQueueLength()
        {
            return queue.Count;
        }

        public void Enqueue(int startPosID, int endPosID, int width, int height, MapMode mode, MapSpecial special)
        {
            queue.Enqueue(new TripRequest(startPosID, endPosID, width, height, MapType.Trip, mode, special));
        }

        private void Enqueue(MapType type, double lat, double lng, string name)
        {
            queue.Enqueue(new POIRequest(type, lat, lng, name));
        }

        public void Run()
        {
            Tools.DebugLog("StaticMapService:Run()");
            try
            {
                while (true)
                {
                    if (!queue.IsEmpty)
                    {
                        Work();
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog("StaticMapService: Exception", ex);
            }
        }

        private void Work()
        {
            Tools.DebugLog("StaticMapService:Work() queue:" + queue.Count);
            if (_StaticMapProvider != null)
            {
                if (queue.TryDequeue(out Request request))
                {
                    int width = request.Width > 0 ? request.Width : 240;
                    int height = request.Height > 0 ? request.Height : (int)(width / 1.618033);
                    if (request is TripRequest)
                    {
                        Tools.DebugLog($"StaticMapService:Work() request:{request.Type} {((TripRequest)request).StartPosID}->{((TripRequest)request).EndPosID}");
                        using (DataTable dt = TripToCoords((TripRequest)request, out int CarID))
                        {
                            string filename = System.IO.Path.Combine(GetMapDir(), GetMapFileName(CarID, ((TripRequest)request).StartPosID, ((TripRequest)request).EndPosID));
                            if (DeleteOldMapFile(filename))
                            {
                                _StaticMapProvider.CreateTripMap(dt, width, height, request.Mode == MapMode.Dark ? MapMode.Dark : MapMode.Regular, request.Special, filename);
                            }
                        }
                    }
                    else if (request is POIRequest)
                    {
                        Tools.DebugLog($"StaticMapService:Work() request:{request.Type} {((POIRequest)request).Name}");
                        string filename = System.IO.Path.Combine(GetMapDir(), GetMapFileName(request.Type, ((POIRequest)request).Name));
                        if (DeleteOldMapFile(filename))
                        {
                            switch (request.Type)
                            {
                                case MapType.Charge:
                                    _StaticMapProvider.CreateChargingMap(((POIRequest)request).Lat, ((POIRequest)request).Lng, width, height, request.Mode, request.Special, filename);
                                    break;
                                case MapType.Park:
                                    _StaticMapProvider.CreateParkingMap(((POIRequest)request).Lat, ((POIRequest)request).Lng, width, height, request.Mode, request.Special, filename);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private DataTable TripToCoords(TripRequest request, out int CarID)
        {
            DataTable dt = new DataTable();
            CarID = int.MinValue;
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  CarID
FROM
  pos
WHERE
  id = @startID ", con))
                {
                    cmd.Parameters.AddWithValue("@startID", request.StartPosID);
                    Tools.DebugLog(cmd);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        int.TryParse(dr[0].ToString(), out CarID);
                    }
                }
            }
            if (CarID != int.MinValue && CarID > 0)
            {
                using (MySqlDataAdapter da = new MySqlDataAdapter(@"
SELECT
  lat,
  lng
FROM
  pos
WHERE
  CarID = @CarID
  AND id >= @startID
  AND id <= @endID
ORDER BY
  Datum", DBHelper.DBConnectionstring))
                {
                    da.SelectCommand.Parameters.AddWithValue("@CarID", CarID);
                    da.SelectCommand.Parameters.AddWithValue("@startID", request.StartPosID);
                    da.SelectCommand.Parameters.AddWithValue("@endID", request.EndPosID);
                    Tools.DebugLog(da.SelectCommand);
                    da.Fill(dt);
                }
            }
            else
            {
                Tools.DebugLog($"TripToCoords: could not find CarID for pos {request.StartPosID}");
            }
            return dt;
        }

        public static void CreateAllChargingMaps()
        {
            using (DataTable dt = new DataTable())
            {
                using (MySqlDataAdapter da = new MySqlDataAdapter($@"
SELECT
  AVG(lat) AS lat,
  AVG(lng) AS lng,
  {addressfilter}
FROM
  chargingstate
JOIN pos ON
  chargingstate.pos = pos.id
GROUP BY
  address", DBHelper.DBConnectionstring))
                {
                    da.Fill(dt);
                }
                foreach (DataRow dr in dt.Rows)
                {
                    GetSingleton().Enqueue(MapType.Charge, (double)dr["lat"], (double)dr["lng"], dr["addr"].ToString());
                }
            }
        }

        public static void CreateAllParkingMaps()
        {
            using (DataTable dt = new DataTable())
            {
                using (MySqlDataAdapter da = new MySqlDataAdapter($@"
SELECT
  AVG(lat) AS lat,
  AVG(lng) AS lng,
  {addressfilter}
FROM
  pos    
LEFT JOIN
  chargingstate ON pos.id = chargingstate.pos
WHERE
  pos.id IN (
  SELECT
    pos
  FROM
    chargingstate
  )
  OR pos.id IN (
  SELECT
    StartPos
  FROM
    drivestate
  )
  OR pos.id IN (
  SELECT
    EndPos
  FROM
    drivestate
  )
GROUP BY
  address", DBHelper.DBConnectionstring))
                {
                    da.Fill(dt);
                }
                foreach (DataRow dr in dt.Rows)
                {
                    GetSingleton().Enqueue(MapType.Park, (double)dr["lat"], (double)dr["lng"], dr["addr"].ToString());
                }
            }
        }

        public static void CreateAllTripMaps()
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();

                using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  startposid,
  endposid,
  carid
FROM
  trip
ORDER BY
  startdate DESC ", con))
                {
                    MySqlDataReader dr = cmd.ExecuteReader();

                    try
                    {
                        while (dr.Read())
                        {
                            GetSingleton().Enqueue(Convert.ToInt32(dr["startposid"]), Convert.ToInt32(dr["endposid"]), 0, 0, MapMode.Dark, MapSpecial.None);
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

        public static string GetMapDir()
        {
            string mapdir = "/var/lib/grafana/plugins/teslalogger-timeline-panel/dist/maps";
            try
            {
                if (!System.IO.Directory.Exists(mapdir))
                {
                    System.IO.Directory.CreateDirectory(mapdir);
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
            return mapdir;
        }

        private static string GetMapFileName(StaticMapProvider.MapType type, string name)
        {
            switch (type)
            {
                case StaticMapProvider.MapType.Trip:
                    break;
                case StaticMapProvider.MapType.Charge:
                    return "C-" + name + ".png";
                    break;
                case StaticMapProvider.MapType.Park:
                    return "P-" + name + ".png";
                    break;
            }
            return "error.png";
        }

        public static string GetMapFileName(int carID, int startpos, int endpos)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("T-");
            sb.Append(carID);
            sb.Append("-");
            sb.Append(startpos);
            sb.Append("-");
            sb.Append(endpos);
            sb.Append(".png");
            return sb.ToString();
        }



        private static bool DeleteOldMapFile(string filename)
        {
            try
            {
                // check file age
                if (File.Exists(filename))
                {
                    if ((DateTime.UtcNow - File.GetCreationTimeUtc(filename)).TotalDays > 90)
                    {
                        File.Delete(filename);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
            return false;
        }

        internal void CreateChargingMapOnChargingCompleted(int CarID)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand($@"
SELECT
  lat,
  lng,
  {addressfilter}
FROM
 chargingstate
JOIN pos ON
  chargingstate.pos = pos.id
WHERE
  EndDate IS NULL
  AND chargingstate.CarID = @CarID", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", CarID);

                        MySqlDataReader dr = cmd.ExecuteReader();

                        try
                        {
                            while (dr.Read())
                            {
                                GetSingleton().Enqueue(MapType.Charge, (double)dr["lat"], (double)dr["lng"], dr["addr"].ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            Logfile.Log(ex.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        internal void CreateParkingMapFromPosid(int posID)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand($@"
SELECT
  lat,
  lng,
  {addressfilter}
FROM
  pos
WHERE
  id = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", posID);
                        MySqlDataReader dr = cmd.ExecuteReader();

                        try
                        {
                            while (dr.Read())
                            {
                                GetSingleton().Enqueue(MapType.Charge, Convert.ToDouble(dr["lat"]), Convert.ToDouble(dr["lng"]), dr["name"].ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            Logfile.Log(ex.ToString());
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
