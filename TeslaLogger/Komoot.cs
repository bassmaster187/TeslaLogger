using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless;
using MySql.Data.MySqlClient;
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
            internal int carID;
            internal long tourID;
            readonly internal DateTime start;
            internal double distance_m = double.NaN;
            readonly string type;
            readonly string sport;
            internal Position firstPosition;
            internal Position lastPosition;
            internal string json = "{}";
            internal DateTime end;

            internal Dictionary<int, Position> positions = new Dictionary<int, Position>();

            public KomootTour(int carID, long tourid, string type, string sport, DateTime start)
            {
                this.carID = carID;
                tourID = tourid;
                this.type = type;
                this.sport = sport;
                this.start = start;
                end = start;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"TourID: {tourID}");
                sb.AppendLine($"Type: {type}");
                sb.AppendLine($"Sport: {sport}");
                sb.AppendLine($"Start: {start}");
                sb.AppendLine($"Positions: {positions.Count}");
                foreach (int key in positions.Keys.OrderBy(k => k))
                {
                    sb.AppendLine($" {key} - ({positions[key].lat},{positions[key].lng}) t:{positions[key].delta_t} s:{positions[key].speed}");
                }
                return sb.ToString();
            }

            internal class Position
            {
                internal double lat;
                internal double lng;
                internal double alt;
                internal long delta_t;
                internal double speed;
                internal double dist_km = 0.0;

                public Position(double lat, double lng, double alt, int delta_t, double speed)
                {
                    this.lat = lat;
                    this.lng = lng;
                    this.alt = alt;
                    this.delta_t = delta_t;
                    this.speed = speed;
                }

                internal double calculateDistance(Position other)
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
                    return dist_km;
                }


                internal double calculateSpeed(Position other)
                {
                    // calculate distance and speed with previous pos
                    // inspired by https://github.com/mapado/haversine/blob/main/haversine/haversine.py
                    double speed = this.dist_km / Math.Abs(this.delta_t - other.delta_t) * 3600000; // km/ms -> km/h
                    return speed;
                }
            }

            internal void addPosition(double lat, double lng, double alt, int delta_t, double speed = double.NaN)
            {
                if (positions.Count == 0)
                {
                    // insert first pos
                    firstPosition = new Position(lat, lng, alt, delta_t, double.IsNaN(speed) ? 0.0 : speed);
                    positions.Add(delta_t, firstPosition);
                    lastPosition = firstPosition;
                }
                else if (!positions.ContainsKey(delta_t))
                {
                    Position newPosition = new Position(lat, lng, alt, delta_t, speed);
                    newPosition.dist_km = newPosition.calculateDistance(lastPosition);
                    if (double.IsNaN(speed))
                    {
                        newPosition.speed = newPosition.calculateSpeed(lastPosition);
                    }
                    positions.Add(delta_t, newPosition);
                    lastPosition = newPosition;
                }
                else
                {
                    Tools.DebugLog($"addPosition(lat:{lat}, lng:{lng}, alt:{alt}, delta_t:{delta_t}, speed:{speed} not added");
                }
                if (start.AddMilliseconds(delta_t) > end)
                {
                    end = start.AddMilliseconds(delta_t);
                }
            }

            internal void DownloadTour(KomootLoginInfo kli)
            {
                Logfile.Log($"#{kli.carID} Komoot: DownloadTour {tourID} ...");
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{kli.user_id}:{kli.token}")));
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://api.komoot.de/v007/tours/{tourID}?_embedded=coordinates,way_types,surfaces,directions,participants,timeline&directions=v2&fields=timeline&format=coordinate_array&timeline_highlights_fields=tips,recommenders")))
                    {
                        HttpResponseMessage result = httpClient.SendAsync(request).Result;
                        if (result.IsSuccessStatusCode)
                        {
                            string resultContent = result.Content.ReadAsStringAsync().Result;
                            Logfile.Log($"#{kli.carID} Komoot: DownloadTour {tourID} done");
                            this.json = resultContent;
                        }
                        else
                        {
                            Logfile.Log($"#{kli.carID} Komoot: DownloadTour{tourID} error: {result.StatusCode}");
                        }
                    }
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
            { "KomootSaveSettings", "/komoot/saveSettings" }
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
                if (!DBHelper.TableExists("komoot"))
                {
                    string sql = @"
CREATE TABLE komoot (
    tourID BIGINT NOT NULL,
    carID int(11) NOT NULL,
    drivestateID int(11) NULL,
    json LONGTEXT NOT NULL,
    UNIQUE ix_tourID(tourID)
)";
                    Logfile.Log(sql);
                    UpdateTeslalogger.AssertAlterDB();
                    DBHelper.ExecuteSQLQuery(sql);
                    Logfile.Log("CREATE TABLE komoot OK");
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("Komoot.CheckSchema: Exception", ex);
            }
        }

        internal static string CheckVIN(int carID, string VIN)
        {
            if (VIN.Length != 17)
            {
                string newVIN = $"KOMOOT{carID}";
                newVIN = newVIN.Length < 17 ? newVIN.PadRight(17, 'A') : newVIN.Substring(0, 17);
                Tools.DebugLog($"CheckVIN rename {VIN}->{newVIN}");
                try
                {
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();
                        using (MySqlCommand cmd = new MySqlCommand(@"
UPDATE
    cars
SET
    vin = @VIN
WHERE
    id = @CarID
"
                        , con))
                        {
                            cmd.Parameters.AddWithValue("@CarID", carID);
                            cmd.Parameters.AddWithValue("@VIN", newVIN);
                            _ = SQLTracer.TraceNQ(cmd, out long _);
                            return newVIN;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToExceptionless().FirstCarUserID().Submit();
                    Logfile.Log(ex.ToString());
                }
            }
            return VIN;
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
                            foreach (int tourID in tours.Keys.OrderBy(k => k))
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
            try
            {
                KomootLoginInfo kli = new KomootLoginInfo(carID, username, password, string.Empty, string.Empty);
                while (true)
                {
                    Work(kli);
                    Thread.Sleep(interval * 1000);
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("Komoot: Exception", ex);
            }
        }

        private static void Work(KomootLoginInfo kli, bool dumpJSON = false)
        {
            Login(kli);
            if (!kli.loginSuccessful)
            {
                Logfile.Log($"#{kli.carID} Komoot: Login failed!");
            }
            else
            {
                Dictionary<long, KomootTour> tours = DownloadTours(kli, dumpJSON);
                if (tours.Count > 0)
                {
                    ParseTours(kli, tours, dumpJSON);
                }
                else
                {
                    Logfile.Log($"#{kli.carID} Komoot: no tours found!");
                }
            }
            Logfile.Log($"#{kli.carID} Komoot: done");
        }

        private static void ParseTours(KomootLoginInfo kli, Dictionary<long, KomootTour> tours, bool dumpJSON = false)
        {
            foreach (int tourid in tours.Keys.OrderBy(k => k))
            {
                KomootTour tour = tours[tourid];
                Logfile.Log($"#{kli.carID} Komoot: ParseTours" + Environment.NewLine + tour);
                // check if tour already exists in table komoot
                if (TourExists(tourid))
                {
                    Tools.DebugLog($"Komoot tour {tourid} exists in table komoot, continue");
                    continue;
                }
                // download tour to get source JSON
                tour.DownloadTour(kli);
                if (dumpJSON)
                {
                    Logfile.Log($"#{kli.carID} Komoot: tour({tourid}) JSON:" + Environment.NewLine + tour.json);
                }
                // check if drivestate already exists
                if (DriveStateExists(kli.carID, tour.start, out int drivestateID))
                {
                    // tour does not exist in komoot table, but drivestate exists
                    // --> insert into komoot table
                    Tools.DebugLog($"Komoot tour {tourid} does not exist in table komoot, but drivestate with carID {kli.carID} start {tours[tourid].start} exists: {drivestateID}");
                    InsertTour(kli.carID, drivestateID, tour);
                    continue;
                }
                // tour does not exist in table komoot nor in table drivestate
                if (dumpJSON)
                {
                    Logfile.Log($"#{kli.carID} Komoot: GetTour({tourid}) JSON:" + Environment.NewLine + tour.json);
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
                dynamic jsonResult = JsonConvert.DeserializeObject(tour.json);
                // check JSON contents
                if (jsonResult.ContainsKey("_embedded") && jsonResult["_embedded"].ContainsKey("coordinates") && jsonResult["_embedded"]["coordinates"].ContainsKey("items"))
                {
                    // JSON OK
                    Logfile.Log($"#{kli.carID} Komoot: ParseTours({tourid}) parsing {jsonResult["_embedded"]["coordinates"]["items"].Count} coordinates ...");
                    foreach (dynamic pos in jsonResult["_embedded"]["coordinates"]["items"])
                    {
                        if (pos.ContainsKey("lat") && pos.ContainsKey("lng") && pos.ContainsKey("alt") && pos.ContainsKey("t"))
                        {
                            if (double.TryParse(pos["lat"].ToString(), out double lat) && double.TryParse(pos["lng"].ToString(), out double _) && double.TryParse(pos["alt"].ToString(), out double _) && int.TryParse(pos["t"].ToString(), out int _))
                            {
                                tour.addPosition(lat, double.Parse(pos["lng"].ToString()), double.Parse(pos["alt"].ToString()), int.Parse(pos["t"].ToString()));
                            }
                            else
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.AppendLine();
                                sb.Append("lat:");
                                sb.AppendLine(pos["lat"]);
                                sb.Append("lng:");
                                sb.AppendLine(pos["lng"]);
                                sb.Append("alt:");
                                sb.AppendLine(pos["alt"]);
                                sb.Append("t:");
                                sb.AppendLine(pos["t"]);
                                Logfile.Log($"#{kli.carID} Komoot: ParseTours({tourid}) parsing tours.pos error - error parsing JSON contents" + sb.ToString());
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
                            Logfile.Log($"#{kli.carID} Komoot: ParseTours({tourid}) parsing tours.pos error - missing JSON contents" + sb.ToString());
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
                    Logfile.Log($"#{kli.carID} Komoot: parsing tours error - missing JSON contents" + sb.ToString() + tour.json);
                }
                // positions parsed, continue to insert positions into table pos
                Tools.DebugLog($"#{kli.carID} Komoot: ParseTours({tourid}) " + Environment.NewLine + tour);
                // find initial odometer for first pos
                double odo = GetInitialOdo(tour.carID, tour.start);
                Tools.DebugLog($"#{kli.carID} Komoot: ParseTours({tourid}) initialOdo:{odo}");
                int firstPosID = 0;
                int LastPosId = 0;
                foreach (int posID in tour.positions.Keys.OrderBy(k => k))
                {
                    KomootTour.Position pos = tour.positions[posID];
                    if (pos == tour.firstPosition)
                    {
                        firstPosID = InsertPos(tour.carID, pos.lat, pos.lng, tour.start.AddMilliseconds(pos.delta_t), pos.speed, pos.alt, odo);
                    }
                    else
                    {
                        odo = odo + pos.dist_km;
                        LastPosId = InsertPos(tour.carID, pos.lat, pos.lng, tour.start.AddMilliseconds(pos.delta_t), pos.speed, pos.alt, odo);
                    }
                }
                if (firstPosID > 0 && LastPosId > 0)
                {
                    // successfully added positions to table pos
                    drivestateID = CreateDriveState(tour.carID, tour.start, firstPosID, tour.end, LastPosId);
                    InsertTour(kli.carID, drivestateID, tour);
                }
                else
                {
                    Logfile.Log($"#{kli.carID} Komoot: error - no positions added to table pos - tour JSON:" + Environment.NewLine + tour.json);
                }
            }
        }

        private static void InsertTour(int carID, int drivestateID, KomootTour komootTour)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
INSERT
    komoot(
        tourID,
        carID,
        drivestateID,
        json
)
VALUES(
    @tourID,
    @carID,
    @drivestateID,
    @json
)"
                    , con))
                    {
                        cmd.Parameters.AddWithValue("@tourID", komootTour.tourID);
                        cmd.Parameters.AddWithValue("@CarID", carID);
                        cmd.Parameters.AddWithValue("@drivestateID", drivestateID);
                        cmd.Parameters.AddWithValue("@json", komootTour.json);
                        _ = SQLTracer.TraceNQ(cmd, out long _);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
        }

        private static bool DriveStateExists(int carID, DateTime start, out int drivestateID)
        {
            drivestateID = -1;
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    id
FROM
    drivestate
WHERE
    carID = @carID
    AND StartDate = @StartDate", con))
                    {
                        cmd.Parameters.AddWithValue("@carID", carID);
                        cmd.Parameters.AddWithValue("@StartDate", start);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read() && dr[0] != DBNull.Value)
                        {
                            if (int.TryParse(dr[0].ToString(), out drivestateID))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
            return false;
        }

        private static bool TourExists(int tourid)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    count(tourID)
FROM
    komoot
WHERE
    tourID = @tourID", con))
                    {
                        cmd.Parameters.AddWithValue("@tourID", tourid);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read() && dr[0] != DBNull.Value)
                        {
                            if (int.TryParse(dr[0].ToString(), out int count))
                            {
                                return count > 0 ? true : false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
            return false;
        }

        private static double GetInitialOdo(int carid, DateTime start)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    odometer
FROM
    pos
WHERE
    CarID = @CarID
    AND Datum < @start
ORDER BY Datum DESC
LIMIT 1", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", carid);
                        cmd.Parameters.AddWithValue("@start", start);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read() && dr[0] != DBNull.Value)
                        {
                            if (double.TryParse(dr[0].ToString(), out double odo))
                            {
                                return odo;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
            return 0.0;
        }

        private static int CreateDriveState(int carID, DateTime start, int firstPosID, DateTime end, int lastPosID)
        {
            Logfile.Log($"#{carID} Komoot: CreateDriveState {firstPosID}->{lastPosID} {start} {end} ...");
            int drivestateID = -1;
            try
            {
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
                        using (MySqlCommand cmdid = new MySqlCommand("SELECT LAST_INSERT_ID()", con))
                        {
                            drivestateID = Convert.ToInt32(cmdid.ExecuteScalar());
                        }
                    }
                }
                _ = Task.Factory.StartNew(() =>
                {
                    // update start address and end address
                    WebHelper.UpdateAllPOIAddresses();
                    // create timeline maps
                    StaticMapService.GetSingleton().Enqueue(carID, firstPosID, lastPosID, 0, 0, StaticMapProvider.MapMode.Dark, StaticMapProvider.MapSpecial.None);
                    StaticMapService.CreateParkingMapFromPosid(firstPosID);
                    StaticMapService.CreateParkingMapFromPosid(lastPosID);
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
            return drivestateID;
        }

        private static int InsertPos(int carID, double lat, double lng, DateTime timestamp, double speed, double alt, double odometer)
        {
            int posid = 0;
            try
            {
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
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
            return posid;
        }

        private static Dictionary<long, KomootTour> DownloadTours(KomootLoginInfo kli, bool dumpJSON = false)
        {
            Logfile.Log($"#{kli.carID} Komoot: downloading tours ...");
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
                            Tools.DebugLog($"#{kli.carID} Komoot: GetTours result: {resultContent.Length}");
                            if (dumpJSON)
                            {
                                Logfile.Log($"#{kli.carID} Komoot: tours JSON:" + Environment.NewLine + resultContent);
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
                                Logfile.Log($"#{kli.carID} Komoot: found {jsonResult["_embedded"]["tours"].Count} tours ...");
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
                                            KomootTour newTour = new KomootTour(kli.carID, tourid, tour["type"].ToString(), tour["sport"].ToString(), DateTime.Parse(tour["date"].ToString()));
                                            if (tour.ContainsKey("id") && double.TryParse(tour["distance"].ToString(), out double _))
                                            {
                                                newTour.distance_m = double.Parse(tour["distance"].ToString());
                                            }
                                            komootTours.Add(tourid, newTour);
                                        }
                                        else
                                        {
                                            Logfile.Log($"#{kli.carID} Komoot: error parsing tour ID {tour["id"]}");
                                        }
                                    }
                                    else if (tour.ContainsKey("id") && tour.ContainsKey("type"))
                                    {
                                        Logfile.Log($"#{kli.carID} Komoot: tour {tour["id"]} skipped, type: {tour["type"]} ...");
                                    }
                                }
                            }
                            else
                            {
                                Logfile.Log($"#{kli.carID} Komoot: error: tours does not contain _embedded.tours");
                            }
                        }
                    }
                }
            }
            return komootTours;
        }

        private static KomootLoginInfo Login(KomootLoginInfo kli)
        {
            Logfile.Log($"#{kli.carID} Komoot: logging in as {kli.username} ...");
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{kli.username}:{kli.password}")));
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://api.komoot.de/v006/account/email/{kli.username}/")))
                {
                    HttpResponseMessage result = httpClient.SendAsync(request).Result;
                    if (result.IsSuccessStatusCode)
                    {
                        string resultContent = result.Content.ReadAsStringAsync().Result;
                        Tools.DebugLog($"#{kli.carID} Komoot: login result: {resultContent.Length}");
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
                                Logfile.Log($"#{kli.carID} Komoot: logged in as {jsonUser["displayname"]}");
                            }
                            else
                            {
                                Logfile.Log($"#{kli.carID} Komoot: login failed - user JSON does not contain displayname");
                            }
                        }
                        else
                        {
                            Logfile.Log($"#{kli.carID} Komoot: login failed - JSON does not contain user");
                        }
                    }
                    else
                    {
                        Logfile.Log($"#{kli.carID} Komoot: login failed ({result.StatusCode})");
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
                    HandleRequest_KomootListSettings(request, response);
                    break;
                case bool _ when request.Url.LocalPath.Equals(EndPoints["KomootSaveSettings"], StringComparison.Ordinal):
                    HandleRequest_KomootSaveSettings(request, response);
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    WebServer.WriteString(response, @"URL Not Found!");
                    break;
            }
        }

        private static void HandleRequest_KomootSaveSettings(HttpListenerRequest request, HttpListenerResponse response)
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

        private static void HandleRequest_KomootListSettings(HttpListenerRequest _, HttpListenerResponse response)
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

