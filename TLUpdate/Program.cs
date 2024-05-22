using System;
using System.IO;

namespace TLUpdate
{
    internal class Program
    {
        static void Main(string[] _)
        {
            Console.WriteLine(" *** TLUpdate MAIN ***");
            try
            {
                Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/git/TeslaLogger/bin"), new DirectoryInfo("/etc/teslalogger"), "TeslaLogger.exe");

                Tools.CopyFile("/etc/teslalogger/git/TeslaLogger/bin/TeslaLogger.exe", "/etc/teslalogger/TeslaLogger.exe");

                Console.WriteLine("End update");

                if (Tools.IsDocker())
                {
                    Console.WriteLine("Restarting ...");
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("Rebooting ...");
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
