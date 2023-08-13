using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "<Pending>")]
    internal class CaptchaSolver
    {
        private static string API_KEY = CaptchaSolverKey.key;
        static HttpClient client; // defaults to null;
        string captcha_id;
        DateTime start = DateTime.Now;
        readonly Car car;

        public CaptchaSolver(Car c)
        {
            this.car = c;
            if (client == null)
            {
                client = new HttpClient();
            }

        }

        public string Send(string sitekey, string pageurl)
        {
            using (var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("key", API_KEY),
                new KeyValuePair<string, string>("method", "userrecaptcha"),
                new KeyValuePair<string, string>("googlekey", sitekey),
                new KeyValuePair<string, string>("pageurl", pageurl)
            }))
            {
                car.Log("Get Recaptcha (in)");

                car.ExternalLog("2captcha in");

                var response = client.PostAsync(new Uri("http://2captcha.com/in.php"), formContent).Result;
                string result = response.Content.ReadAsStringAsync().Result;

                var r = result.Split('|');
                captcha_id = r[1];

                car.Log("2Captcha in: " + r[0]+ " ID: " + r[1]);
                start = DateTime.Now;

                return captcha_id;
            }
        }

        public string Get()
        {
            for (int x = 1; x < 20; x++)
            {
                car.Log("2Captcha res try #" + x);

                using (var formContent = new FormUrlEncodedContent(new[]
                 {
                    new KeyValuePair<string, string>("key", API_KEY),
                    new KeyValuePair<string, string>("action", "get"),
                    new KeyValuePair<string, string>("id", captcha_id)
                }))
                {

                    var response2 = client.PostAsync(new Uri("http://2captcha.com/res.php"), formContent).Result;
                    string result2 = response2.Content.ReadAsStringAsync().Result;

                    if (result2 == "CAPCHA_NOT_READY")
                    {
                        car.Passwortinfo.Append("&nbsp;&nbsp;Still waiting for Recaptcha solver.<br>");
                        car.Log("CAPCHA_NOT_READY");
                        System.Threading.Thread.Sleep(5000);
                        continue;
                    }

                    string[] ret = result2.Split('|');

                    car.Log("2Captcha res: " + ret[0] + " ID: " + ret[1]);
                    TimeSpan ts = DateTime.Now - start;
                    car.ExternalLog("2captcha res: " + ret[0] + " / " + ts.TotalMilliseconds + "ms");

                    return ret[1];
                }
            }

            return null;
        }

        internal string SearchForSitekey(string content)
        {
            string pattern = "'sitekey'\\s:\\s'(.*?)'";
            Match m = Regex.Match(content, pattern);
            if (m.Success)
            {
                string s = m.Groups[1].Captures[0].ToString();

                car.Log("Recaptcha Sitekey: " + s);

                return s;
            }

            return null;
        }
    }
}
