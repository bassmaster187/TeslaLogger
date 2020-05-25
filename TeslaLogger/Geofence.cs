using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TeslaLogger
{
    public class Address
    {
        public Address(string name, double lat, double lng, int radius)
        {
            this.name = name;
            this.lat = lat;
            this.lng = lng;
            this.radius = radius;
        }

        public string name;
        public double lat;
        public double lng;
        public int radius;
    }

    public class Geofence
    {
        private List<Address> sortedList;
        private System.IO.FileSystemWatcher fsw;

        public bool RacingMode = false;

        private static int FSWCounter = 0;

        public enum SpecialFlags
        {
            OpenChargePort
        }

        private static Dictionary<string, HashSet<SpecialFlags>> specialFlags = new Dictionary<string, HashSet<SpecialFlags>>();

        public Geofence()
        {
            Init();
            
            if (fsw == null)
            {
                fsw = new System.IO.FileSystemWatcher(FileManager.GetExecutingPath(), "*.csv");
                FSWCounter++;
                if (FSWCounter > 1) 
                {
                    Logfile.Log("ERROR: more than one FileSystemWatcher created!");
                }
                fsw.NotifyFilter = System.IO.NotifyFilters.LastWrite;
                fsw.Changed += Fsw_Changed;
                // fsw.Created += Fsw_Changed;
                // fsw.Renamed += Fsw_Changed;
                fsw.EnableRaisingEvents = true;
            }
        }

        private void Init()
        {
            List<Address> list = new List<Address>();

            if (System.IO.File.Exists(FileManager.GetFilePath(TLFilename.GeofenceRacingFilename)) && ApplicationSettings.Default.RacingMode)
            {
                ReadGeofenceFile(list, FileManager.GetFilePath(TLFilename.GeofenceRacingFilename));
                RacingMode = true;

                Logfile.Log("*** RACING MODE ***");
            }
            else
            {
                RacingMode = false;
                ReadGeofenceFile(list, FileManager.GetFilePath(TLFilename.GeofenceFilename));
                if (!System.IO.File.Exists(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename)))
                {
                    Logfile.Log("Create: " + FileManager.GetFilePath(TLFilename.GeofencePrivateFilename));
                    System.IO.File.AppendAllText(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename), "");
                }

                UpdateTeslalogger.Chmod(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename), 666);
                ReadGeofenceFile(list, FileManager.GetFilePath(TLFilename.GeofencePrivateFilename));
            }
            
            Logfile.Log("Addresses inserted: " + list.Count);

            sortedList = list.OrderBy(o => o.lat).ToList();
        }

        private void Fsw_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            try
            {
                Logfile.Log($"FileSystemWatcher");

                fsw.EnableRaisingEvents = false;
                
                DateTime dt = System.IO.File.GetLastWriteTime(e.FullPath);
                TimeSpan ts = DateTime.Now - dt;

                System.Threading.Thread.Sleep(5000);

                if (ts.TotalSeconds > 5)
                {
                    return;
                }

                Logfile.Log($"CSV File changed: {e.Name} at {dt}");
                
                Init();

                Task.Factory.StartNew(() => WebHelper.UpdateAllPOIAddresses());
            }
            finally
            {
                fsw.EnableRaisingEvents = true;
            }
        }

        private static void ReadGeofenceFile(List<Address> list, string filename)
        {
            filename = filename.Replace(@"Debug\", "");
            if (System.IO.File.Exists(filename))
            {
                Logfile.Log("Read Geofence File: " + filename);
                string line;
                using (System.IO.StreamReader file = new System.IO.StreamReader(filename))
                {
                    while ((line = file.ReadLine()) != null)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(line))
                            {
                                continue;
                            }

                            int radius = 50;

                            string[] args = line.Split(',');

                            if (args.Length > 3)
                            {
                                int.TryParse(args[3], out radius);
                            }

                            if (args.Length > 4)
                            {
                                string flags = args[4];
                                Logfile.Log(args[0].Trim() + ": special flags found: " + flags);
                                ParseSpecialFlags(args[0].Trim(), flags);
                            }

                            list.Add(new Address(args[0].Trim(),
                                double.Parse(args[1].Trim(), Tools.ciEnUS.NumberFormat),
                                double.Parse(args[2].Trim(), Tools.ciEnUS.NumberFormat),
                                radius));

                            if (!filename.Contains("geofence.csv"))
                            {
                                Logfile.Log("Address inserted: " + args[0]);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logfile.ExceptionWriter(ex, line);
                        }
                    }
                }
            }
            else
            {
                Logfile.Log("ReadGeofenceFile FileNotFound: " + filename);
            }
        }

        private static void ParseSpecialFlags(string _locationname, string _flags)
        {
            if (_flags.Contains("+ocp"))
            {
                if (!specialFlags.ContainsKey(_locationname))
                {
                    specialFlags.Add(_locationname, new HashSet<SpecialFlags>());
                }
                specialFlags[_locationname].Add(SpecialFlags.OpenChargePort);
            }
        }

        public static HashSet<SpecialFlags> GetSpecialFlagsForLocationName(string _locationname)
        {
            Logfile.Log("GetSpecialFlagsForLocationName(" + _locationname + ")");
            return specialFlags.ContainsKey(_locationname) ? specialFlags[_locationname] : new HashSet<SpecialFlags>();
        }

        public Address GetPOI(double lat, double lng, bool logDistance = true)
        {
            Address ret = null;
            double retDistance = 0;
            int found = 0;

            lock (sortedList)
            {
                double range = 0.2; // apprx 10km

                foreach (Address p in sortedList)
                {
                    
                    if (p.lat - range > lat)
                    {
                        return ret; // da die liste sortiert ist, kann nichts mehr kommen
                    }

                    if ((p.lat - range) < lat &&
                        lat < (p.lat + range) &&
                        (p.lng - range) < lng &&
                        lng < (p.lng + range))
                    {
                        double distance = GetDistance(lng, lat, p.lng, p.lat);
                        if (p.radius > distance)
                        {
                            found++;
                            if (logDistance)
                            {
                                Logfile.Log($"Distance: {distance} - Radius: {p.radius} - {p.name}");
                            }

                            if (ret == null)
                            {
                                ret = p;
                                retDistance = distance; 
                            }
                            else
                            {
                                if (distance < retDistance)
                                {
                                    ret = p;
                                    retDistance = distance;
                                }
                            }
                        }
                    }
                }
            }

            return ret;
        }

        public double GetDistance(double longitude, double latitude, double otherLongitude, double otherLatitude)
        {
            double d1 = latitude * (Math.PI / 180.0);
            double num1 = longitude * (Math.PI / 180.0);
            double d2 = otherLatitude * (Math.PI / 180.0);
            double num2 = (otherLongitude * (Math.PI / 180.0)) - num1;
            double d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + (Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0));

            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }
    }
}
