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
            DateTime dateTime = new DateTime(2022, 12, 21, 00, 00, 00);

            for (int i = 0; i < 24; i++)
            {
                DateTime dt = dateTime.AddHours(i);
                GetData(dt);
            }
        }

        double GetData(DateTime dateTime)
        {
            string content = "";

            string filename = "week_2022_51.json";
            string country = "ch";
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
                if (name.Contains("forecast") || name.Contains("consumption") || name.Contains("planned"))
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

                System.Diagnostics.Debug.WriteLine(name + ": "+ wert + "MW "+ co2factor + "co2 g/kWh");
            }

            double avgCO2 = co2sum / co2count;
            System.Diagnostics.Debug.WriteLine("CO2 AVG: " + avgCO2+ "\r\n\r\n");

            return avgCO2;
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
                resultContent = client.DownloadString($"https://www.energy-charts.info/charts/power/data/{country}/{filename}");

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
    }
}
