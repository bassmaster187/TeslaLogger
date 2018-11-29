using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeslaLogger
{
    public class Address
    {
        public Address(string name, double lat, double lng)
        {
            this.name = name;
            this.lat = lat;
            this.lng = lng;
        }
               
        public string name;
        public double lat;
        public double lng;
    }

    

    class Geofence
    {
        List<Address> sortedList;

        public Geofence()
        {
            List<Address> list = new List<Address>();

            string filename = "geofence.csv";

            if (System.IO.File.Exists(filename))
            {
                string line;
                using (System.IO.StreamReader file = new System.IO.StreamReader(filename))
                {
                    while ((line = file.ReadLine()) != null)
                    {
                        try
                        {
                            if (String.IsNullOrEmpty(line))
                                continue;

                            var args = line.Split(',');
                            list.Add(new Address(args[0].Trim(), 
                                Double.Parse(args[1].Trim(), Tools.ciEnUS.NumberFormat),
                                Double.Parse(args[2].Trim(), Tools.ciEnUS.NumberFormat)));

                            Tools.Log("Address inserted: " + args[0]);
                        }
                        catch (Exception ex)
                        {
                            Tools.ExceptionWriter(ex, line);
                        }
                    }
                }
            }
            else
            {
                Tools.Log("FileNotFound: " + filename);
            }

            Tools.Log("Addresses inserted: " + list.Count);

            sortedList = list.OrderBy(o => o.lat).ToList();
        }

        public Address GetPOI(double lat, double lng)
        {
            double range = 0.0007;

            foreach (var p in sortedList)
            {
                if (p.lat - range > lat)
                    return null; // da die liste sortiert ist, kann nichts mehr kommen

                if ((p.lat - range) < lat && 
                    lat < (p.lat + range) &&
                    (p.lng - range) < lng &&
                    lng < (p.lng + range))
                    return p;
            }

            return null;
        }
    }
}
