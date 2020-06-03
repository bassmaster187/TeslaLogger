using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TeslaLogger
{
    public class Address
    {
        public enum SpecialFlags
        {
            OpenChargePort,
            HighFrequencyLogging,
            EnableSentryMode
        }

        public string name;
        public double lat;
        public double lng;
        public int radius;
        public Dictionary<SpecialFlags, string> specialFlags;

        public Address(string name, double lat, double lng, int radius)
        {
            this.name = name;
            this.lat = lat;
            this.lng = lng;
            this.radius = radius;
            specialFlags = new Dictionary<SpecialFlags, string>();
        }

        public override string ToString()
        {
            string ret = "Address:\nname:"+name+"\nlat:"+lat+"\nlng:"+lng+"\nradius:"+radius;
            foreach (KeyValuePair<SpecialFlags, string> flag in specialFlags)
            {
                ret += "\n" + flag.Key.ToString() + ":" + flag.Value;
            }
            return ret;
        }
    }

    public class Geofence
    {
        private List<Address> sortedList;
        private FileSystemWatcher fsw;

        public bool RacingMode = false;

        private static int FSWCounter = 0;

        public Geofence()
        {
            Init();
            
            if (fsw == null)
            {
                fsw = new FileSystemWatcher(FileManager.GetExecutingPath(), "*.csv");
                FSWCounter++;
                if (FSWCounter > 1) 
                {
                    Logfile.Log("ERROR: more than one FileSystemWatcher created!");
                }
                fsw.NotifyFilter = NotifyFilters.LastWrite;
                fsw.Changed += Fsw_Changed;
                // fsw.Created += Fsw_Changed;
                // fsw.Renamed += Fsw_Changed;
                fsw.EnableRaisingEvents = true;
            }
        }

        private void Init()
        {
            List<Address> list = new List<Address>();

            if (File.Exists(FileManager.GetFilePath(TLFilename.GeofenceRacingFilename)) && ApplicationSettings.Default.RacingMode)
            {
                ReadGeofenceFile(list, FileManager.GetFilePath(TLFilename.GeofenceRacingFilename));
                RacingMode = true;

                Logfile.Log("*** RACING MODE ***");
            }
            else
            {
                RacingMode = false;
                ReadGeofenceFile(list, FileManager.GetFilePath(TLFilename.GeofenceFilename));
                if (!File.Exists(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename)))
                {
                    Logfile.Log("Create: " + FileManager.GetFilePath(TLFilename.GeofencePrivateFilename));
                    File.AppendAllText(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename), "");
                }

                UpdateTeslalogger.Chmod(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename), 666);
                ReadGeofenceFile(list, FileManager.GetFilePath(TLFilename.GeofencePrivateFilename), true);
            }
            
            Logfile.Log("Addresses inserted: " + list.Count);

            sortedList = list.OrderBy(o => o.lat).ToList();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                Logfile.Log("FileSystemWatcher");

                fsw.EnableRaisingEvents = false;
                
                DateTime dt = File.GetLastWriteTime(e.FullPath);
                TimeSpan ts = DateTime.Now - dt;

                Thread.Sleep(5000);

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

        private static void ReadGeofenceFile(List<Address> list, string filename, bool replaceExistiongPOIs = false)
        {
            filename = filename.Replace(@"Debug\", "");
            List<Address> localList = new List<Address>();
            if (File.Exists(filename))
            {
                Logfile.Log("Read Geofence File: " + filename);
                string line;
                using (StreamReader file = new StreamReader(filename))
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

                            Address addr = new Address(args[0].Trim(),
                                double.Parse(args[1].Trim(), Tools.ciEnUS.NumberFormat),
                                double.Parse(args[2].Trim(), Tools.ciEnUS.NumberFormat),
                                radius);

                            if (args.Length > 4)
                            {
                                string flags = args[4];
                                Logfile.Log(args[0].Trim() + ": special flags found: " + flags);
                                ParseSpecialFlags(addr, flags);
                            }

                            localList.Add(addr);

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
                if (replaceExistiongPOIs)
                {
                    HashSet<string> uniqueNameList = new HashSet<string>();
                    foreach (Address addr in localList)
                    {
                        if (addr != null && addr.name != null)
                        {
                            uniqueNameList.Add(addr.name);
                        }
                    }
                    if (uniqueNameList.Count > 0)
                    {
                        foreach (Address addr in list)
                        {
                            bool keepAddr = true;
                            foreach (string localName in uniqueNameList)
                            {
                                if (addr != null && addr.name != null)
                                {
                                    if (localName.Equals(addr.name))
                                    {
                                        Logfile.Log("replace " + addr.name + " with value(s) from " + filename);
                                        keepAddr = false;
                                        break;
                                    }
                                }
                            }
                            if (keepAddr)
                            {
                                localList.Add(addr);
                            }
                        }
                    }
                    list.Clear();
                }
                list.AddRange(localList);
            }
            else
            {
                Logfile.Log("ReadGeofenceFile FileNotFound: " + filename);
            }
        }

        private static void ParseSpecialFlags(Address _addr, string _flags)
        {
            foreach (string flag in _flags.Split('+'))
            {
                if (flag.StartsWith("ocp"))
                {
                    SpecialFlag_OCP(_addr, flag);
                }
                else if (flag.StartsWith("hfl"))
                {
                    SpecialFlag_HFL(_addr, flag);
                }
                else if (flag.StartsWith("esm"))
                {
                    SpecialFlag_ESM(_addr, flag);
                }
            }
        }

        private static void SpecialFlag_ESM(Address _addr, string _flag)
        {
            string pattern = "esm:([PRND]+)->([PRND]+)";
            Match m = Regex.Match(_flag, pattern);
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1)
            {
                _addr.specialFlags.Add(Address.SpecialFlags.EnableSentryMode, m.Groups[0].Captures[1].ToString() + "->" + m.Groups[0].Captures[2].ToString());
            }
            else
            {
                // default
                _addr.specialFlags.Add(Address.SpecialFlags.EnableSentryMode, "RND->P");
            }
        }

        private static void SpecialFlag_HFL(Address _addr, string _flag)
        {
            string pattern = "hfl:([0-9]+)([a-z]{0,1})";
            Match m = Regex.Match(_flag, pattern);
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1)
            {
                _addr.specialFlags.Add(Address.SpecialFlags.HighFrequencyLogging, m.Groups[1].Captures[0].ToString() + m.Groups[2].Captures[0].ToString());
            }
            else
            {
                // default
                _addr.specialFlags.Add(Address.SpecialFlags.HighFrequencyLogging, "100");
            }
        }

        private static void SpecialFlag_OCP(Address _addr, string _flag)
        {
            string pattern = "ocp:([PRND]+)->([PRND]+)";
            Match m = Regex.Match(_flag, pattern);
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1)
            {
                _addr.specialFlags.Add(Address.SpecialFlags.OpenChargePort, m.Groups[1].Captures[0].ToString() + "->" + m.Groups[2].Captures[0].ToString());
            }
            else
            {
                // default
                _addr.specialFlags.Add(Address.SpecialFlags.OpenChargePort, "RND->P");
            }
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
