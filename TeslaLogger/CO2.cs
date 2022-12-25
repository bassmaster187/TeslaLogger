using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;


namespace TeslaLogger
{
    public class CO2
    {
        public void GetData()
        {
            DateTime dateTime = new DateTime(2022, 12, 21, 22, 00, 00);

            for (int i = 0; i < 1; i++)
            {
                DateTime dt = dateTime.AddHours(i);
                GetData("ro", dt);
            }
        }

        public int GetData(string country, DateTime dateTime)
        {
            string content = "";

            int wi = GetWeekOfYear(dateTime);
            string w = wi.ToString("D2");
            int year = dateTime.Year;

            string filename = $"week_{year}_{w}.json";
            string path = $"EngergyChartData/{country}/{filename}";

            if (File.Exists(path))
                content = File.ReadAllText(path);
            else
                content = GetEnergyChartData(country, filename);

            dynamic j = JsonConvert.DeserializeObject(content);

            Newtonsoft.Json.Linq.JArray unixtimes = j[0]["xAxisValues"];

            long unixTimestamp = (long)(dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            unixTimestamp *= 1000;

            int ix = 0;
            for (int i = 0; i < unixtimes.Count; i++)
            {
                dynamic t = unixtimes[i];   
                if (t >= unixTimestamp)
                {
                    ix = i;
                    break;
                }
            }

            System.Diagnostics.Debug.WriteLine("Date:" + dateTime + " ix:" + ix);

            double co2count = 0;
            double co2sum = 0;

            foreach (dynamic d in j)
            {
                string name = d["name"]["en"];
                string namede = d["name"]["de"];
                if (name.Contains("forecast") || name.Contains("consumption") || name.Contains("planned") 
                    || name == "Residual load" || name == "Renewable share of generation" || name == "Renewable share of load" || name == "Import Balance" || name == "Load")
                    continue;

                dynamic data = d["data"];

                if (data[ix].Type == Newtonsoft.Json.Linq.JTokenType.Null)
                    continue;

                double wert = data[ix];
                double co2factor = 0;

                switch (name)
                {
                    case "Nuclear": co2factor = 12; break;
                    case "Hydro Run-of-River": co2factor = 11; break;
                    case "Biomass": co2factor = 230; break;
                    case "Fossil brown coal / lignite": co2factor = 1150; break;
                    case "Fossil hard coal": co2factor = 798; break;
                    case "Fossil oil": co2factor = 1125; break;
                    case "Fossil gas": co2factor = 661; break;
                    case "Geothermal": co2factor = 38; break;
                    case "Hydro water reservoir": co2factor = 24; break;
                    case "Hydro pumped storage": co2factor = 0; break;
                    case "Others": co2factor = 700; break;
                    case "Waste": co2factor = 700; break;
                    case "Wind offshore": co2factor = 13; break;
                    case "Wind onshore": co2factor = 13; break;
                    case "Solar": co2factor = 35; break;
                }

                if (co2factor > 0)
                {
                    co2sum += co2factor * wert;
                    co2count += wert;
                }

                System.Diagnostics.Debug.WriteLine(String.Format("{0,-28}", name) + ": "+ wert + "MW "+ co2factor + " CO2 g/kWh");
            }

            GetImport(country, dateTime, ref co2sum, ref co2count);

            double avgCO2 = co2sum / co2count;
            System.Diagnostics.Debug.WriteLine("CO2 AVG: " + avgCO2+ "\r\n\r\n");

            return (int)Math.Round(avgCO2,0);
        }

        private void GetImport(string country, DateTime dateTime, ref double co2sum, ref double co2count)
        {
            string content = "";

            int wi = GetWeekOfYear(dateTime);
            string w = wi.ToString("D2");
            int year = dateTime.Year;

            string filename = $"week_cbpf_saldo_{year}_{w}.json";
            string path = $"EngergyChartData/{country}/{filename}";

            if (File.Exists(path))
                content = File.ReadAllText(path);
            else
                content = GetEnergyChartData(country, filename);

            dynamic j = JsonConvert.DeserializeObject(content);

            Newtonsoft.Json.Linq.JArray unixtimes = j[0]["xAxisValues"];

            long unixTimestamp = (long)(dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            unixTimestamp *= 1000;

            int ix = 0;
            for (int i = 0; i < unixtimes.Count; i++)
            {
                dynamic t = unixtimes[i];
                if (t >= unixTimestamp)
                {
                    ix = i;
                    break;
                }
            }

            // System.Diagnostics.Debug.WriteLine("Date:" + dateTime + " ix:" + ix);

            foreach (dynamic d in j)
            {
                string name = d["name"][0]["en"];
                string namede = d["name"][0]["de"];
                if (name== "sum")
                    continue;

                dynamic data = d["data"];

                if (data[ix].Type == Newtonsoft.Json.Linq.JTokenType.Null)
                    continue;

                double wert = data[ix];

                if (wert < 0) // export not relevant for co2 calculation
                    continue;

                wert *= 1000;

                double co2factor = 0;

                // to prevent cycle loops, we will use avarage values of whole year 2021 from electricitymaps.com
                // for instance a cycle loop: germany export to France, France export to Swizerland, Swizerland export to Germany

                switch (name)
                {
                    case "Austria": co2factor = 230; break;
                    case "Belgium": co2factor = 185; break;
                    case "Bosnia-Herzegovina": co2factor = 525; break;
                    case "Bulgaria": co2factor = 477; break;
                    case "Croatia": co2factor = 307; break; // maybe wrong
                    case "Czech Republic": co2factor = 536; break;
                    case "Denmark": co2factor = 218; break;
                    case "France": co2factor = 78; break;
                    case "Germany": co2factor = 463; break;
                    case "Greece": co2factor = 437; break;
                    case "Hungary": co2factor = 312; break;
                    case "Italy": co2factor = 351; break;
                    case "Luxembourg": co2factor = 300; break; // maybe wrong
                    case "Malta": co2factor = 351; break; // maybe wrong
                    case "Moldova": co2factor = 600; break; // maybe wrong
                    case "Montenegro": co2factor = 398; break;
                    case "Netherlands": co2factor = 453; break;
                    case "Norway": co2factor = 30; break;
                    case "Poland": co2factor = 859; break;
                    case "Portugal": co2factor = 219; break;
                    case "Serbia": co2factor = 537; break;
                    case "Slovenia": co2factor = 275; break;
                    case "Slovak Republic": co2factor = 331; break;
                    case "Spain": co2factor = 184; break;
                    case "Sweden": co2factor = 44; break;
                    case "Switzerland": co2factor = 44; break;
                    case "Ukraine": co2factor = 190; break; // maybe wrong
                    case "United Kingdom": co2factor = 255; break; 


                    default: 
                        System.Diagnostics.Debug.WriteLine("Country not handled: " + name); 
                        break;
                }

                if (co2factor > 0)
                {
                    co2sum += co2factor * wert;
                    co2count += wert;
                }

                System.Diagnostics.Debug.WriteLine(String.Format("{0,-28}", name) + ": " + wert + "MW " + co2factor + "co2 g/kWh");
            }

        }

        public string GetEnergyChartData(string country, string filename)
        {
            string resultContent = "";

            using (WebClient client = new WebClient())
            {
                // week_2022_51.json

                DateTime start = DateTime.UtcNow;
                client.Headers["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                client.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36";
                string url = $"https://www.energy-charts.info/charts/power/data/{country}/{filename}";
                try
                {
                    resultContent = client.DownloadString(url);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("URL: " + url);
                    throw;
                }

                if (resultContent.Contains("Nuclear"))
                {
                    if (!Directory.Exists("EngergyChartData"))
                        Directory.CreateDirectory("EngergyChartData");

                    if (!Directory.Exists("EngergyChartData/" + country))
                        Directory.CreateDirectory("EngergyChartData/"+ country);

                    string path = $"EngergyChartData/{country}/{filename}";

                    if (File.Exists(path))
                        File.Delete(path);

                    File.WriteAllText(path, resultContent);
                }
            }

            return resultContent;
        }

        public static int GetWeekOfYear(DateTime dt)
        {
            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("de-DE");
            int w = ci.Calendar.GetWeekOfYear(dt, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            return w;
        }
    }
}
