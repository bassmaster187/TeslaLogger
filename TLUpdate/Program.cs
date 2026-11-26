using System;
using System.IO;
using System.Reflection;

namespace TLUpdate
{
    internal class Program
    {
        static void Main(string[] _)
        {
            Console.WriteLine(" *** TLUpdate MAIN " + Assembly.GetExecutingAssembly().GetName().Version + " ***");
            try
            {
                bool copySuccessful = Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/git/TeslaLogger/bin"), new DirectoryInfo("/etc/teslalogger"), "TeslaLogger.exe");
                copySuccessful &= Tools.CopyFile("/etc/teslalogger/git/TeslaLogger/bin/TeslaLogger.exe", "/etc/teslalogger/TeslaLogger.exe");

                if (!copySuccessful)
                {
                    // never reboot into a half copied installation (old and new files mixed),
                    // keep the running version alive, the update will be retried later
                    Console.WriteLine(" *** Update incomplete: copy failed, NO reboot, keeping current version");
                    return;
                }

                Console.WriteLine(" *** End update");

                if (Tools.IsDocker())
                {
                    Console.WriteLine(" *** Restarting ...");
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine(" *** Rebooting ...");
                    Tools.ExecMono("reboot", "");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
