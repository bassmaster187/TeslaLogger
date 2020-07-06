using NUnit.Framework;

namespace TeslaLogger
{
    [TestFixture()]
    public class AutoUpdateTest
    {
        [Test()]
        public void VersionCheck1()
        {
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.0.0", Tools.UpdateType.all));
        }

        [Test()]
        public void VersionCheck2()
        {
            Assert.IsTrue(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.0.1", Tools.UpdateType.all));
        }

        [Test()]
        public void VersionCheck3()
        {
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.1", "1.0.0.0", Tools.UpdateType.all));
        }

        [Test()]
        public void VersionCheck4()
        {
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.0.0", Tools.UpdateType.stable));
        }

        [Test()]
        public void VersionCheck5()
        {
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.0.1", Tools.UpdateType.stable));
        }

        [Test()]
        public void VersionCheck6()
        {
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.1.0", Tools.UpdateType.stable));
        }

        [Test()]
        public void VersionCheck7()
        {
            Assert.IsTrue(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.1.0.0", Tools.UpdateType.stable));
        }

        [Test()]
        public void VersionCheck8()
        {
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.2.3.4", Tools.UpdateType.stable));
        }

        [Test()]
        public void VersionCheck9()
        {
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "2.0.0.1", Tools.UpdateType.stable));
        }

        [Test()]
        public void VersionCheck10()
        {
            Assert.IsTrue(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "2.0.0.0", Tools.UpdateType.stable));
        }

        [Test()]
        public void VersionCheck11()
        {
            Assert.IsTrue(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "2.1.0.0", Tools.UpdateType.stable));
        }

        [Test()]
        public void VersionCheck12()
        {
            Assert.IsTrue(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "2.0.0.0", Tools.UpdateType.stable));
        }

        [Test()]
        public void VersionCheck13()
        {
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.0.0", Tools.UpdateType.none));
        }

        [Test()]
        public void VersionCheck14()
        {
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.0.1", Tools.UpdateType.none));
        }

        [Test()]
        public void VersionCheck15()
        {
            Assert.IsTrue(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "2.0.0.0", Tools.UpdateType.stable));
        }
    }
}