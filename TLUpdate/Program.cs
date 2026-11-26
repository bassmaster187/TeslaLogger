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

                // legacy mono binary - not contained in the NET8 update packages anymore, only copy if present
                if (File.Exists("/etc/teslalogger/git/TeslaLogger/bin/TeslaLogger.exe"))
                {
                    copySuccessful &= Tools.CopyFile("/etc/teslalogger/git/TeslaLogger/bin/TeslaLogger.exe", "/etc/teslalogger/TeslaLogger.exe");
                }
                else
                {
                    Console.WriteLine(" *** TeslaLogger.exe not in update package (NET8 package), skipping");
                }

                // always reboot, even when copies failed: Teslalogger may already be dead when
                // TLUpdate runs (it was made an external process exactly for that case), so the
                // reboot is the recovery mechanism. A broken copy is retried by the daily
                // version check, because the local version stays older than the online version.
                if (!copySuccessful)
                {
                    Console.WriteLine(" *** Update incomplete: some copies failed, rebooting anyway");
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
