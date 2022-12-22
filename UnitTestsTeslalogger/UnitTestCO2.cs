using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestCO2
    {
        [TestMethod]
        public void TestGetData()
        {
            ServicePointManager.ServerCertificateValidationCallback += (p1, p2, p3, p4) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            CO2 co2 = new CO2();
            co2.GetData();

        }
    }
}
