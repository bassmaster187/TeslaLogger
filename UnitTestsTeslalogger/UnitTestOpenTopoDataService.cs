using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestOpenTopoDataService
    {
        [TestMethod]
        public void SomeLocations()
        {
            Tuple<long, double, double>[] items = new Tuple<long, double, double>[8];
            items[0] = new Tuple<long, double, double>(0, 48.456708, 10.029897); 
            items[1] = new Tuple<long, double, double>(1, 48.4053267, 9.9547932);
            items[2] = new Tuple<long, double, double>(2, 48.96092, 9.43113);
            items[3] = new Tuple<long, double, double>(3, 40.773667, -74.039867);
            items[4] = new Tuple<long, double, double>(4, 35.677121, 139.751033);
            items[5] = new Tuple<long, double, double>(5, 0, 0);
            items[6] = new Tuple<long, double, double>(6, 31.490833, 35.479722);
            items[7] = new Tuple<long, double, double>(7, 47.42122, 10.9863);

            int count = 0;

            OpenTopoDataService.RequestLocations(items, (long id, double elevation ) => {
                count ++;
                if (id == 0)
                    Assert.AreEqual(568, elevation);
                else if (id == 1)
                    Assert.AreEqual(503, elevation);
                else if (id == 2)
                    Assert.AreEqual(272, elevation);
                else if (id == 3)
                    Assert.AreEqual(22, elevation);
                else if (id == 4)
                    Assert.AreEqual(29, elevation);
                else if (id == 5)
                    Assert.AreEqual(-4935, elevation); // LOL
                else if (id == 6)
                    Assert.AreEqual(-415, elevation);
                else if (id == 7)
                    Assert.AreEqual(2927, elevation);
                else
                    Assert.Fail();
            });

            Assert.AreEqual(items.Length, count);
        }
    }
}
