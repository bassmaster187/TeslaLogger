using System;
using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;
using static TeslaLogger.StaticMapProvider;
using Exceptionless;


namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "<Pending>")]
    public class StaticMapService
    {
        internal abstract class Request
        {
            private MapMode mode = MapMode.Regular;
            private MapSpecial special = MapSpecial.None;
            private MapType type = MapType.Trip;
            private int width; // defaults to 0;
            private int height; // defaults to 0;

            internal MapType Type { get => type; set => type = value; }
            internal int Width { get => width; set => width = value; }
            internal int Height { get => height; set => height = value; }
            internal MapMode Mode { get => mode; set => mode = value; }
            internal MapSpecial Special { get => special; set => special = value; }
        }

        internal class TripRequest : Request
        {
            public TripRequest(int carID, int startPosID, int endPosID, MapType type, MapMode mode, MapSpecial special, int width = 0, int height = 0)
            {
                StartPosID = startPosID;
                EndPosID = endPosID;
                Width = width;
                Height = height;
                Type = type;
                Mode = mode;
                Special = special;
                CarID = carID;
            }

            public int StartPosID { get; }
            public int EndPosID { get; }
            public int CarID { get; }
        }

        internal class POIRequest : Request
        {
            public POIRequest(MapType type, double lat, double lng, MapMode mode, int width = 0, int height = 0)
            {
                Type = type;
                Lat = lat;
                Lng = lng;
                Width = width;
                Height = height;
                Mode = mode;
            }

            public double Lat { get; }
            public double Lng { get; }
        }

        private static StaticMapService _StaticMapService; // defaults to null;
        private static StaticMapProvider _StaticMapProvider; // defaults to null;

        private readonly ConcurrentQueue<Request> queue = new ConcurrentQueue<Request>();

        private StaticMapService()
        {
            Logfile.Log("StaticMapService initialized");
        }

        public static StaticMapService GetSingleton()
        {
            if (_StaticMapService == null)
            {
                _StaticMapService = new StaticMapService();
                _StaticMapProvider = StaticMapProvider.GetSingleton();
                Logfile.Log("selected MapProvider: " + _StaticMapProvider.GetType());
            }
            return _StaticMapService;
        }

        public int GetQueueLength()
        {
            return queue.Count;
        }

        public void Enqueue(int CarID, int startPosID, int endPosID, int width, int height, MapMode mode, MapSpecial special)
        {
            queue.Enqueue(new TripRequest(CarID, startPosID, endPosID, MapType.Trip, mode, special, width, height));
        }

        private void Enqueue(MapType type, double lat, double lng, MapMode mode)
        {
            queue.Enqueue(new POIRequest(type, lat, lng, mode));
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
                        try
                        {
                            Work();
                        }
                        catch (Exception ex)
                        {
                            ex.ToExceptionless().FirstCarUserID().Submit();
                            Tools.DebugLog("StaticMapService: Exception", ex);
                        }

                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("StaticMapService: Exception", ex);
            }
        }

        private void Work()
        {
            //Tools.DebugLog("StaticMapService:Work() queue:" + queue.Count + " MapProvider:" + _StaticMapProvider);
            if (_StaticMapProvider != null)
            {
                if (queue.TryDequeue(out Request request))
                {
                    //Tools.DebugLog("StaticMapService:Work() queue:" + queue.Count + " MapProvider:" + _StaticMapProvider);
                    int width = request.Width > 0 ? request.Width : 200;
                    int height = request.Height > 0 ? request.Height : 150;
                    if (request is TripRequest)
                    {
                        //Tools.DebugLog($"StaticMapService:Work() request:{request.Type} {((TripRequest)request).StartPosID}->{((TripRequest)request).EndPosID}");
                        string filename = System.IO.Path.Combine(GetMapDir(), GetMapFileName(((TripRequest)request).CarID, ((TripRequest)request).StartPosID, ((TripRequest)request).EndPosID));
                        if (MapFileExistsOrIsTooOld(filename))
                        {
                            using (DataTable dt = TripToCoords((TripRequest)request))
                            {
                                if (dt != null & dt.Rows.Count > 1)
                                {
                                    _StaticMapProvider.CreateTripMap(dt, width, height, request.Mode, request.Special, filename);
                                    dt.Clear();
                                    dt.Dispose();
                                    if (_StaticMapProvider != null)
                                    {
                                        Thread.Sleep(_StaticMapProvider.GetDelayMS());
                                    }
                                    else
                                    {
                                        Thread.Sleep(1000);
                                    }
                                }
                            }
                        }
                    }
                    else if (request is POIRequest)
                    {
                        //Tools.DebugLog($"StaticMapService:Work() request:{request.Type} {((POIRequest)request).Lat},{((POIRequest)request).Lng}");
                        string filename = System.IO.Path.Combine(GetMapDir(), GetMapFileName(request.Type, ((POIRequest)request).Lat, ((POIRequest)request).Lng));
                        if (MapFileExistsOrIsTooOld(filename))
                        {
                            switch (request.Type)
                            {
                                case MapType.Charge:
                                    _StaticMapProvider.CreateChargingMap(((POIRequest)request).Lat, ((POIRequest)request).Lng, width, height, request.Mode, request.Special, filename);
                                    break;
                                case MapType.Park:
                                    _StaticMapProvider.CreateParkingMap(((POIRequest)request).Lat, ((POIRequest)request).Lng, width, height, request.Mode, request.Special, filename);
                                    break;
                                case MapType.Trip:
                                    break;
                                default:
                                    break;
                            }
                            if (_StaticMapProvider != null)
                            {
                                Thread.Sleep(_StaticMapProvider.GetDelayMS());
                            }
                            else
                            {
                                Thread.Sleep(1000);
                            }
                        }
                    }
                }
            }
        }

        private static DataTable TripToCoords(TripRequest request)
        {
            DataTable dt = new DataTable();
            int CarID = int.MinValue;
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  CarID
FROM
  pos
WHERE
  id = @startID ", con)
                {
                    CommandTimeout = 6000
                })
                {
                    cmd.Parameters.AddWithValue("@startID", request.StartPosID);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        _ = int.TryParse(dr[0].ToString(), out CarID);
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
                    da.SelectCommand.CommandTimeout = 6000;
                    //Tools.DebugLog(da.SelectCommand);
                    SQLTracer.TraceDA(dt, da);
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
SELECT DISTINCT
  round(lat, 4) as lat,
  round(lng, 4) as lng
FROM
  chargingstate
JOIN pos ON
  chargingstate.pos = pos.id
", DBHelper.DBConnectionstring))
                {
                    da.Fill(dt);
                }
                foreach (DataRow dr in dt.Rows)
                {
                    GetSingleton().Enqueue(MapType.Charge, (double)dr["lat"], (double)dr["lng"], MapMode.Dark);
                }
                dt.Clear();
            }
        }

        public static void CreateAllParkingMaps()
        {
            using (DataTable dt = new DataTable())
            {
                using (MySqlDataAdapter da = new MySqlDataAdapter($@"
SELECT DISTINCT
    ROUND(lat, 4) AS lat,
    ROUND(lng, 4) AS lng
FROM
    pos
WHERE
    pos.id IN(
SELECT
    pos
FROM
    chargingstate
)
UNION ALL
SELECT DISTINCT
    ROUND(lat, 4) AS lat,
    ROUND(lng, 4) AS lng
FROM
    pos
WHERE
    pos.id IN(
    SELECT
        StartPos
    FROM
        drivestate
)
UNION ALL
SELECT DISTINCT
    ROUND(lat, 4) AS lat,
    ROUND(lng, 4) AS lng
FROM
    pos
WHERE
    pos.id IN(
SELECT
    EndPos
FROM
    drivestate
)", DBHelper.DBConnectionstring))
                {
                    da.SelectCommand.CommandTimeout = 600;
                    SQLTracer.TraceDA(dt, da);
                }
                foreach (DataRow dr in dt.Rows)
                {
                    GetSingleton().Enqueue(MapType.Park, (double)dr["lat"], (double)dr["lng"], MapMode.Dark);
                }
                dt.Clear();
            }
        }

        public static void CreateAllTripMaps(MapMode mapMode = MapMode.Dark)
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
                            GetSingleton().Enqueue(Convert.ToInt32(dr["carid"], Tools.ciEnUS), Convert.ToInt32(dr["startposid"], Tools.ciEnUS), Convert.ToInt32(dr["endposid"], Tools.ciEnUS), 0, 0, mapMode, MapSpecial.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.ToExceptionless().FirstCarUserID().Submit();
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                        Logfile.Log(ex.ToString());
                    }
                }
            }
        }

        public static string GetMapDir()
        {
            string mapdir = "/var/lib/grafana/plugins/teslalogger-timeline-panel/dist/maps";

            if (!Tools.IsMono())
                mapdir = new DirectoryInfo("maps").FullName;

            try
            {
                if (!System.IO.Directory.Exists(mapdir))
                {
                    System.IO.Directory.CreateDirectory(mapdir);
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
            return mapdir;
        }

        private static string GetMapFileName(MapType type, double lat, double lng)
        {
            switch (type)
            {
                case MapType.Charge:
                    return "C-" + lat + "-" + lng + ".png";
                case MapType.Park:
                    return "P-" + lat + "-" + lng + ".png";
                case MapType.Trip:
                    break;
                default:
                    break;
            }
            return "error.jpg";
        }

        public static string GetMapFileName(int CarID, int startpos, int endpos)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("T");
            sb.Append(CarID);
            sb.Append("-");
            sb.Append(startpos);
            sb.Append("-");
            sb.Append(endpos);
            sb.Append(".png");
            return sb.ToString();
        }

        internal static void CreateChargingMapOnChargingCompleted(int CarID)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand($@"
SELECT
  round(lat, 4) as lat,
  round(lng, 4) as lng
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
                                GetSingleton().Enqueue(MapType.Charge, Convert.ToDouble(dr["lat"], Tools.ciEnUS), Convert.ToDouble(dr["lng"], Tools.ciEnUS), MapMode.Dark);
                            }
                        }
                        catch (Exception ex)
                        {
                            ex.ToExceptionless().FirstCarUserID().Submit();
                            Logfile.Log(ex.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
        }

        internal static void CreateParkingMapFromPosid(int posID)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();

                    using (MySqlCommand cmd = new MySqlCommand($@"
SELECT
  round(lat, 4) as lat,
  round(lng, 4) as lng
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
                                GetSingleton().Enqueue(MapType.Park, Convert.ToDouble(dr["lat"], Tools.ciEnUS), Convert.ToDouble(dr["lng"], Tools.ciEnUS), MapMode.Dark);
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
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
        }
    }
}