using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless;
using Microsoft.VisualBasic.Logging;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;

// inspired by https://github.com/timschneeb/KomootGPX

namespace TeslaLogger
{
    public class Komoot
    {
        private class KomootLoginInfo
        {
            internal int carID;
            internal string username;
            internal string password;
            internal string user_id;
            internal string token;
            internal bool loginSuccessful = false;

            internal KomootLoginInfo(int carID, string username, string password, string user_id, string token)
            {
                this.carID = carID;
                this.username = username;
                this.password = password;
                this.user_id = user_id;
                this.token = token;
            }
        }

        private class KomootTour
        {
            internal long tourID;
            readonly internal DateTime start;
            readonly string type;
            readonly string sport;
            internal Position firstPosition;
            internal Position lastPosition;

            private Dictionary<int, Position> positions = new Dictionary<int, Position>();

            public KomootTour(long tourid, string type, string sport, DateTime start)
            {
                tourID = tourid;
                this.type = type;
                this.sport = sport;
                this.start = start;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"TourID: {tourID}");
                sb.AppendLine($"Type: {type}");
                sb.AppendLine($"Sport: {sport}");
                sb.AppendLine($"Start: {start}");
                sb.AppendLine($"Positions: {positions.Count}");
                foreach (int key in positions.Keys)
                {
                    sb.AppendLine($" {key} - ({positions[key].lat},{positions[key].lng} t:{positions[key].delta_t} s:{positions[key].speed}");
                }
                return sb.ToString();
            }

            internal class Position
            {
                internal double lat;
                internal double lng;
                internal long delta_t;
                internal double speed;

                public Position(double lat, double lng, int delta_t, double speed)
                {
                    this.lat = lat;
                    this.lng = lng;
                    this.delta_t = delta_t;
                    this.speed = speed;
                }

                internal double calculateSpeed(Position other)
                {
                    // calculate distance and speed with previous pos
                    // inspired by https://github.com/mapado/haversine/blob/main/haversine/haversine.py
                    Position pos1 = this;
                    Position pos2 = other;
                    double AVG_EARTH_RADIUS_KM = 6371.0088;
                    double lat1 = pos1.lat * Math.PI / 180;
                    double lng1 = pos1.lng * Math.PI / 180;
                    double lat2 = pos2.lat * Math.PI / 180;
                    double lng2 = pos2.lng * Math.PI / 180;
                    double d = Math.Pow(Math.Sin((lat2 - lat1) * 0.5), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin((lng2 - lng1) * 0.5), 2);
                    double dist_km = AVG_EARTH_RADIUS_KM * 2 * Math.Asin(Math.Sqrt(d));
                    double speed = dist_km / Math.Abs(pos1.delta_t - pos2.delta_t) * 3600000; // km/ms -> km/h
                    return speed;
                }
            }

            internal void addPosition(double lat, double lng, int delta_t, double speed = double.NaN)
            {
                if (positions.Count == 0)
                {
                    // insert first pos
                    firstPosition = new Position(lat, lng, delta_t, double.IsNaN(speed) ? 0 : speed);
                    lastPosition = firstPosition;
                }
                else if (!positions.ContainsKey(delta_t))
                {
                    Position newPosition = new Position(lat, lng, delta_t, speed);
                    if (double.IsNaN(speed))
                    {
                        newPosition.speed = newPosition.calculateSpeed(lastPosition);
                    }
                    positions.Add(delta_t, newPosition);
                    lastPosition = newPosition;
                }
            }

        }

        private readonly int interval = 6 * 60 * 60; // 6 hours in seconds
        private readonly int carID = -1;
        private readonly string username = string.Empty;
        private readonly string password = string.Empty;

        private static readonly Dictionary<string, string> EndPoints = new Dictionary<string, string>()
        {
            { "KomootListSettings", "/komoot/listSettings" },
            { "KomootSaveSettings", "/komoot/saveSettings" },
            { "KomootReimport" , "/komoot/reimport" }
        };

        public Komoot(int CarID, string Username, string Password)
        {
            this.carID = CarID;
            this.username = Username;
            this.password = Password;
        }

