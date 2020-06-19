using NUnit.Framework;
using TeslaLogger;

namespace TLNUnit
{
    [TestFixture()]
    public class Test
    {
        [Test()]
        public void GeofenceInitTest()
        {
            Geofence geofence = new Geofence(false);
            Assert.NotNull(geofence);
        }
    }
}
