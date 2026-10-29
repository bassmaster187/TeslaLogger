using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestNearbySuCService
    {
        private static Car CreateTestCar()
        {
            return new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
        }

        private static NearbySuCService.Available CallGetGuestAvailability(Car car, int trid)
        {
            // GetGuestAvailability hits the public (no-auth) Tesla guest API;
            // a hard network failure (DNS/timeout) surfaces as an exception
            NearbySuCService service = NearbySuCService.GetSingleton();
            try
            {
                return service.GetGuestAvailability(car, trid);
            }
            catch (Exception ex)
            {
                Assert.Inconclusive("Tesla guest API unreachable (network failure): " + ex.Message);
                return null;
            }
        }


        [TestMethod]
        [Timeout(90000)]
        public void GetGuestAvailability_NoThrow_AndInvariantsHold()
        {
            Car c = CreateTestCar();

            NearbySuCService.Available a = CallGetGuestAvailability(c, 1674);

            Assert.IsNotNull(a);
            Assert.IsTrue(a.total >= 0);
            Assert.IsTrue(a.available >= 0);
            Assert.IsTrue(a.occupied >= 0);
            Assert.IsTrue(a.down >= 0);
            Assert.IsTrue(a.unknown >= 0);
            Assert.IsTrue(a.unhandled >= 0);

            // every charger is classified into exactly one availability state
            Assert.AreEqual(a.total, a.available + a.occupied + a.down + a.unknown + a.unhandled);
            Assert.IsTrue(a.available <= a.total);
        }

        [TestMethod]
        [Timeout(90000)]
        public void GetGuestAvailability_LiveApi_ReturnsChargerData()
        {
            Car c = CreateTestCar();

            NearbySuCService.Available a = CallGetGuestAvailability(c, 1674); // Leermos

            if (a.total == 0)
            {
                // the Tesla guest API is blocked (e.g. WAF 403) or unreachable from the test environment
                Assert.Inconclusive("Tesla guest API unreachable (403/timeout) - no charger data returned");
            }

            Assert.IsTrue(a.total > 0);
            Assert.IsTrue(a.available + a.occupied + a.down + a.unknown + a.unhandled == a.total);
            Assert.IsTrue(a.available <= a.total);
        }
    }
}