        internal static void CheckSchema()
        {
            try
            {
                if (DBHelper.ColumnExists("drivestate", "komoot_tour_id"))
                {
                    Logfile.Log("ALTER TABLE drivestate ADD komoot_tour_id BIGINT NULL");
                    UpdateTeslalogger.AssertAlterDB();
                    DBHelper.ExecuteSQLQuery(@"ALTER TABLE drivestate ADD komoot_tour_id BIGINT NULL", 600);
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("Komoot.CheckSchema: Exception", ex);
            }
        }

        private static void CheckDriveState(KomootLoginInfo kli, Dictionary<int, KomootTour> tours)
        {
            Tools.DebugLog($"CheckDriveState");
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    id,
    StartDate
FROM
    drivestate
WHERE
    CarID = @CarID
    AND wheel_type IS NULL", con))
                {
                    cmd.Parameters.AddWithValue("@CarID", kli.carID);
                    MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                    if (dr.Read() && dr[0] != DBNull.Value)
                    {
                        if (int.TryParse(dr[0].ToString(), out int id) && DateTime.TryParse(dr[1].ToString(), out DateTime _))
                        {
                            // try to find tour by startdate
                            DateTime start = DateTime.Parse(dr[1].ToString());
                            foreach (int tourID in tours.Keys)
                            {
                                if (tours[tourID].start.Equals(start))
                                {
                                    Tools.DebugLog($"CheckDriveState: found tour for start {start} -> {tourID}");
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            Tools.DebugLog($"CheckDriveState done");
        }

        // main loop
        public void Run()
        {
            bool firstrun = true;
            try
            {
                while (true)
                {
                    Work(firstrun);
                    firstrun = false;
                    Thread.Sleep(interval * 1000);
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("Komoot: Exception", ex);
            }
        }

        private void Work(bool firstrun)
        {
            KomootLoginInfo kli = new KomootLoginInfo(carID, username, password, string.Empty, string.Empty);
            Login(kli);
            if (!kli.loginSuccessful)
            {
                Logfile.Log($"Komoot_{carID}: Login failed!");
            }
            else
            {
                Dictionary<int, KomootTour> tours = DownloadTours(kli);
                if (tours.Count > 0)
                {
                    ParseTours(kli, tours);
                    if (firstrun)
                    {
                        CheckDriveState(kli, tours);
                    }
                }
                else
                {
                    Logfile.Log($"Komoot_{carID}: no tours found!");
                }
            }
            Logfile.Log($"Komoot_{carID}: done");
        }

        private static string DownloadTour(KomootLoginInfo kli, int tourID)
        {
            Logfile.Log($"Komoot_{kli.carID}: DownloadTour{tourID} ...");
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{kli.user_id}:{kli.token}")));
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://api.komoot.de/v007/tours/{tourID}?_embedded=coordinates,way_types,surfaces,directions,participants,timeline&directions=v2&fields=timeline&format=coordinate_array&timeline_highlights_fields=tips,recommenders")))
                {
                    HttpResponseMessage result = httpClient.SendAsync(request).Result;
                    if (result.IsSuccessStatusCode)
                    {
                        string resultContent = result.Content.ReadAsStringAsync().Result;
                        Logfile.Log($"Komoot_{kli.carID}: DownloadTour{tourID} done");
                        return resultContent;
                    }
                    Logfile.Log($"Komoot_{kli.carID}: DownloadTour{tourID} error: {result.StatusCode}");
                }
            }
            return string.Empty;
        }

        private static void ParseTours(KomootLoginInfo kli, Dictionary<long, KomootTour> tours, bool dumpJSON = false, bool checkExistingTours = false)
        {
            string KVSkey = $"Komoot_{kli.carID}_Max_Tour_ID";
            if (KVS.Get(KVSkey, out int maxTourIDint32) == KVS.SUCCESS)
            {
                // migrate int to long
                KVS.Remove(KVSkey);
                if (KVS.InsertOrUpdate(KVSkey, (long)maxTourIDint32) == KVS.SUCCESS)
                {
                    Logfile.Log($"Komoot_{kli.carID}: migrated KVS {KVSkey} from int32 to int64");
                }
                else
                {
                    Logfile.Log($"Komoot_{kli.carID}: error migrating KVS {KVSkey} from int32 to int64");
                }
            }
            if (KVS.Get(KVSkey, out long maxTourID) == KVS.FAILED)
            {
                maxTourID = 0;
            }
            if (checkExistingTours)
            {
                maxTourID = 0;
            }
            Logfile.Log($"Komoot_{kli.carID}: parsing tours newer than tourID {maxTourID} ...");
            foreach (int tourid in tours.Keys)
            {
                if (tourid > maxTourID)
                {
                    Logfile.Log($"Komoot_{kli.carID}: {tours[tourid]}");
                    string resultContent = DownloadTour(kli, tourid);
                    Tools.DebugLog($"Komoot_{kli.carID} GetTour({tourid}) result: {resultContent.Length}");
                    if (dumpJSON)
                    {
                        Logfile.Log($"Komoot_{kli.carID}: GetTour({tourid}) JSON:" + Environment.NewLine + resultContent);
                    }
                    /* expected JSON
{
"id": 987654321,
"type": "tour_recorded",
"name": "Tour",
"source": {
"api": "de.komoot.main-api/tour/recorded",
"type": "tour_recorded",
"id": 987654321
},
"status": "private",
"date": "2025-02-12T17:00:10.291Z",
"kcal_active": 0,
"kcal_resting": 0,
"start_point": {
"lat": 54.4484,
"lng": 33.340371,
"alt": 42.1
},
"distance": 2272.7719709160106,
"duration": 945,
"elevation_up": 8.89315328029921,
"elevation_down": 4.006897096236379,
"sport": "touringbicycle",
"time_in_motion": 441,
"changed_at": "2025-02-12T17:18:10.047Z",
"map_image": {
"src": "https://tourpic-vector.maps.komoot.net/r/big/gk__@wxcdPh@CB/?width={width}&height={height}&crop={crop}",
"templated": true,
"type": "image/*",
"attribution": "Map data © OpenStreetMap contributors"
},
"map_image_preview": {
"src": "https://tourpic-vector.maps.komoot.net/r/small/gk__@wxcf@iIJ%7B@Dfoo@CB/?width={width}&height={height}&crop={crop}",
"templated": true,
"type": "image/*",
"attribution": "Map data © OpenStreetMap contributors"
},
"vector_map_image": {
"src": "https://tourpic-vector.maps.komoot.net/r/big/gk__@wpcGxcfSVWPh@CB/",
"templated": false,
"type": "image/*",
"attribution": "Map data © OpenStreetMap contributors"
},
"vector_map_image_preview": {
"src": "https://tourpic-vector.maps.komoot.net/r/small/gk__@wpcGcfrPh@CB/",
"templated": false,
"type": "image/*",
"attribution": "Map data © OpenStreetMap contributors"
},
"potential_route_update": false,
"_embedded": {
"coordinates": {
"items": [
    {
        "lat": 42.4484,
        "lng": 53.340371,
        "alt": 42.3,
        "t": 0
    },
    {
        "lat": 62.448262,
        "lng": 73.340385,
        "alt": 42.3,
        "t": 2000
    },
    {
        "lat": 82.448126,
        "lng": 33.340499,
        "alt": 42.3,
        "t": 4709
    }
],
"_links": {
    "self": {
        "href": "https://api.komoot.de/v007/tours/987654321/coordinates"
    }
}
},
"creator": {
"username": "1234567890",
"avatar": {
    "src": "https://dfr.cloudfront.net/www/000/defaultuserimage-full/0?width={width}&height={height}&crop={crop}",
    "templated": true,
    "type": "image/*"
},
"status": "public",
"_links": {
    "self": {
        "href": "https://api.komoot.de/v007/users/1234567890/profile_embedded"
    },
    "relation": {
        "href": "https://api.komoot.de/v007/users/{username}/relations/1234567890",
        "templated": true
    }
},
"display_name": "DisplayNameCFGT",
"is_premium": false
},
"participants": [],
"timeline": {
"_links": {
    "self": {
        "href": "https://api.komoot.de/v007/tours/987654321/timeline/"
    }
},
"page": {
    "size": 0,
    "totalElements": 0,
    "totalPages": 1,
    "number": 0
}
}
},
"_links": {
"creator": {
"href": "https://api.komoot.de/v007/users/1234567890/profile_embedded"
},
"self": {
"href": "https://api.komoot.de/v007/tours/987654321?_embedded=way_types%252Cdirections%252Csurfaces%252Ccoordinates%252Ctimeline%252Cparticipants"
},
"coordinates": {
"href": "https://api.komoot.de/v007/tours/987654321/coordinates"
},
"tour_line": {
"href": "https://api.komoot.de/v007/tours/987654321/tour_line"
},
"participants": {
"href": "https://api.komoot.de/v007/tours/987654321/participants/"
},
"timeline": {
"href": "https://api.komoot.de/v007/tours/987654321/timeline/"
},
"translations": {
"href": "https://api.komoot.de/v007/tours/987654321/translations"
},
"cover_images": {
"href": "https://api.komoot.de/v007/tours/987654321/cover_images/"
},
"tour_rating": {
"href": "https://api.komoot.de/v007/tours/987654321/ratings/1234567890"
}
}
}
                     */
                    dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);
                    if (jsonResult.ContainsKey("date") && jsonResult.ContainsKey("_embedded") && jsonResult["_embedded"].ContainsKey("coordinates") && jsonResult["_embedded"]["coordinates"].ContainsKey("items"))
                    {
                        DateTime start = DateTime.Parse(jsonResult["date"].ToString(), null, DateTimeStyles.AdjustToUniversal);
                        DateTime end = start;
                        bool firstPos = true;
                        double lat = double.NaN;
                        double lng = double.NaN;
                        double prev_lat = double.NaN;
                        double prev_lng = double.NaN;
                        int firstPosID = 0;
                        int lastPosID = 0;
                        long t = 0;
                        long prev_t = 0;
                        double odo = GetMaxOdo(kli.carID);
                        Logfile.Log($"Komoot_{kli.carID}: inserting {jsonResult["_embedded"]["coordinates"]["items"].Count} positions ...");
                        // TODO filter out duplicate positions
                        foreach (dynamic pos in jsonResult["_embedded"]["coordinates"]["items"])
                        {
                            if (pos.ContainsKey("lat") && pos.ContainsKey("lng") && pos.ContainsKey("alt") && pos.ContainsKey("t"))
                            {
                                double alt = 0.0;
                                double speed = 0.0;
                                if (!firstPos)
                                {
                                    prev_lat = lat;
                                    prev_lng = lng;
                                    prev_t = t;
                                }
                                if (Double.TryParse(pos["lat"].ToString(), out lat) && Double.TryParse(pos["lng"].ToString(), out lng) && Double.TryParse(pos["alt"].ToString(), out alt) && long.TryParse(pos["t"].ToString(), out t))
                                {
                                    end = start.AddMilliseconds(t);
                                    if (firstPos)
                                    {
                                        firstPos = false;
                                        // if checkExistingTours is false, then this is a new tour and can be added
                                        if (!checkExistingTours)
                                        {
                                            firstPosID = InsertPos(kli.carID, lat, lng, start, speed, alt, odo);
                                        }
                                        else
                                        {
                                            // TODO
                                        }
                                    }
                                    else
                                    {
                                        // calculate distance and speed with previous pos
                                        // inspired by https://github.com/mapado/haversine/blob/main/haversine/haversine.py
                                        double AVG_EARTH_RADIUS_KM = 6371.0088;
                                        double lat1 = lat * Math.PI / 180;
                                        double lng1 = lng * Math.PI / 180;
                                        double lat2 = prev_lat * Math.PI / 180;
                                        double lng2 = prev_lng * Math.PI / 180;
                                        double d = Math.Pow(Math.Sin((lat2 - lat1) * 0.5), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin((lng2 - lng1) * 0.5), 2);
                                        double dist_km = AVG_EARTH_RADIUS_KM * 2 * Math.Asin(Math.Sqrt(d));
                                        speed = dist_km / (t - prev_t) * 3600000; // km/ms -> km/h
                                        odo = odo + dist_km;
                                        //Tools.DebugLog($"<{tourid}> {lat} {lng} {alt} {start.AddMilliseconds(t)} dist:{dist_km} speed:{speed}");
                                        // if checkExistingTours is false, then this is a new tour and can be added
                                        if (!checkExistingTours)
                                        {
                                            lastPosID = InsertPos(kli.carID, lat, lng, end, speed, alt, odo);
                                        }
                                        else
                                        {
                                            // TODO
                                        }
                                    }
                                }
                            }
                            else
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.AppendLine();
                                sb.Append("lat:");
                                sb.AppendLine(jsonResult.ContainsKey("lat"));
                                sb.Append("lng:");
                                sb.AppendLine(jsonResult.ContainsKey("lng"));
                                sb.Append("alt:");
                                sb.AppendLine(jsonResult.ContainsKey("alt"));
                                sb.Append("t:");
                                sb.AppendLine(jsonResult.ContainsKey("t"));
                                Logfile.Log($"Komoot_{kli.carID}: parsing tours error - missing JSON contents" + sb.ToString());
                            }
                        }
                        if (firstPosID > 0 && lastPosID > 0)
                        {
                            // if checkExistingTours is false, then this is a new tour and can be added
                            if (!checkExistingTours)
                            {
                                CreateDriveState(kli.carID, start, firstPosID, end, lastPosID);
                            }
                        }
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine();
                        sb.Append("date:");
                        sb.AppendLine(jsonResult.ContainsKey("date"));
                        sb.Append("_embedded:");
                        sb.AppendLine(jsonResult.ContainsKey("_embedded"));
                        if (jsonResult.ContainsKey("_embedded"))
                        {
                            sb.Append("_embedded.coordinates:");
                            sb.AppendLine(jsonResult["_embedded"].ContainsKey("coordinates"));
                            if (jsonResult["_embedded"].ContainsKey("coordinates"))
                            {
                                sb.Append("_embedded.coordinates.items:");
                                sb.AppendLine(jsonResult["_embedded"]["coordinates"].ContainsKey("items"));
                            }
                        }
                        sb.AppendLine();
                        Logfile.Log($"Komoot_{kli.carID}: parsing tours error - missing JSON contents" + sb.ToString() + resultContent);
                    }

                    KVS.InsertOrUpdate(KVSkey, tourid);
                }
            }
        }

        private static double GetMaxOdo(int carid)
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    MAX(odometer)
FROM
    pos
WHERE
    CarID = @CarID", con))
                {
                    cmd.Parameters.AddWithValue("@CarID", carid);
                    MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                    if (dr.Read() && dr[0] != DBNull.Value)
                    {
                        if (double.TryParse(dr[0].ToString(), out double pos))
                        {
                            return pos;
                        }
                    }
                }
            }
            return 0.0;
        }

        // TODO wheel_type = tourID
        private static void CreateDriveState(int carID, DateTime start, int firstPosID, DateTime end, int lastPosID)
        {
            Logfile.Log($"Komoot_{carID}: CreateDriveState {firstPosID}->{lastPosID} {start} {end} ...");
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"
INSERT
    drivestate(
        CarID,
        StartDate,
        StartPos,
        EndDate,
        EndPos
)
VALUES(
    @CarID,
    @StartDate,
    @StartPos,
    @EndDate,
    @EndPos
)"
                , con))
                {
                    cmd.Parameters.AddWithValue("@CarID", carID);
                    cmd.Parameters.AddWithValue("@StartDate", start);
                    cmd.Parameters.AddWithValue("@StartPos", firstPosID);
                    cmd.Parameters.AddWithValue("@EndDate", end);
                    cmd.Parameters.AddWithValue("@EndPos", lastPosID);
                    _ = SQLTracer.TraceNQ(cmd, out long _);
                }
            }
            // update start address and end address
            _ = WebHelper.UpdateAllPOIAddresses(0, $"{firstPosID},{lastPosID}");
            // create timeline maps
            _ = Task.Factory.StartNew(() =>
            {
                StaticMapService.GetSingleton().Enqueue(carID, firstPosID, lastPosID, 0, 0, StaticMapProvider.MapMode.Dark, StaticMapProvider.MapSpecial.None);
                StaticMapService.CreateParkingMapFromPosid(firstPosID);
                StaticMapService.CreateParkingMapFromPosid(lastPosID);
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        }

        private static int InsertPos(int carID, double lat, double lng, DateTime timestamp, double speed, double alt, double odometer)
        {
            int posid = 0;
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"
INSERT
    pos(
        CarID,
        Datum,
        lat,
        lng,
        speed,
		altitude,
		odometer
)
VALUES(
    @CarID,
    @Datum,
    @lat,
    @lng,
    @speed,
    @altitude,
    @odometer
)"
                , con))
                {
                    cmd.Parameters.AddWithValue("@CarID", carID);
                    cmd.Parameters.AddWithValue("@Datum", timestamp);
                    cmd.Parameters.AddWithValue("@lat", lat);
                    cmd.Parameters.AddWithValue("@lng", lng);
                    cmd.Parameters.AddWithValue("@speed", speed);
                    cmd.Parameters.AddWithValue("@altitude", alt);
                    cmd.Parameters.AddWithValue("@odometer", odometer);
                    _ = SQLTracer.TraceNQ(cmd, out long _);
                    using (MySqlCommand cmdid = new MySqlCommand("SELECT LAST_INSERT_ID()", con))
                    {
                        posid = Convert.ToInt32(cmdid.ExecuteScalar());
                    }
                }
            }
            return posid;
        }

        private static Dictionary<long, KomootTour> DownloadTours(KomootLoginInfo kli, bool dumpJSON = false)
        {
            Logfile.Log($"Komoot_{kli.carID}: getting tours ...");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            Dictionary<long, KomootTour> komootTours = new Dictionary<long, KomootTour>();
            bool nextPage = true;
            string url = $"https://api.komoot.de/v007/users/{kli.user_id}/tours/";
            while (nextPage)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{kli.user_id}:{kli.token}")));
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(url)))
                    {
                        HttpResponseMessage result = httpClient.SendAsync(request).Result;
                        if (result.IsSuccessStatusCode)
                        {
                            string resultContent = result.Content.ReadAsStringAsync().Result;
                            Tools.DebugLog($"Komoot_{kli.carID} GetTours result: {resultContent.Length}");
                            if (dumpJSON)
                            {
                                Logfile.Log($"Komoot_{kli.carID}: tours JSON:" + Environment.NewLine + resultContent);
                            }
                            /* expected JSON
		{
    "_embedded": {
        "tours": [
            {
                "id": 987654321,
                "type": "tour_recorded",
                "name": "Tour",
                "source": "{\"api\":\"de.komoot.main-api/tour/recorded\",\"type\":\"tour_recorded\",\"id\":2052310542}",
                "status": "private",
                "date": "2025-02-12T17:00:10.291Z",
                "kcal_active": 0,
                "kcal_resting": 0,
                "start_point": {
                    "lat": 53.4484,
                    "lng": 23.340371,
                    "alt": 42.1
                },
                "distance": 2272.7719709160106,
                "duration": 945,
                "elevation_up": 8.89315328029921,
                "elevation_down": 4.006897096236379,
                "sport": "touringbicycle",
                "time_in_motion": 441,
                "changed_at": "2025-02-12T17:18:10.047Z",
                "map_image": {
                    "src": "https://tourpic-vector.maps.komoot.net/r/big/gk__@wpcGxxxVWxxCB/?width={width}&height={height}&crop={crop}",
                    "templated": true,
                    "type": "image/*",
                    "attribution": "Map data © OpenStreetMap contributors"
                },
                "map_image_preview": {
                    "src": "https://tourpic-vector.maps.komoot.net/r/small/gk__@wpcGDAITxxx@iIJ%7xxxh@CB/?width={width}&height={height}&crop={crop}",
                    "templated": true,
                    "type": "image/*",
                    "attribution": "Map data © OpenStreetMap contributors"
                },
                "vector_map_image": {
                    "src": "https://tourpic-vector.maps.komoot.net/r/big/gk__@wpcGxxx@iIJ%7B@xxx@CB/",
                    "templated": false,
                    "type": "image/*",
                    "attribution": "Map data © OpenStreetMap contributors"
                },
                "vector_map_image_preview": {
                    "src": "https://tourpic-vector.maps.komoot.net/r/small/gk__@wpcGxxxVWPh@CB/",
                    "templated": false,
                    "type": "image/*",
                    "attribution": "Map data © OpenStreetMap contributors"
                },
                "potential_route_update": false,
                "_embedded": {
                    "creator": {
                        "username": "1234567890",
                        "avatar": {
                            "src": "https://xcd.cloudfront.net/www/000/defaultuserimage-full/0?width={width}&height={height}&crop={crop}",
                            "templated": true,
                            "type": "image/*"
                        },
                        "status": "public",
                        "_links": {
                            "self": {
                                "href": "https://api.komoot.de/v007/users/1234567890/profile_embedded"
                            },
                            "relation": {
                                "href": "https://api.komoot.de/v007/users/{username}/relations/1234567890",
                                "templated": true
                            }
                        },
                        "display_name": "DisplayNamexyz",
                        "is_premium": false
                    }
                },
                "_links": {
                    "creator": {
                        "href": "https://api.komoot.de/v007/users/1234567890/profile_embedded"
                    },
                    "self": {
                        "href": "https://api.komoot.de/v007/tours/987654321"
                    },
                    "coordinates": {
                        "href": "https://api.komoot.de/v007/tours/987654321/coordinates"
                    },
                    "tour_line": {
                        "href": "https://api.komoot.de/v007/tours/987654321/tour_line"
                    },
                    "participants": {
                        "href": "https://api.komoot.de/v007/tours/987654321/participants/"
                    },
                    "timeline": {
                        "href": "https://api.komoot.de/v007/tours/987654321/timeline/"
                    },
                    "translations": {
                        "href": "https://api.komoot.de/v007/tours/987654321/translations"
                    },
                    "cover_images": {
                        "href": "https://api.komoot.de/v007/tours/987654321/cover_images/"
                    },
                    "tour_rating": {
                        "href": "https://api.komoot.de/v007/tours/987654321/ratings/1234567890"
                    }
                }
            }
        ]
    },
    "_links": {
        "self": {
            "href": "https://api.komoot.de/v007/users/1234567890/tours/"
        },
        "next": {
            "href": "https://api.komoot.de/v007/users/1234567890/tours/?page=1&limit=100"
        }
    },
    "page": {
        "size": 100,
        "totalElements": 101,
        "totalPages": 2,
        "number": 0
    }
}
							 */
                            dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);
                            if (jsonResult.ContainsKey("_links") && jsonResult["_links"].ContainsKey("next") && jsonResult["_links"]["next"].ContainsKey("href"))
                            {
                                url = jsonResult["_links"]["next"]["href"];
                            }
                            else
                            {
                                nextPage = false;
                            }
                            if (jsonResult.ContainsKey("_embedded") && jsonResult["_embedded"].ContainsKey("tours"))
                            {
                                dynamic jtours = jsonResult["_embedded"]["tours"];
                                Logfile.Log($"Komoot_{kli.carID}: found {jsonResult["_embedded"]["tours"].Count} tours ...");
                                foreach (dynamic tour in jtours)
                                {
                                    // build tour info
                                    if (tour.ContainsKey("id"))
                                    {
                                        sb.Append($"Tour id:{tour["id"]}");
                                    }
                                    if (tour.ContainsKey("type"))
                                    {
                                        sb.Append($" type:{tour["type"]}");
                                    }
                                    if (tour.ContainsKey("sport"))
                                    {
                                        sb.Append($" sport:{tour["sport"]}");
                                    }
                                    if (tour.ContainsKey("date"))
                                    {
                                        sb.Append($" date:{tour["date"]}");
                                    }
                                    sb.AppendLine();
                                    if (tour.ContainsKey("id") && tour.ContainsKey("sport") && tour.ContainsKey("date") && tour.ContainsKey("type") && tour["type"].ToString().Equals("tour_recorded"))
                                    {
                                        if (Int64.TryParse(tour["id"].ToString(), out long tourid) && DateTime.TryParse(tour["date"].ToString(), out DateTime _))
                                        {
                                            komootTours.Add(tourid, new KomootTour(tourid, tour["type"].ToString(), tour["sport"].ToString(), DateTime.Parse(tour["date"].ToString())));
                                        }
                                        else
                                        {
                                            Logfile.Log($"Komoot_{kli.carID}: error parsing tour ID {tour["id"]}");
                                        }
                                    }
                                    else if (tour.ContainsKey("id") && tour.ContainsKey("type"))
                                    {
                                        Logfile.Log($"Komoot_{kli.carID}: tour {tour["id"]} skipped, type: {tour["type"]} ...");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return komootTours;
        }

        private static KomootLoginInfo Login(KomootLoginInfo kli)
        {
            Logfile.Log($"Komoot_{kli.carID}: logging in as {kli.username} ...");
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{kli.username}:{kli.password}")));
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://api.komoot.de/v006/account/email/{kli.username}/")))
                {
                    HttpResponseMessage result = httpClient.SendAsync(request).Result;
                    if (result.IsSuccessStatusCode)
                    {
                        string resultContent = result.Content.ReadAsStringAsync().Result;
                        Tools.DebugLog($"Komoot_{kli.carID} login result: {resultContent.Length}");
                        /* expected JSON
{
    "email": "abc@xyz.net",
    "username": "1234567890",
    "user": {
        "createdAt": "2018-07-12 16:16:53 +0000",
        "username": "1234567890",
        "displayname": "DisplayNameXY",
        "content": {
            "hasImage": true
        },
        "state": "1",
        "newsletter": true,
        "welcomeMails": false,
        "metric": true,
        "locale": "de_DE",
        "imageUrl": "https://xxx.cloudfront.net/www/000/defaultuserimage-full/0",
        "fitness": {
            "personalised": false
        }
    },
    "password": "asdfasdfasdf"
}
						 */
                        dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);
                        if (jsonResult.ContainsKey("user"))
                        {
                            dynamic jsonUser = jsonResult["user"];
                            if (jsonUser.ContainsKey("displayname") && jsonResult.ContainsKey("username") && jsonResult.ContainsKey("password"))
                            {
                                kli.user_id = jsonResult["username"];
                                kli.token = jsonResult["password"];
                                kli.loginSuccessful = true;
                                Logfile.Log($"Komoot_{kli.carID}: logged in as {jsonUser["displayname"]}");
                            }
                            else
                            {
                                Logfile.Log($"Komoot_{kli.carID}: login failed - user JSON does not contain displayname");
                            }
                        }
                        else
                        {
                            Logfile.Log($"Komoot_{kli.carID}: login failed - JSON does not contain user");
                        }
                    }
                    else
                    {
                        Logfile.Log($"Komoot_{kli.carID}: login failed ({result.StatusCode})");
                    }
                }
            }
            return kli;
        }

        internal static bool CanHandleRequest(HttpListenerRequest request)
        {
            return EndPoints.ContainsValue(request.Url.LocalPath);
        }

        internal static void HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            switch (true)
            {
                case bool _ when request.Url.LocalPath.Equals(EndPoints["KomootListSettings"], StringComparison.Ordinal):
                    KomootListSettings(request, response);
                    break;
                case bool _ when request.Url.LocalPath.Equals(EndPoints["KomootSaveSettings"], StringComparison.Ordinal):
                    KomootSaveSettings(request, response);
                    break;
                case bool _ when request.Url.LocalPath.Equals(EndPoints["KomootReimport"], StringComparison.Ordinal):
                    CheckAllTours(request, response);
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    WebServer.WriteString(response, @"URL Not Found!");
                    break;
            }
        }

        private static void CheckAllTours(HttpListenerRequest _, HttpListenerResponse response)
        {
            Logfile.Log("CheckAllTours");
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    id,
    tesla_name,
    tesla_password
FROM
    cars
WHERE
    tesla_name LIKE 'KOMOOT:%'", con))
                {
                    MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                    while (dr.Read() && dr[0] != DBNull.Value && dr[1] != DBNull.Value && dr[2] != DBNull.Value)
                    {
                        int carID = Convert.ToInt32(dr["id"], Tools.ciDeDE);
                        string username = dr["tesla_name"].ToString().Replace("KOMOOT:", string.Empty);
                        Logfile.Log($"CheckAllTours carID {carID} username {username}");
                        string password = dr["tesla_password"].ToString();
                        KomootLoginInfo kli = new KomootLoginInfo(carID, username, password, string.Empty, string.Empty);
                        kli = Login(kli);
                        if (!kli.loginSuccessful)
                        {
                            Logfile.Log($"CheckAllTours carID {kli.carID} Login failed!");
                        }
                        else
                        {
                            Dictionary<long, KomootTour> tours = DownloadTours(kli, true);
                            Logfile.Log($"CheckAllTours carID {kli.carID} tours found: {tours.Count}");
                            if (tours.Count > 0)
                            {
                                ParseTours(kli, tours, true, true);
                            }
                            else
                            {
                                Logfile.Log($"CheckAllTours carID {kli.carID} no tours found!");
                            }
                        }
                    }
                }
            }
            Logfile.Log("CheckAllTours done");
            WebServer.WriteString(response, "OK");
        }

        private static void KomootSaveSettings(HttpListenerRequest request, HttpListenerResponse response)
        {
            using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
            {
                string data = reader.ReadToEnd();
                Tools.DebugLog($"KomootSaveSettings request: {data}");
                try
                {
                    dynamic komootSettings = JsonConvert.DeserializeObject(data);
                    foreach (dynamic komootSetting in komootSettings)
                    {
                        if (komootSetting.ContainsKey("komoot_carid")
                            && komootSetting.ContainsKey("komoot_user")
                            && komootSetting.ContainsKey("komoot_passwd")
                            && komootSetting.ContainsKey("komoot_displayname"))
                        {
                            int komoot_carid = (int)komootSetting["komoot_carid"];
                            if (komoot_carid == -1)
                            {
                                // new entry
                                komoot_carid = DBHelper.GetNextAvailableCarID();
                            }
                            string komoot_user = "KOMOOT:" + komootSetting["komoot_user"];
                            string komoot_passwd = komootSetting["komoot_passwd"];
                            string komoot_displayname = komootSetting["komoot_displayname"];
                            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                            {
                                con.Open();
                                using (MySqlCommand cmd = new MySqlCommand(@"
INSERT INTO cars SET
    id = @komoot_carid,
    tesla_name = @komoot_user,
    tesla_password = @komoot_passwd,
    display_name = @komoot_displayname,
    vin = @VIN
ON DUPLICATE KEY UPDATE
    id = @komoot_carid,
    tesla_name = @komoot_user,
    tesla_password = @komoot_passwd,
    display_name = @komoot_displayname,
    vin = @VIN
"
                                , con))
                                {
                                    cmd.Parameters.AddWithValue("@komoot_carid", komoot_carid);
                                    cmd.Parameters.AddWithValue("@komoot_user", komoot_user);
                                    cmd.Parameters.AddWithValue("@komoot_passwd", komoot_passwd);
                                    cmd.Parameters.AddWithValue("@komoot_displayname", komoot_displayname);
                                    cmd.Parameters.AddWithValue("@VIN", $"KOMOOT{komoot_carid}");
                                    int rowsAffected = SQLTracer.TraceNQ(cmd, out _);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToExceptionless().FirstCarUserID().Submit();
                    Logfile.Log(ex.ToString());
                    WebServer.WriteString(response, "not OK");
                    return;
                }
            }
            WebServer.WriteString(response, "OK");
        }

        private static void KomootListSettings(HttpListenerRequest _, HttpListenerResponse response)
        {
            List<object> komootConfigs = new List<object>();
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    id,
    tesla_name,
    tesla_password,
    display_name
FROM
    cars
WHERE
    tesla_name LIKE 'KOMOOT:%'
"
                , con))
                {
                    MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                    while (dr.Read())
                    {
                        Dictionary<string, object> komootConfig = new Dictionary<string, object>
                        {
                            { "carid", dr["id"] },
                            { "user", dr["tesla_name"] },
                            { "passwd", dr["tesla_password"] },
                            { "displayname", dr["display_name"] },
                        };
                        komootConfigs.Add(komootConfig);
                    }
                }
            }
            string json = JsonConvert.SerializeObject(komootConfigs, Formatting.Indented);
            //Tools.DebugLog($"KomootInfo JSON: {json}");
            WebServer.WriteString(response, json, "application/json");
        }

    }
}

