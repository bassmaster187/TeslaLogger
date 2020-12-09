using System;
using System.Collections.Generic;
using System.IO;
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

        public string name;
        public string rawName;
        public double lat;
        public double lng;
        public int radius;
        public Dictionary<SpecialFlags, string> specialFlags;
        private bool isHome = false;
        private bool isWork = false;

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

        public bool IsCharger { get; set; } = false;
        public bool NoSleep { get; set; } = false;

        public Address(string name, double lat, double lng, int radius)
        {
            this.name = name;
            rawName = name;
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

    public class AddressByLatLng : IComparer<Address>
    {

        public int Compare(Address x, Address y)
        {
            if (x.lat < y.lat)
            {
                return -1;
            }
            else if (x.lat > y.lat)
            {
                return 1;
            }
            else
            {
                if (x.lng < y.lng)
                {
                    return -1;
                }
                else if (x.lng > y.lng)
                {
                    return 1;
                }
            }
            return 0;
        }
    }

    public class Geofence
    {
        private static Geofence _geofence = null;

        public static Geofence GetInstance()
        {
            if (_geofence == null)
            {
                _geofence = new Geofence(ApplicationSettings.Default.RacingMode);
            }
            return _geofence;
        }

        internal SortedSet<Address> geofenceList = new SortedSet<Address>(new AddressByLatLng());
        internal SortedSet<Address> geofencePrivateList = new SortedSet<Address>(new AddressByLatLng());
        private FileSystemWatcher fsw;

        public bool RacingMode = false;
        private bool _RacingMode = false;

        private static int FSWCounter = 0;

        internal Geofence(bool RacingMode)
        {
            _RacingMode = RacingMode;
            Logfile.Log("Geofence initialized");
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
            if (File.Exists(FileManager.GetFilePath(TLFilename.GeofenceRacingFilename)) && _RacingMode)
            {
                ReadGeofenceFile(geofenceList, FileManager.GetFilePath(TLFilename.GeofenceRacingFilename));
                RacingMode = true;

                Logfile.Log("*** RACING MODE ***");
            }
            else
            {
                RacingMode = false;
                ReadGeofenceFile(geofenceList, FileManager.GetFilePath(TLFilename.GeofenceFilename));
                Logfile.Log("Geofence: addresses inserted from geofence.csv: " + geofenceList.Count);
                if (!File.Exists(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename)))
                {
                    Logfile.Log("Create: " + FileManager.GetFilePath(TLFilename.GeofencePrivateFilename));
                    File.AppendAllText(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename), "");
                }

                UpdateTeslalogger.Chmod(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename), 666);
                ReadGeofenceFile(geofencePrivateList, FileManager.GetFilePath(TLFilename.GeofencePrivateFilename));
            }

            Logfile.Log("Geofence: addresses inserted from geofence-private.csv: " + geofencePrivateList.Count);
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

        private static int ReadGeofenceFile(SortedSet<Address> list, string filename)
        {
            list.Clear();
            filename = filename.Replace(@"Debug\", "");
            int replaceCount = 0;
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
                                ParseSpecialFlags(addr, flags);
                            }
                            if (filename.Equals(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename)))
                            {
                                Logfile.Log("GeofencePrivate: Address inserted: " + args[0]);
                            }

                            if (addr.name.StartsWith("Supercharger-V3 ") || addr.name.StartsWith("Ionity "))
                            {
                                addr.name = "\u26A1\u26A1\u26A1 " + addr.name;
                            }
                            else if (addr.name.StartsWith("Supercharger "))
                            {
                                addr.name = "\u26A1\u26A1 " + addr.name;
                            }
                            else if (addr.name.StartsWith("Urbancharger "))
                            {
                                addr.name = "\u26A1 " + addr.name;
                            }

                            list.Add(addr);
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
                    _addr.name = "\uD83D\uDC40 " + _addr.name;
                }
                else if (flag.Equals("home"))
                {
                    _addr.IsHome = true;
                    _addr.name = "\uD83C\uDFE0 " + _addr.name;
                }
                else if (flag.Equals("work"))
                {
                    _addr.IsWork = true;
                    _addr.name = "\uD83D\uDCBC " + _addr.name;
                }
                else if (flag.Equals("nosleep"))
                {
                    _addr.NoSleep = true;
                    _addr.name = "\u2615 " + _addr.name;
                }
                else if (flag.Equals("charger"))
                {
                    _addr.IsCharger = true;
                    _addr.name = "\uD83D\uDD0C " + _addr.name;
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
                    SpecialFlag_CCP(_addr);
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

        private static void SpecialFlag_CCP(Address _addr)
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

        public Address GetPOI(double lat, double lng, bool logDistance = true, string brand = null, int max_power = 0)
        {
            Address ret = null;
            double retDistance = 0;
            int found = 0;

            // look for POI in geofence-private first
            lock (geofencePrivateList)
            {
                LookupPOIinList(geofencePrivateList, lat, lng, logDistance, brand, max_power, ref ret, ref retDistance, ref found);
            }
            if (ret == null)
            {
                lock (geofenceList)
                {
                    LookupPOIinList(geofenceList, lat, lng, logDistance, brand, max_power, ref ret, ref retDistance, ref found);
                }
            }

            return ret;
        }

        private void LookupPOIinList(SortedSet<Address> list, double lat, double lng, bool logDistance, string brand, int max_power, ref Address ret, ref double retDistance, ref int found)
        {
            double range = 0.2; // apprx 10km

            foreach (Address p in list)
            {
                if (p.lat - range > lat)
                {
                    break;
                }

                if ((p.lat - range) < lat &&
                    lat < (p.lat + range) &&
                    (p.lng - range) < lng &&
                    lng < (p.lng + range))
                {
                    double distance = GetDistance(lng, lat, p.lng, p.lat);
                    if (p.radius > distance)
                    {
                        if (brand == "Tesla")
                        {
                            if (!p.name.Contains("Tesla") && !p.name.Contains("Supercharger"))
                            {
                                continue;
                            }

                            if (max_power > 150 && !p.name.Contains("V3"))
                            {
                                continue;
                            }
                        }

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

        public static double GetDistance(double longitude, double latitude, double otherLongitude, double otherLatitude)
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
