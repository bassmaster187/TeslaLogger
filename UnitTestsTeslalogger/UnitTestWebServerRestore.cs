using Microsoft.VisualStudio.TestTools.UnitTesting;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestWebServerRestore
    {
        [TestMethod]
        public void AllowedLocalBackupFile()
        {
            Assert.IsTrue(WebServer.IsAllowedLocalBackupFile("/etc/teslalogger/backup/mysqldump20230101.gz"));
        }

        [TestMethod]
        public void RejectedLocalBackupFiles()
        {
            Assert.IsFalse(WebServer.IsAllowedLocalBackupFile(null));
            Assert.IsFalse(WebServer.IsAllowedLocalBackupFile(""));
            Assert.IsFalse(WebServer.IsAllowedLocalBackupFile("/etc/passwd"));
            Assert.IsFalse(WebServer.IsAllowedLocalBackupFile("/etc/teslalogger/backup/../settings.json"));
            Assert.IsFalse(WebServer.IsAllowedLocalBackupFile("/etc/teslalogger/backup/other.gz"));
            Assert.IsFalse(WebServer.IsAllowedLocalBackupFile("/etc/teslalogger/backup/mysqldump.gz; rm -rf /"));
            Assert.IsFalse(WebServer.IsAllowedLocalBackupFile("/etc/teslalogger/backup/mysqldump$(id).gz"));
        }

        [TestMethod]
        public void ShellQuote()
        {
            Assert.AreEqual("'abc'", WebServer.ShellQuote("abc"));
            Assert.AreEqual("'a b;c'", WebServer.ShellQuote("a b;c"));
            Assert.AreEqual("'it'\\''s'", WebServer.ShellQuote("it's"));
            Assert.AreEqual("''", WebServer.ShellQuote(null));
        }
    }
}
