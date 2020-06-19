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
            Assert.IsEmpty(list);
        }

        [Test()]
        public void ParseGeofenceLine2()
        {
            List<Address> list = new List<Address>();
            Geofence.ParseGeofenceLine("filename", list, "gdsgdfgdfgfgdggd", 50);
            Assert.IsEmpty(list);
        }

        [Test()]
        public void ParseGeofenceLine3()
        {
            List<Address> list = new List<Address>();
            Geofence.ParseGeofenceLine("geofence.csv", list, "Supercharger DE-Herzsprung, 53.067343, 12.533212", 50);
            Assert.IsNotEmpty(list);
            Assert.IsNotNull(list[0]);
            Assert.IsInstanceOf<Address>(list[0]);
            Assert.AreEqual(list[0].name, "Supercharger DE-Herzsprung");
        }

        [Test()]
        public void ParseGeofenceLine4()
        {
            List<Address> list = new List<Address>();
            Geofence.ParseGeofenceLine("geofence.csv", list, "Supercharger DE-Herzsprung, 53.067343, 12.533212,", 50);
            Assert.IsNotEmpty(list);
            Assert.AreEqual(list[0].radius, 50);
        }

        [Test()]
        public void ParseGeofenceLine5()
        {
            List<Address> list = new List<Address>();
            Geofence.ParseGeofenceLine("geofence.csv", list, "Supercharger DE-Herzsprung, 53.067343, 12.533212,,", 50);
            Assert.IsEmpty(list[0].specialFlags);
        }

        [Test()]
        public void ParseGeofenceLine6()
        {
            List<Address> list = new List<Address>();
            Geofence.ParseGeofenceLine("geofence.csv", list, "Supercharger DE-Herzsprung, 53.067343, 12.533212,25", 50);
            Assert.IsNotEmpty(list);
            Assert.AreEqual(list[0].radius, 25);
        }

        [Test()]
        public void ParseGeofenceLine7()
        {
            List<Address> list = new List<Address>();
            Geofence.ParseGeofenceLine("geofence.csv", list, "Supercharger DE-Herzsprung, 53.067343, 12.533212,25,", 50);
            Assert.IsNotEmpty(list);
            Assert.AreEqual(list[0].radius, 25);
        }
    }
}
