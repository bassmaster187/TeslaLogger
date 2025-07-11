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
            internal bool loginSuccessful;

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
            readonly internal DateTime startTS;
            internal double distance_m = double.NaN;
            internal double distance_calculated;
            readonly string type;
            readonly string sport;
            internal Position firstPosition;
            internal Position lastPosition;
            internal string json = "{}";
            internal DateTime endTS;

            internal Dictionary<int, Position> positions = new Dictionary<int, Position>();
            internal double odometer;

            public KomootTour(int carID, long tourid, string type, string sport, DateTime start)
            {
                this.carID = carID;
                tourID = tourid;
                this.type = type;
                this.sport = sport;
                this.startTS = start;
                endTS = start;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"TourID: {tourID}");
                sb.AppendLine($"Type: {type}");
                sb.AppendLine($"Sport: {sport}");
                sb.AppendLine($"Start: {startTS}");
                sb.AppendLine($"Positions: {positions.Count}");
                foreach (int key in positions.Keys.OrderBy(k => k))
                {
                    sb.AppendLine($" {key} - ({positions[key].lat},{positions[key].lng}) t:{positions[key].delta_t} s:{positions[key].speed} dist:{positions[key].dist_km} head:{positions[key].heading}");
                }
                return sb.ToString();
            }

            internal class Position
            {
                internal double lat; // defaults to 0.0
                internal double lng; // defaults to 0.0
                internal double alt; // defaults to 0.0
                internal long delta_t;
                internal double speed; // defaults to 0.0
                internal double dist_km; // defaults to 0.0
                internal double heading; // defaults to 0.0

                public Position(double lat, double lng, double alt, int delta_t, double speed)
                {
                    this.lat = lat;
                    this.lng = lng;
                    this.alt = alt;
                    this.delta_t = delta_t;
                    this.speed = speed;
                }

                /// Calculates the distance in kilometers between the current position and another position
                /// using the Haversine formula.
                /// </summary>
                /// <param name="other">The other position to calculate the distance to.</param>
                /// <returns>The distance in kilometers between the two positions.</returns>
                internal double CalculateDistance(Position other)
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

                /// <summary>
                /// Calculates the speed between the current position and another position.
                /// </summary>
                /// <param name="other">The other position to compare with the current position.</param>
                /// <returns>
                /// The calculated speed in kilometers per hour (km/h), based on the distance 
                /// between the two positions and the time difference.
                /// </returns>
                /// <remarks>
                /// The calculation uses the Haversine formula to determine the distance 
                /// between two geographical points and assumes the time difference is in milliseconds.
                /// </remarks>
                internal double CalculateSpeed(Position other)
                {
                    // calculate distance and speed with previous pos
                    // inspired by https://github.com/mapado/haversine/blob/main/haversine/haversine.py
                    double speed = CalculateDistance(other) / Math.Abs(this.delta_t - other.delta_t) * 3600000; // km/ms -> km/h
                    return speed;
                }
            }

            internal void AddPosition(double lat, double lng, double alt, int delta_t, double speed = double.NaN)
            {
                if (positions.Count == 0)
                {
                    // insert first pos
                    firstPosition = new Position(lat, lng, alt, delta_t, double.IsNaN(speed) ? 0.0 : speed);
                    positions.Add(delta_t, firstPosition);
                    lastPosition = firstPosition;
                }
                // TODO: .net8 change to TryAdd for performance reasons
                else if (!positions.ContainsKey(delta_t))
                {
                    Position newPosition = new Position(lat, lng, alt, delta_t, speed);
                    newPosition.dist_km = newPosition.CalculateDistance(lastPosition);
                    distance_calculated += newPosition.dist_km;
                    if (double.IsNaN(speed))
                    {
                        newPosition.speed = newPosition.CalculateSpeed(lastPosition);
                    }
                    positions.Add(delta_t, newPosition);
                    lastPosition = newPosition;
                }
                else
                {
                    Tools.DebugLog($"addPosition(lat:{lat}, lng:{lng}, alt:{alt}, delta_t:{delta_t}, speed:{speed} not added");
                }
                if (startTS.AddMilliseconds(delta_t) > endTS)
                {
                    endTS = startTS.AddMilliseconds(delta_t);
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
                        DateTime start = DateTime.UtcNow;
                        HttpResponseMessage result = httpClient.SendAsync(request).Result;
                        DBHelper.AddMothershipDataToDB("Komoot: DownloadTour", start, (int)result.StatusCode, kli.carID);
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

            /// <summary>
            /// Adjusts the distances of positions in the tour to correct any discrepancies
            /// between the computed total distance and the actual measured distance.
            /// </summary>
            /// <remarks>
            /// This method calculates a correction factor based on the ratio of the actual
            /// measured distance to the computed total distance. It then applies this correction
            /// factor to each position's distance to ensure the total matches the measured value.
            /// </remarks>
            internal void CorrectPositionDistances()
            {
                double distance_computed_km = 0.0;
                foreach (int posID in positions.Keys.OrderBy(k => k))
                {
                    distance_computed_km += positions[posID].dist_km;
                }
                double correction_factor = distance_m / 1000 / distance_computed_km;
                Tools.DebugLog($"Tour {tourID} correction_factor:{correction_factor}");
                foreach (int posID in positions.Keys.OrderBy(k => k))
                {
                    positions[posID].dist_km *= correction_factor;
                }
            }

            internal void ComputeHeading()
            {
                if (this.positions.Count == 2)
                {
                    // ignore just one position, compute special case two positions
                    this.firstPosition.heading = GetBearing(this.firstPosition.lat, this.firstPosition.lng, this.lastPosition.lat, this.lastPosition.lng);
                    this.lastPosition.heading = GetBearing(this.firstPosition.lat, this.firstPosition.lng, this.lastPosition.lat, this.lastPosition.lng);
                }
                else if (this.positions.Count > 2)
                {
                    // 3 or more positions
                    Dictionary<int, int> positionKeys = new Dictionary<int, int>();
                    int index = 0;
                    foreach (int posID in positions.Keys.OrderBy(k => k))
                    {
                        positionKeys.Add(index, posID);
                        index += 1;
                    }
                    // compute first
                    positions[positionKeys[0]].heading = GetBearing(positions[positionKeys[0]].lat, positions[positionKeys[0]].lng, positions[positionKeys[1]].lat, positions[positionKeys[1]].lng);
                    // compute last
                    positions[positionKeys[positionKeys.Count - 1]].heading = GetBearing(positions[positionKeys[positionKeys.Count - 2]].lat, positions[positionKeys[positionKeys.Count - 2]].lng, positions[positionKeys[positionKeys.Count - 1]].lat, positions[positionKeys[positionKeys.Count - 1]].lng);
                    // compute everything in between 2..(n-1)
                    for (index = 1; index < positionKeys.Count - 1; index++)
                    {
                        Position prevP = positions[positionKeys[index - 1]];
                        Position currP = positions[positionKeys[index]];
                        Position nextP = positions[positionKeys[index + 1]];
                        double bearing1 = GetBearing(prevP.lat, prevP.lng, currP.lat, currP.lng);
                        double bearing2 = GetBearing(currP.lat, currP.lng, nextP.lat, nextP.lng);
                        currP.heading = (bearing1 + bearing2) / 2;
                        //Tools.DebugLog($"({prevP.lat},{prevP.lng})->({currP.lat},{currP.lng}) heading:{prevP.heading}{currP.heading} diff:{Math.Abs(prevP.heading - currP.heading)}");
                    }
                }
            }

            // generated by ChatGPT
            private static double GetBearing(double lat1, double lon1, double lat2, double lon2)
            {
                double radLat1 = ToRadians(lat1);
                double radLat2 = ToRadians(lat2);
                double deltaLon = ToRadians(lon2 - lon1);

                double y = Math.Sin(deltaLon) * Math.Cos(radLat2);
                double x = Math.Cos(radLat1) * Math.Sin(radLat2) - Math.Sin(radLat1) * Math.Cos(radLat2) * Math.Cos(deltaLon);
                double theta = Math.Atan2(y, x);

                double bearing = (ToDegrees(theta) + 360) % 360; // Normalisierung auf 0–360°
                return bearing;
            }

            private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
            private static double ToDegrees(double radians) => radians * 180.0 / Math.PI;

            internal void CheckSpeed(KomootLoginInfo kli)
            {
                if (this.positions.Count > 3)
                {
                    Tools.DebugLog("CheckSpeed() speed4");
                    // 4 or more positions
                    Dictionary<int, int> positionKeys = new Dictionary<int, int>();
                    int index = 0;
                    foreach (int posID in positions.Keys.OrderBy(k => k))
                    {
                        positionKeys.Add(index, posID);
                        index += 1;
                    }
                    for (index = 1; index < positionKeys.Count - 3; index++)
                    {
                        Position prevP = positions[positionKeys[index - 1]];
                        Position currP1 = positions[positionKeys[index]];
                        Position currP2 = positions[positionKeys[index + 1]];
                        Position nextP = positions[positionKeys[index + 2]];
                        double acceleration1 = ((currP1.speed - prevP.speed) / 3.6) / ((currP1.delta_t - prevP.delta_t) / 1000);
                        double acceleration2 = ((currP2.speed - currP1.speed) / 3.6) / ((currP2.delta_t - currP1.delta_t) / 1000);
                        double acceleration3 = ((nextP.speed - currP2.speed) / 3.6) / ((nextP.delta_t - currP2.delta_t) / 1000);
                        //Tools.DebugLog($"speed:{prevP.speed}->{currP1.speed}->{currP2.speed}->{nextP.speed} acceleration1:{acceleration1}m/s acceleration2:{acceleration2}m/s acceleration3:{acceleration3}m/s ");
                        if (acceleration1 > 1.1 && Math.Abs(acceleration2) < 0.5 && acceleration3 < -1.1)
                        {
                            // drop speed at currP1 and CurrP2 and replace with weighted 3:2 avg(prevP,nextP)
                            Logfile.Log($"#{kli.carID} Komoot: speed4: {Math.Round(prevP.speed, 2)}->{Math.Round(currP1.speed, 2)}->{Math.Round(currP2.speed, 2)}->{Math.Round(nextP.speed, 2)} acc:{Math.Round(acceleration1, 2)}-{Math.Round(acceleration2, 2)} correct speed:{Math.Round(currP1.speed, 2)}->{Math.Round((prevP.speed * 3 + nextP.speed * 2) / 5.0, 2)},{Math.Round(currP2.speed, 2)}->{Math.Round((prevP.speed * 2 + nextP.speed * 3) / 5.0, 2)}");
                            currP1.speed = (prevP.speed * 3 + nextP.speed * 2) / 5.0;
                            currP2.speed = (prevP.speed * 2 + nextP.speed * 3) / 5.0;
                        }
                        if (acceleration1 > 0.75 && Math.Abs(acceleration2) < 0.25 && acceleration3 < -0.75)
                        {
                            Tools.DebugLog($"speed4: candidate? {prevP.speed}->{currP1.speed}->{currP2.speed}->{nextP.speed} acceleration1:{acceleration1}m/s acceleration2:{acceleration2}m/s acceleration3:{acceleration3}m/s ");
                        }
                    }
                }
                if (this.positions.Count > 2)
                {
                    Tools.DebugLog("CheckSpeed() speed3");
                    // 3 or more positions
                    Dictionary<int, int> positionKeys = new Dictionary<int, int>();
                    int index = 0;
                    foreach (int posID in positions.Keys.OrderBy(k => k))
                    {
                        positionKeys.Add(index, posID);
                        index += 1;
                    }
                    for (index = 1; index < positionKeys.Count - 2; index++)
                    {
                        Position prevP = positions[positionKeys[index - 1]];
                        Position currP = positions[positionKeys[index]];
                        Position nextP = positions[positionKeys[index + 1]];
                        double acceleration1 = ((currP.speed - prevP.speed) / 3.6) / ((currP.delta_t - prevP.delta_t) / 1000);
                        double acceleration2 = ((nextP.speed - currP.speed) / 3.6) / ((nextP.delta_t - currP.delta_t) / 1000);
                        //Tools.DebugLog($"speed:{prevP.speed}->{currP.speed}->{nextP.speed} acceleration1:{acceleration1}m/s acceleration2:{acceleration2}m/s");
                        if (acceleration1 > 1.1 && acceleration2 < -1.1)
                        {
                            // drop speed at currP and replace with avg(prevP,nextP)
                            Tools.DebugLog($"#{kli.carID} Komoot: speed3: {Math.Round(prevP.speed, 2)}->{Math.Round(currP.speed, 2)}->{Math.Round(nextP.speed, 2)} acc:{Math.Round(acceleration1, 2)}-{Math.Round(acceleration2, 2)} correct speed:{Math.Round(currP.speed, 2)}->{Math.Round((prevP.speed+nextP.speed)/2.0,2 )}");
                            currP.speed = (prevP.speed + nextP.speed) / 2.0;
                        }
                    }
                }
            }
        }

        private readonly int interval = 3 * 60 * 60; // 3 hours in seconds
        private readonly int carID = -1;
        private readonly string username = string.Empty;
        private readonly string password = string.Empty;

        private bool workNow; // defaults to false
        private static List<Komoot> komootInstances = new List<Komoot>();

        private static readonly Dictionary<string, string> EndPoints = new Dictionary<string, string>()
        {
            { "KomootListSettings", "/komoot/listSettings" },
            { "KomootSaveSettings", "/komoot/saveSettings" },
            { "KomootWorkNow", "/komoot/workNow" }
        };

        public Komoot(int CarID, string Username, string Password)
        {
            this.carID = CarID;
            this.username = Username;
            this.password = Password;
            komootInstances.Add(this);
        }

        public void WorkNow()
        {
            Tools.DebugLog("WorkNow()");
            workNow = true;
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
                    Logfile.Log($"Komoot: {sql}");
                    UpdateTeslalogger.AssertAlterDB();
                    DBHelper.ExecuteSQLQuery(sql);
                    Logfile.Log($"Komoot: CREATE TABLE komoot OK");
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

        // main loop
        public void Run()
        {
            try
            {
                KomootLoginInfo kli = new KomootLoginInfo(carID, username, password, string.Empty, string.Empty);
                while (true)
                {
                    Work(kli);
                    for (int i = 0; i < 1000; i++)
                    {
                        if (workNow) { break; }
                        Thread.Sleep((int)(((DateTime.Now.Hour >= 4 && DateTime.Now.Hour < 18) ? 0.5 : 1.0) * (double)interval));
                    }
                    workNow = false;
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                // Tools.DebugLog("Komoot: Exception", ex);
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
                CurrentJSON.FromKVS(kli.carID);
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
            foreach (long tourid in tours.Keys.OrderBy(k => k))
            {
                KomootTour tour = tours[tourid];
                // Logfile.Log($"#{kli.carID} Komoot: ParseTours" + Environment.NewLine + tour);
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
                if (DriveStateExists(kli.carID, tour.startTS, out int drivestateID))
                {
                    // tour does not exist in komoot table, but drivestate exists
                    // --> insert into komoot table
                    Tools.DebugLog($"Komoot tour {tourid} does not exist in table komoot, but drivestate with carID {kli.carID} start {tours[tourid].startTS} exists: {drivestateID}");
                    InsertTour(kli.carID, drivestateID, tour);
                    continue;
                }
                // tour does not exist in table komoot nor in table drivestate
                if (dumpJSON)
                {
                    Logfile.Log($"#{kli.carID} Komoot: GetTour({tourid}) JSON:" + Environment.NewLine + tour.json);
                }
                ParseTourJSON(kli, tourid, tour);
                // positions parsed, continue to insert positions into table pos
                // find initial odometer for first pos
                tour.odometer = GetInitialOdo(tour.carID, tour.startTS);
                Logfile.Log($"#{kli.carID} Komoot: ParseTours({tourid}) initialOdo:{tour.odometer}");
                int firstPosID = 0;
                int LastPosId = 0;
                tour.CorrectPositionDistances();
                tour.ComputeHeading();
                tour.CheckSpeed(kli);
                // Tools.DebugLog($"#{kli.carID} Komoot: ParseTours({tourid}) " + Environment.NewLine + tour);
                foreach (int posID in tour.positions.Keys.OrderBy(k => k))
                {
                    KomootTour.Position pos = tour.positions[posID];
                    if (pos == tour.firstPosition)
                    {
                        firstPosID = InsertPos(tour.carID, pos.lat, pos.lng, tour.startTS.AddMilliseconds(pos.delta_t), pos.speed, pos.alt, tour.odometer);
                    }
                    else
                    {
                        tour.odometer = tour.odometer + pos.dist_km;
                        LastPosId = InsertPos(tour.carID, pos.lat, pos.lng, tour.startTS.AddMilliseconds(pos.delta_t), pos.speed, pos.alt, tour.odometer);
                    }
                }
                if (firstPosID > 0 && LastPosId > 0)
                {
                    // successfully added positions to table pos
                    drivestateID = CreateDriveState(tour.carID, tour.startTS, firstPosID, tour.endTS, LastPosId);
                    InsertTour(kli.carID, drivestateID, tour);
                    // mock CurrentJSON
                    MockCurrentJSON(kli, tour);
                }
                else
                {
                    Logfile.Log($"#{kli.carID} Komoot: error - no positions added to table pos - tour JSON:" + Environment.NewLine + tour.json);
                }
            }

        }

        static void MockCurrentJSON(KomootLoginInfo kli, KomootTour tour)
        {
            CurrentJSON.jsonStringHolder[kli.carID] = $@"{{
    ""sleeping"": true,
    ""odometer"": {tour.odometer},
    ""ts"": ""{tour.endTS.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")}"",
    ""latitude"": {tour.lastPosition.lat},
    ""longitude"": {tour.lastPosition.lng},
    ""heading"": {tour.lastPosition.heading},
    ""trip_start_dt"": ""{tour.startTS.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")}"",
    ""trip_duration_sec"": ""{(int)(tour.endTS-tour.startTS).TotalSeconds}"",
    ""locked"": true
}}";
            CurrentJSON.ToKVS(kli.carID);
        }

        private static void ParseTourJSON(KomootLoginInfo kli, long tourid, KomootTour tour)
        {
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
                Dictionary<int, Tuple<double, double, double>> positions = new Dictionary<int, Tuple<double, double, double>>();
                Logfile.Log($"#{kli.carID} Komoot: ParseTourJSON({tourid}) parsing {jsonResult["_embedded"]["coordinates"]["items"].Count} coordinates ...");
                foreach (dynamic pos in jsonResult["_embedded"]["coordinates"]["items"])
                {
                    if (pos.ContainsKey("lat") && pos.ContainsKey("lng") && pos.ContainsKey("alt") && pos.ContainsKey("t"))
                    {
                        if (double.TryParse(pos["lat"].ToString(), out double lat) && double.TryParse(pos["lng"].ToString(), out double _) && double.TryParse(pos["alt"].ToString(), out double _) && int.TryParse(pos["t"].ToString(), out int _))
                        {
                            // generate pseudopositions?
                            if (positions.Count > 0)
                            {
                                var lastpos = positions.OrderByDescending(kvp => kvp.Key).First();
                                if (int.Parse(pos["t"].ToString()) - lastpos.Key > 11000)
                                {
                                    int timediff = int.Parse(pos["t"].ToString()) - lastpos.Key;
                                    int pseudopositions = (int)Math.Floor((timediff - 2000) / 2000.0);
                                    int step = (timediff - 2000) / pseudopositions;
                                    Tools.DebugLog($"pos delta {timediff} -> insert pseudo positions: {pseudopositions}");
                                    Tools.DebugLog($"lastpos delta_t: {lastpos.Key}");
                                    for (int i = 1; i <= pseudopositions; i++)
                                    {
                                        Tools.DebugLog($"add pseudopos {i} at delta_t {lastpos.Key + step * i} {Tuple.Create(lastpos.Value.Item1, lastpos.Value.Item2, lastpos.Value.Item3)}");
                                        positions[lastpos.Key + step * i] = Tuple.Create(lastpos.Value.Item1, lastpos.Value.Item2, lastpos.Value.Item3);
                                    }
                                    Tools.DebugLog($"currpos delta_t: {pos["t"].ToString()}");
                                }
                            }
                            positions[int.Parse(pos["t"].ToString())] = Tuple.Create(lat, double.Parse(pos["lng"].ToString()), double.Parse(pos["alt"].ToString()));
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
                            Logfile.Log($"#{kli.carID} Komoot: ParseTourJSON({tourid}) parsing tours.pos error - error parsing JSON contents" + sb.ToString());
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
                        Logfile.Log($"#{kli.carID} Komoot: ParseTourJSON({tourid}) parsing tours.pos error - missing JSON contents" + sb.ToString());
                    }
                }
                // positions parsed from JSON, pseudopositions generated, now add positions
                Logfile.Log($"#{kli.carID} Komoot: ParseTourJSON({tourid}) found {positions.Count} positions (including generated pseudopositions)");
                foreach (KeyValuePair<int, Tuple<double, double, double>> pos in positions)
                {
                    tour.AddPosition(pos.Value.Item1, pos.Value.Item2, pos.Value.Item3, pos.Key);
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

        private static bool TourExists(long tourid)
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
ORDER BY
  Datum DESC
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
  drivestate (
    CarID,
    StartDate,
    StartPos,
    EndDate,
    EndPos
)
VALUES (
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
                        // update start address and end address
                        int count = 0;
                        WebHelper.UpdateAllPOIAddresses(count, $"{firstPosID},{lastPosID}");
                    }
                }
                _ = Task.Factory.StartNew(() =>
                {
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
  pos (
    CarID,
    Datum,
    lat,
    lng,
    speed,
	altitude,
    odometer
)
VALUES (
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
                        DateTime start = DateTime.UtcNow;
                        HttpResponseMessage result = httpClient.SendAsync(request).Result;
                        DBHelper.AddMothershipDataToDB("Komoot: DownloadTours", start, (int)result.StatusCode, kli.carID);
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
                                            KomootTour newTour = new KomootTour(kli.carID, tourid, tour["type"].ToString(), tour["sport"].ToString(), DateTime.Parse(tour["date"].ToString()).ToLocalTime());
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
                            // komoot responds with the newest tour as first tour
                            // check if we already have this and skip next pages
                            if (TourIsAlreadyInDatabase(komootTours.Keys.Max())) {
                                Tools.DebugLog($"Tour {komootTours.Keys.Max()} is already in the database");
                                Logfile.Log($"#{kli.carID} Komoot: no new tours");
                                nextPage = false;
                            }
                        }
                        else
                        {
                            Logfile.Log($"#{kli.carID} Komoot: download error: {result.StatusCode}");
                        }
                    }
                }
            }
            return komootTours;
        }

        private static bool TourIsAlreadyInDatabase(long tourID)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  tourID
FROM
  komoot
WHERE
  tourID = @tourID
", con))
                    {
                        cmd.Parameters.AddWithValue("@tourID", tourID);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read() && dr[0] != DBNull.Value)
                        {
                            if (long.TryParse(dr[0].ToString(), out long _))
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

        private static KomootLoginInfo Login(KomootLoginInfo kli)
        {
            Logfile.Log($"#{kli.carID} Komoot: logging in as {kli.username} ...");
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{kli.username}:{kli.password}")));
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://api.komoot.de/v006/account/email/{kli.username}/")))
                {
                    DateTime start = DateTime.UtcNow;
                    HttpResponseMessage result = httpClient.SendAsync(request).Result;
                    DBHelper.AddMothershipDataToDB("Komoot: Login", start, (int)result.StatusCode, kli.carID);
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
                case bool _ when request.Url.LocalPath.Equals(EndPoints["KomootWorkNow"], StringComparison.Ordinal):
                    HandleRequest_KomootWorkNow(request, response);
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    WebServer.WriteString(response, @"URL Not Found!");
                    break;
            }
        }

        private static void HandleRequest_KomootWorkNow(HttpListenerRequest _, HttpListenerResponse response)
        {
            foreach (Komoot komoot in komootInstances)
            {
                komoot.WorkNow();
            }
            WebServer.WriteString(response, "OK");
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
INSERT INTO
  cars
SET
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

