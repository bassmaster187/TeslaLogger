using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using Exceptionless;
using MySqlX.XDevAPI;
using Newtonsoft.Json;

namespace TeslaLogger
{
	public class Komoot
	{
        private int interval = 6 * 60 * 60; // 6 hours in seconds
		private int carID = -1;
		private string username = string.Empty;
		private string password = string.Empty;
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
			GetTours();
			ParseTours();
		}

        private void ParseTours()
        {
            
        }

        private void GetTours()
        {
            
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
						Tools.DebugLog($"Komoot_{{carID}} login result: {resultContent}");
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

