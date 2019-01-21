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
            try
            {
                Tools.Log("Start update");

                if (Tools.IsMono())
                {
                    chmod("/etc/teslalogger/nohup.out", 666);

                    if (!exec_mono("git", "--version").Contains("git version"))
                    {
                        exec_mono("apt-get", "-y install git");
                        exec_mono("git", "--version");
                    }


                    exec_mono("rm", "-rf /etc/teslalogger/git/*");

                    exec_mono("rm", "-rf /etc/teslalogger/git");
                    exec_mono("mkdir", "/etc/teslalogger/git");
                    exec_mono("git", "clone https://github.com/bassmaster187/TeslaLogger /etc/teslalogger/git/");
                    Tools.CopyFilesRecursively(new System.IO.DirectoryInfo("/etc/teslalogger/git/TeslaLogger/www"), new System.IO.DirectoryInfo("/var/www/html"));
                }

                Tools.Log("End update");
            }
            catch (Exception ex)
            {
                Tools.Log("Error in update: " + ex.ToString());
            }
        }

        public static string exec_mono(string cmd, string param)
        {
            try
            {
                if (!Tools.IsMono())
                    return "";

                Tools.Log("execute: " + cmd + " " + param);

                StringBuilder sb = new StringBuilder();

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.FileName = cmd;
                proc.StartInfo.Arguments = param;
                
                proc.Start();
                while (!proc.StandardOutput.EndOfStream)
                {
                    string line = proc.StandardOutput.ReadLine();
                    Tools.Log(" " + line);
                    sb.AppendLine(line);
                }

                while (!proc.StandardError.EndOfStream)
                {
                    string line = proc.StandardError.ReadLine();
                    Tools.Log("Error: " + line);
                }

                proc.WaitForExit();

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Tools.Log("Exception " + cmd + " " + ex.Message);
                return "Exception";
            }
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
