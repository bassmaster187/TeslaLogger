using System;
using System.IO;
using System.Reflection;

namespace TLUpdate
{
    internal class Program
    {
        static void Main(string[] _)
        {
            Tools.Log(" *** TLUpdate MAIN " + Assembly.GetExecutingAssembly().GetName().Version + " ***");
            try
            {
                var exclude = new string[] { "TeslaLogger.exe", "TLUpdate.exe" };
                Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/git/TeslaLogger/bin"), new DirectoryInfo("/etc/teslalogger"), exclude);

                Tools.CopyFile("/etc/teslalogger/git/TeslaLogger/bin/TeslaLogger.exe", "/etc/teslalogger/TeslaLogger.exe");

                Tools.Log(" *** End update");

                if (Tools.IsDocker())
                {
                    Tools.Log(" *** Restarting ...");
                    Environment.Exit(0);
                }
                else
                {
                    Tools.Log(" *** Rebooting ...");
                    Tools.ExecMono("reboot", "");
                }
            }
            catch (Exception ex)
            {
                Tools.Log(ex.ToString());
            }
        }
    }
}
