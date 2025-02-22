using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;
using Exceptionless;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

// inspired by https://github.com/timschneeb/KomootGPX

namespace TeslaLogger
{
	public class Komoot
	{
		private readonly int interval = 6 * 60 * 60; // 6 hours in seconds
		private readonly int carID = -1;
		private readonly string username = string.Empty;
		private readonly string password = string.Empty;
		private string user_id = string.Empty;
		private string token = string.Empty;

		public Komoot(int CarID, string Username, string Password)
		{
			this.carID = CarID;
			username = Username;
			password = Password;
		}

		// main loop
		public void Run()
		{
			try
			{
				while (true)
				{
					Work();
					Thread.Sleep(interval * 1000);
				}
			}
			catch (Exception ex)
			{
				ex.ToExceptionless().FirstCarUserID().Submit();
				Tools.DebugLog("Komoot: Exception", ex);
			}
		}

		private void Work()
		{
			Login();
			List<int> tours = GetTours();
			tours.Sort();
			ParseTours(tours);
		}

		private void ParseTours(List<int> tours)
		{
			string KVSkey = $"Komoot_{carID}_Max_Tour_ID";
			foreach (int tourid in tours)
			{
				if (KVS.Get(KVSkey, out int maxTourID) == KVS.FAILED) {
					maxTourID = 0;
				}
				if (tourid > maxTourID) {
					Logfile.Log($"Komoot_{carID}: getting tour {tourid} ...");
					using (HttpClient httpClient = new HttpClient())
					{
						httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user_id}:{token}")));
						using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://api.komoot.de/v007/tours/{tourid}?_embedded=coordinates,way_types,surfaces,directions,participants,timeline&directions=v2&fields=timeline&format=coordinate_array&timeline_highlights_fields=tips,recommenders")))
						{
							HttpResponseMessage result = httpClient.SendAsync(request).Result;
							if (result.IsSuccessStatusCode)
							{
								string resultContent = result.Content.ReadAsStringAsync().Result;
								Tools.DebugLog($"Komoot_{{carID}} GetTour({tourid}) result: {resultContent.Length}");
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
									long firstPosID = 0;
									long lastPosID = 0;
									long t = 0;
									long prev_t = 0;
									double odo = GetMaxOdo(carID);
                                    Logfile.Log($"Komoot_{carID}: inserting {jsonResult["_embedded"]["coordinates"]["items"].Count} positions ...");
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
													firstPosID = InsertPos(carID, lat, lng, start, speed, alt, odo);
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
													lastPosID = InsertPos(carID, lat, lng, end, speed, alt, odo);
												}
											}
										}
									}
									if (firstPosID > 0 && lastPosID > 0)
									{
										CreateDriveState(carID, start, firstPosID, end, lastPosID);
									}
								}
							}
						}
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
						if (double.TryParse(dr[0].ToString(), out double pos)) {
							return pos;
						}
                    }
                }
            }
            return 0.0;
        }

        private static void CreateDriveState(int carID, DateTime start, long firstPosID, DateTime end, long lastPosID)
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
		} 

        private static long InsertPos(int carID, double lat, double lng, DateTime timestamp, double speed, double alt, double odometer)
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

        private List<int> GetTours()
		{
			Logfile.Log($"Komoot_{carID}: getting tours ...");
			List<int> tours = new List<int>();
			bool nextPage = true;
			string url = $"https://api.komoot.de/v007/users/{user_id}/tours/";
			while (nextPage)
			{
				using (HttpClient httpClient = new HttpClient())
				{
					httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user_id}:{token}")));
					using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(url)))
					{
						HttpResponseMessage result = httpClient.SendAsync(request).Result;
						if (result.IsSuccessStatusCode)
						{
							string resultContent = result.Content.ReadAsStringAsync().Result;
							Tools.DebugLog($"Komoot_{{carID}} GetTours result: {resultContent.Length}");
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
								foreach (dynamic tour in jtours)
								{
									if (tour.ContainsKey("id") && tour.ContainsKey("type") && tour["type"].ToString().Equals("tour_recorded"))
									{
										if (Int32.TryParse(tour["id"].ToString(), out int tourid))
										{
											tours.Add(tourid);
										}
									}
								}
                            }
						}
					}
				}
			}
			Tools.DebugLog("GetTours() -> " + string.Join(",", tours));
			return tours;
        }

            private void Login()
        {
            Logfile.Log($"Komoot_{carID}: logging in as {username} ...");
            using (HttpClient httpClient = new HttpClient())
            {
				httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://api.komoot.de/v006/account/email/{username}/")))
                {
                    HttpResponseMessage result = httpClient.SendAsync(request).Result;
					if (result.IsSuccessStatusCode)
					{
						string resultContent = result.Content.ReadAsStringAsync().Result;
						Tools.DebugLog($"Komoot_{{carID}} login result: {resultContent.Length}");
                        dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);
						if (jsonResult.ContainsKey("user"))
						{
                            dynamic jsonUser = jsonResult["user"];
							if (jsonUser.ContainsKey("displayname") && jsonResult.ContainsKey("username") && jsonResult.ContainsKey("password"))
							{
								user_id = jsonResult["username"];
								token = jsonResult["password"];
                                Logfile.Log($"Komoot_{carID}: logged in as {jsonUser["displayname"]}");
                            }
                            else
                            {
                                Logfile.Log($"Komoot_{carID}: login failed - user JSON does not contain displayname");
                            }
                        }
                        else
                        {
                            Logfile.Log($"Komoot_{carID}: login failed - JSON does not contain user");
                        }
                    }
					else
					{
                        Logfile.Log($"Komoot_{carID}: login failed ({result.StatusCode})");
                    }
                }
            }
        }
    }
}

