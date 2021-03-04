using System;
using System.Data;

namespace TeslaLogger
{
    public class MapQuestMapProvider : StaticMapProvider
    {
        protected MapQuestMapProvider()
        {
        }

        public override void CreateChargingMap(double lat, double lng, int width, int height, MapMode mapmode, string filename)
        {
            throw new NotImplementedException();
        }

        public override void CreateParkingMap(double lat, double lng, int width, int height, MapMode mapmode, string filename)
        {
            throw new NotImplementedException();
        }

        public override void CreateTripMap(DataTable coords, int width, int height, MapMode mapmode, string filename)
        {
            throw new NotImplementedException();
        }

        internal override StaticMapProvider GetInstance()
        {
            if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
            {
                return null;
            }
            return new MapQuestMapProvider();
        }
    }
}
