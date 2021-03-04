using System;
using System.Data;
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
                        MethodInfo methodInfo = type.GetMethod("GetInstance");
                        if (methodInfo != null)
                        {
                            object classInstance = Activator.CreateInstance(type);
                            _StaticMapProvider = (StaticMapProvider)methodInfo.Invoke(classInstance, null);
                            if (_StaticMapProvider != null)
                            {
                                Logfile.Log("selected MapProvider: " + _StaticMapProvider.GetType());
                            }
                        }
                    }
                }
                if (_StaticMapProvider == null)
                {
                    _StaticMapProvider = OSMMapProvider.GetSingleton(); // default
                    Logfile.Log("selected MapProvider (default): " + _StaticMapProvider.GetType());
                }
            }
            return _StaticMapProvider;
        }

        internal abstract StaticMapProvider GetInstance();
        public abstract void CreateChargingMap(double lat, double lng, int width, int height, MapMode mapmode, string filename);
        public abstract void CreateParkingMap(double lat, double lng, int width, int height, MapMode mapmode, string filename);
        public abstract void CreateTripMap(DataTable coords, int width, int height, MapMode mapmode, string filename);
    }
}
