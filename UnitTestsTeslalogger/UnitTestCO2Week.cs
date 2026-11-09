using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestCO2Week
    {
        // ISO 8601 week numbers / week-years around new year
        [DataTestMethod]
        [DataRow(2024, 12, 30, 2025, 1)]  // Calendar.GetWeekOfYear(FirstFourDayWeek) returned 53
        [DataRow(2025, 1, 1, 2025, 1)]
        [DataRow(2021, 1, 3, 2020, 53)]   // Sunday belongs to week 53 of 2020
        [DataRow(2026, 12, 31, 2026, 53)] // 2026 has 53 ISO weeks
        [DataRow(2027, 1, 3, 2026, 53)]
        [DataRow(2027, 1, 4, 2027, 1)]
        [DataRow(2026, 6, 15, 2026, 25)]
        public void IsoWeek(int y, int m, int d, int expectedYear, int expectedWeek)
        {
            DateTime dt = new DateTime(y, m, d, 12, 0, 0);
            Assert.AreEqual(expectedWeek, CO2.GetWeekOfYear(dt));
            Assert.AreEqual(expectedYear, CO2.GetWeekYear(dt));
        }
    }
}
