using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestSleepSeconds
    {
        [TestMethod]
        public void RateLimitReached()
        {
            // division by zero when all commands of the day are used
            Assert.AreEqual(10, Tools.CalculateSleepSeconds(300, 300, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual(10, Tools.CalculateSleepSeconds(300, 310, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
        }

        [TestMethod]
        public void RemainingCommands()
        {
            // 12:00 -> 720 minutes left = 43200 s, 100 commands left -> 432 s
            Assert.AreEqual(432, Tools.CalculateSleepSeconds(300, 200, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
        }
    }
}
