using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeslaLogger
{
    class UpdateTeslalogger
    {
        public static void Start()
        {
            chmod("/etc/teslalogger/nohup.out", 666);

        }

        public static void chmod(string filename, int chmod)
        {
            try
            {
                if (!Tools.IsMono())
                    return;

                Tools.Log("chmod " + chmod + " " + filename);

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
                proc.StartInfo.FileName = "chmod";
                proc.StartInfo.Arguments = chmod + " " + filename;
                proc.Start();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                Tools.Log("chmod " + filename + " " + ex.Message);
            }
        }
    }
}
