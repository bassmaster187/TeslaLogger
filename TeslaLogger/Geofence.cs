namespace TeslaLogger
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
        System.IO.FileSystemWatcher fsw;

        public Geofence()
        {
            Init();
            if (fsw == null)
            {
                fsw = new System.IO.FileSystemWatcher(FileManager.GetExecutingPath(), "*.csv");
                fsw.NotifyFilter = System.IO.NotifyFilters.LastWrite;
                fsw.Changed += Fsw_Changed;
                // fsw.Created += Fsw_Changed;
                // fsw.Renamed += Fsw_Changed;
                fsw.EnableRaisingEvents = true;
            }
        }

        void Init()
        {
            List<Address> list = new List<Address>();

            ReadGeofenceFile(list, FileManager.GetFilePath(TLFilename.GeofenceFilename));
            if (!System.IO.File.Exists(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename)))
            {
                Logfile.Log("Create: " + FileManager.GetFilePath(TLFilename.GeofencePrivateFilename));
                System.IO.File.AppendAllText(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename), "");
            }

            UpdateTeslalogger.chmod(FileManager.GetFilePath(TLFilename.GeofencePrivateFilename), 666);
            ReadGeofenceFile(list, FileManager.GetFilePath(TLFilename.GeofencePrivateFilename));

            Logfile.Log("Addresses inserted: " + list.Count);

            sortedList = list.OrderBy(o => o.lat).ToList();
        }

        private void Fsw_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            Logfile.Log("CSV File changed: " + e.Name);
            fsw.EnableRaisingEvents = false;

            System.Threading.Thread.Sleep(5000);
            Init();

            fsw.EnableRaisingEvents = true;
        }

        private static void ReadGeofenceFile(List<Address> list, string filename)
        {
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
                            if (String.IsNullOrEmpty(line))
                                continue;

                            var args = line.Split(',');
                            list.Add(new Address(args[0].Trim(),
                                Double.Parse(args[1].Trim(), Tools.ciEnUS.NumberFormat),
                                Double.Parse(args[2].Trim(), Tools.ciEnUS.NumberFormat)));

                            if (!filename.Contains("geofence.csv"))
                                Logfile.Log("Address inserted: " + args[0]);
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
                Logfile.Log("FileNotFound: " + filename);
            }
        }

        public Address GetPOI(double lat, double lng)
        {
            lock (sortedList)
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
            }

            return null;
        }
    }
}
