using System.Collections.Generic;
using NUnit.Framework;

namespace TeslaLogger
{
    [TestFixture()]
    public class GeofenceTest
    {
        [Test()]
        public void Instantiate()
        {
            Geofence geofence = new Geofence(false);
            Assert.NotNull(geofence);
        }

        [Test()]
        public void ParseGeofenceLine1()
        {
            List<Address> list = new List<Address>();
            Geofence.ParseGeofenceLine("filename", list, "", 50);
        }

        [Test()]
        public void ParseGeofenceLine2()
        {
            List<Address> list = new List<Address>();
            Geofence.ParseGeofenceLine("filename", list, "gdsgdfgdfgfgdggd", 50);
        }
    }
}
