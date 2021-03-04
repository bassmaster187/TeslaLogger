using System;
using System.Collections.Concurrent;
using System.Data;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    public class StaticMapService
    {
        private static StaticMapService _StaticMapService = null;
        private static StaticMapProvider _StaticMapProvider = null;

        private readonly ConcurrentQueue<Tuple<int, int, int, int, StaticMapProvider.MapType, StaticMapProvider.MapMode, StaticMapProvider.MapSpecial>> queue = new ConcurrentQueue<Tuple<int, int, int, int, StaticMapProvider.MapType, StaticMapProvider.MapMode, StaticMapProvider.MapSpecial>>();

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

        public void Enqueue(int startPosID, int endPosID, int width, int height, StaticMapProvider.MapType type, StaticMapProvider.MapMode mode, StaticMapProvider.MapSpecial special)
        {
            Enqueue(new Tuple<int, int, int, int, StaticMapProvider.MapType, StaticMapProvider.MapMode, StaticMapProvider.MapSpecial>(startPosID, endPosID, width, height, type, mode, special));
        }

        private void Enqueue(Tuple<int, int, int, int, StaticMapProvider.MapType, StaticMapProvider.MapMode, StaticMapProvider.MapSpecial> request)
        {
            queue.Enqueue(request);
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
                if (queue.TryDequeue(out Tuple<int, int, int, int, StaticMapProvider.MapType, StaticMapProvider.MapMode, StaticMapProvider.MapSpecial> request))
                {
                    Tools.DebugLog($"StaticMapService:Work() request:{request.Item5} {request.Item1}->{request.Item2}");
                    int width = request.Item3 > 0 ? request.Item3 : 240;
                    int height = request.Item4 > 0 ? request.Item4 : (int)(width / 1.618033);
                    using (DataTable dt = TripToCoords(request, out int CarID))
                    {
                        string filename = System.IO.Path.Combine(GetMapDir(), GetMapFileName(request.Item5, CarID, request.Item1, request.Item2));
                        switch (request.Item5)
                        {
                            case StaticMapProvider.MapType.Trip:
                                _StaticMapProvider.CreateTripMap(dt, width, height, request.Item6 == StaticMapProvider.MapMode.Dark ? StaticMapProvider.MapMode.Dark : StaticMapProvider.MapMode.Regular, StaticMapProvider.MapSpecial.None, filename);
                                break;
                            case StaticMapProvider.MapType.Park:
                                _StaticMapProvider.CreateParkingMap(dt.Rows[0], width, height, request.Item6 == StaticMapProvider.MapMode.Dark ? StaticMapProvider.MapMode.Dark : StaticMapProvider.MapMode.Regular, StaticMapProvider.MapSpecial.None, filename);
                                break;
                            case StaticMapProvider.MapType.Charge:
                                _StaticMapProvider.CreateChargingMap(dt.Rows[0], width, height, request.Item6 == StaticMapProvider.MapMode.Dark ? StaticMapProvider.MapMode.Dark : StaticMapProvider.MapMode.Regular, StaticMapProvider.MapSpecial.None, filename);
                                break;
                        }
                    }
                }
            }
        }

        private DataTable TripToCoords(Tuple<int, int, int, int, StaticMapProvider.MapType, StaticMapProvider.MapMode, StaticMapProvider.MapSpecial> request, out int CarID)
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
                    cmd.Parameters.AddWithValue("@startID", request.Item1);
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
                    da.SelectCommand.Parameters.AddWithValue("@startID", request.Item1);
                    da.SelectCommand.Parameters.AddWithValue("@endID", request.Item2);
                    Tools.DebugLog(da.SelectCommand);
                    da.Fill(dt);
                }
            }
            else
            {
                Tools.DebugLog($"TripToCoords: could not find CarID for pos {request.Item1}");
            }
            return dt;
        }

        public static void CreateAllChargigMaps()
        {
        }

        public static void CreateAllParkingMaps()
        {
        }

        public static void CreateAllTripMaps()
        {
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

        public static string GetMapFileName(StaticMapProvider.MapType type, int carID, int startpos, int endpos)
        {
            StringBuilder sb = new StringBuilder();
            switch (type)
            {
                case StaticMapProvider.MapType.Trip:
                    sb.Append("T-");
                    break;
                case StaticMapProvider.MapType.Charge:
                    sb.Append("C-");
                    break;
                case StaticMapProvider.MapType.Park:
                    sb.Append("P-");
                    break;
            }
            sb.Append(carID);
            sb.Append("-");
            sb.Append(startpos);
            sb.Append("-");
            sb.Append(endpos);
            sb.Append(".png");
            return sb.ToString();
        }

    }
}
