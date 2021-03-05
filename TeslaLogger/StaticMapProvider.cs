using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace TeslaLogger
{
    public abstract class StaticMapProvider
    {

        public enum MapMode
        {
            Regular,
            Dark
        }

        public enum MapSpecial
        {
            None,
            TripHeatMap,
            TripSpeedMap,
            TripPowerMap
        }

        public enum MapType
        {
            Trip,
            Charge,
            Park
        }

        public enum MapIcon
        {
            Start,
            End,
            Park,
            Charge
        }

        private static StaticMapProvider _StaticMapProvider = null;

        protected StaticMapProvider()
        {
        }

        public static StaticMapProvider GetSingleton()
        {
            if (_StaticMapProvider == null)
            {
                foreach (Type type in Assembly.GetAssembly(typeof(StaticMapProvider)).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(StaticMapProvider))))
                {
                    Logfile.Log("available MapProvider: " + type);
                    if (type.ToString().Contains(Tools.GetMapProvider()))
                    {
                        _StaticMapProvider = (StaticMapProvider)Activator.CreateInstance(type);
                    }
                }
                if (_StaticMapProvider == null)
                {
                    _StaticMapProvider = new OSMMapProvider(); // default
                }
            }
            return _StaticMapProvider;
        }

        public abstract void CreateChargingMap(double lat, double lng, int width, int height, MapMode mapmode, MapSpecial special, string filename);
        public abstract void CreateParkingMap(double lat, double lng, int width, int height, MapMode mapmode, MapSpecial special, string filename);
        public abstract void CreateTripMap(DataTable coords, int width, int height, MapMode mapmode, MapSpecial special, string filename);
        public abstract int GetDelayMS();

        public static void SaveImage(Bitmap image, string filename)
        {
            try
            {
                image.Save(filename);
                Logfile.Log("Create File: " + filename);
            }
            catch (Exception ex)
            {
                Tools.DebugLog("Exception", ex);
            }
        }

        protected Tuple<double, double, double, double> DetermineExtent(DataTable coords)
        {
            double min_lat = (double)coords.Compute("Min(lat)", "");
            double min_lng = (double)coords.Compute("Min(lng)", "");
            double max_lat = (double)coords.Compute("Max(lat)", "");
            double max_lng = (double)coords.Compute("Max(lng)", "");
            Tools.DebugLog($"DetermineExtent {min_lat},{min_lng} {max_lat},{max_lng}");
            return new Tuple<double, double, double, double>(min_lat, min_lng, max_lat, max_lng);
        }

    }
}
