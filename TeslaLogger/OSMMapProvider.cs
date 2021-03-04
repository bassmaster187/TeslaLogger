using System;
using System.Data;

namespace TeslaLogger
{
    public class OSMMapProvider : StaticMapProvider
    {
        protected OSMMapProvider()
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
    }
}
