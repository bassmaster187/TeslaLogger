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
            EnableSentryMode,
            SetChargeLimit,
            ClimateOff,
            CopyChargePrice
        }

        public enum GeofenceSource
        {
            Geofence,
            GeofencePrivate
        }

        public string name;
        public double lat;
        public double lng;
        public int radius;
        public Dictionary<SpecialFlags, string> specialFlags;
        private bool isHome = false;
        private bool isWork = false;
        internal GeofenceSource geofenceSource;

        public bool IsHome
        {
            get => isHome; set
            {
                isHome = value;
                if (value)
                {
                    isWork = false;
                }
            }
        }
        public bool IsWork
        {
            get => isWork; set
            {
                isWork = value;
                if (value)
                {
                    isHome = false;
                }
            }
        }

        public Address(string name, double lat, double lng, int radius, GeofenceSource source = GeofenceSource.Geofence)
        {
            this.name = name;
            this.lat = lat;
            this.lng = lng;
            this.radius = radius;
            geofenceSource = source;
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
        internal List<Address> sortedList;
        private FileSystemWatcher fsw;

        public bool RacingMode = false;
        private bool _RacingMode = false;

        private static int FSWCounter = 0;

        public Geofence(bool RacingMode)
        {
            _RacingMode = RacingMode;
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

        internal void Init()
        {
            List<Address> list = new List<Address>();
            int replaceCount = 0;

            if (File.Exists(FileManager.GetFilePath(TLFilename.GeofenceRacingFilename)) && _RacingMode)
            {
                ReadGeofenceFile(list, FileManager.GetFilePath(TLFilename.GeofenceRacingFilename));
                RacingMode = true;

                Logfile.Log("*** RACING MODE ***");
            }
            else
            {
                RacingMode = false;
                ReadGeofenceFile(list, FileManager.GetFilePath(TLFilename.GeofenceFilename));
                Logfile.Log("Geofence: addresses inserted from geofence.csv: " + list.Where(a => a.geofenceSource == Address.GeofenceSource.Geofence).Count());
                if (!File.Exists(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename)))
                {
                    Logfile.Log("Create: " + FileManager.GetFilePath(TLFilename.GeofencePrivateFilename));
                    File.AppendAllText(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename), "");
                }

                UpdateTeslalogger.Chmod(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename), 666);
                replaceCount += ReadGeofenceFile(list, FileManager.GetFilePath(TLFilename.GeofencePrivateFilename), true);
            }

            Logfile.Log("Geofence: addresses inserted from geofence-private.csv: " + list.Where(a => a.geofenceSource == Address.GeofenceSource.GeofencePrivate).Count()); ;
            Logfile.Log($"Geofence: addresses replaced by geofence-private.csv: {replaceCount}");

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

        private static int ReadGeofenceFile(List<Address> list, string filename, bool replaceExistiongPOIs = false)
        {
            filename = filename.Replace(@"Debug\", "");
            int replaceCount = 0;
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

                            string[] args = Regex.Split(line, ",");

                            if (args.Length > 3 && args[3] != null && args[3].Length > 0)
                            {
                                int.TryParse(args[3], out radius);
                            }

                            Address addr = new Address(args[0].Trim(),
                                double.Parse(args[1].Trim(), Tools.ciEnUS.NumberFormat),
                                double.Parse(args[2].Trim(), Tools.ciEnUS.NumberFormat),
                                radius);

                            if (args.Length > 4 && args[4] != null)
                            {
                                string flags = args[4];
                                Tools.DebugLog(args[0].Trim() + ": special flags found: " + flags);
                                ParseSpecialFlags(addr, flags);
                            }
                            if (filename.Equals(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename)))
                            {
                                addr.geofenceSource = Address.GeofenceSource.GeofencePrivate;
                                Logfile.Log("GeofencePrivate: Address inserted: " + args[0]);
                            }

                            localList.Add(addr);
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
                    foreach (Address addr in list)
                    {
                        bool keepAddr = true;
                        foreach (string localName in uniqueNameList)
                        {
                            if (addr != null && addr.name != null && localName != null && localName.Equals(addr.name))
                            {
                                Logfile.Log("replace " + addr.name + " with POI(s) from " + filename);
                                replaceCount++;
                                keepAddr = false;
                                break;
                            }
                        }
                        if (keepAddr)
                        {
                            localList.Add(addr);
                        }
                    }
                    // all entries from geofence that are not overwritten by geofence-private are now copied to locallist
                    list.Clear();
                }
                // copy locallist to list
                list.AddRange(localList);
            }
            else
            {
                Logfile.Log("ReadGeofenceFile FileNotFound: " + filename);
            }
            return replaceCount;
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
                else if (flag.Equals("home"))
                {
                    _addr.IsHome = true;
                    _addr.name = "🏠 " + _addr.name;
                }
                else if (flag.Equals("work"))
                {
                    _addr.IsWork = true;
                    _addr.name = "💼 " + _addr.name;
                }
                else if (flag.StartsWith("scl"))
                {
                    SpecialFlag_SCL(_addr, flag);
                }
                else if (flag.StartsWith("cof"))
                {
                    SpecialFlag_COF(_addr, flag);
                }
                else if (flag.Equals("ccp"))
                {
                    SpecialFlag_CCP(_addr, flag);
                }
            }
        }

        private static void SpecialFlag_ESM(Address _addr, string _flag)
        {
            string pattern = "esm:([PRND]+)->([PRND]+)";
            Match m = Regex.Match(_flag, pattern);
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1)
            {
                _addr.specialFlags.Add(Address.SpecialFlags.EnableSentryMode, m.Groups[1].Captures[0].ToString() + "->" + m.Groups[2].Captures[0].ToString());
            }
            else
            {
                // default
                _addr.specialFlags.Add(Address.SpecialFlags.EnableSentryMode, "RND->P");
            }
        }

        private static void SpecialFlag_COF(Address _addr, string _flag)
        {
            string pattern = "cof:([PRND]+)->([PRND]+)";
            Match m = Regex.Match(_flag, pattern);
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1)
            {
                _addr.specialFlags.Add(Address.SpecialFlags.ClimateOff, m.Groups[1].Captures[0].ToString() + "->" + m.Groups[2].Captures[0].ToString());
            }
            else
            {
                // default
                _addr.specialFlags.Add(Address.SpecialFlags.ClimateOff, "RND->P");
            }
        }

        private static void SpecialFlag_CCP(Address _addr, string _flag)
        {
            _addr.specialFlags.Add(Address.SpecialFlags.CopyChargePrice, "");
        }

        private static void SpecialFlag_SCL(Address _addr, string _flag)
        {
            string pattern = "scl:([0-9]+)";
            Match m = Regex.Match(_flag, pattern);
            if (m.Success && m.Groups.Count == 2 && m.Groups[1].Captures.Count == 1)
            {
                _addr.specialFlags.Add(Address.SpecialFlags.SetChargeLimit, m.Groups[1].Captures[0].ToString());
            }
            else
            {
                // default
                _addr.specialFlags.Add(Address.SpecialFlags.SetChargeLimit, "80");
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
