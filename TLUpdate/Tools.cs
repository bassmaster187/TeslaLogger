using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace TLUpdate
{
    public class Tools
    {
        public static System.Globalization.CultureInfo ciDeDE = new System.Globalization.CultureInfo("de-DE");
        public Tools()
        {
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target, params string[] excludeFiles)
         {
             if (source != null && target != null)
             {
                 try
                 {
                     foreach (DirectoryInfo dir in source.GetDirectories())
                     {
                         CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name), excludeFiles);
                     }

                     foreach (FileInfo file in source.GetFiles())
                     {
                         if (excludeFiles != null && Array.IndexOf(excludeFiles, file.Name) >= 0)
                         {
                             Tools.Log($" *** CopyFilesRecursively: skip {file.Name}");
                         }
                         else
                         {
                             string p = Path.Combine(target.FullName, file.Name);
                             Tools.Log(" *** Copy '" + file.FullName + "' to '" + p + "'");
                             File.Copy(file.FullName, p, true);
                         }
                     }
                 }
                 catch (Exception ex)
                 {
                     Tools.Log(" *** CopyFilesRecursively Exception: " + ex.ToString());
                 }
             }
             else
             {
                 Tools.Log($" *** CopyFilesRecursively: source or target is null - source:{source} target:{target}");
             }
         }

        public static void CopyFile(string srcFile, string directory)
        {
            try
            {
                Tools.Log(" *** Copy '" + srcFile + "' to '" + directory + "'");
                File.Copy(srcFile, directory, true);
            }
            catch (Exception ex)
            {
                Tools.Log(" *** CopyFile Exception: " + ex.ToString());
            }
        }

        public static string ExecMono(string cmd, string param, bool logging = true, bool stderr2stdout = false, int timeout = 0)
        {
            Tools.Log(" *** Exec_mono: " + cmd + " " + param);

            StringBuilder sb = new StringBuilder();

            bool bTimeout = false;

            try
            {
                if (!Tools.IsMono())
                {
                    return "";
                }

                using (Process proc = new Process())
                {
                    proc.EnableRaisingEvents = false;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.FileName = cmd;
                    proc.StartInfo.Arguments = param;

                    proc.Start();

                    do
                    {
                        if (!proc.HasExited)
                        {
                            proc.Refresh();

                            if (timeout > 0 && (DateTime.Now - proc.StartTime).TotalSeconds > timeout)
                            {
                                proc.Kill();
                                bTimeout = true;
                            }
                        }
                    }
                    while (!proc.WaitForExit(100));

                    string line = proc.StandardOutput.ReadToEnd().Replace('\r', '\n');

                    if (logging && line.Length > 0)
                    {
                        Tools.Log(" ***  " + line);
                    }

                    sb.AppendLine(line);
                    line = proc.StandardError.ReadToEnd().Replace('\r', '\n');

                    if (logging && line.Length > 0)
                    {
                        if (stderr2stdout)
                        {
                            Tools.Log(" ***  " + line);
                        }
                        else
                        {
                            Tools.Log(" *** Error: " + line);
                        }
                    }

                    sb.AppendLine(line);
                }
            }
            catch (Exception ex)
            {
                Tools.Log(" *** Exception " + cmd + " " + ex.Message);
                return "Exception";
            }
            return bTimeout ? "Timeout! " + sb.ToString() : sb.ToString();
        }

        public static bool IsMono()
        {
            return GetMonoRuntimeVersion() != "NULL";
        }

        public static string GetMonoRuntimeVersion()
        {
            Type type = Type.GetType("Mono.Runtime");
            if (type != null)
            {
                MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (displayName != null)
                {
                    return displayName.Invoke(null, null).ToString();
                }
            }
            return "NULL";
        }

        public static bool IsDocker()
        {
            try
            {
                string filename = "/tmp/teslalogger-DOCKER";
                if (File.Exists(filename))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Tools.Log(" *** Exception " + ex.Message);
            }

            return false;
        }

        public static void Log(string text)
        {
            string temp = DateTime.Now.ToString(ciDeDE) + " : " + text;

            Console.WriteLine(temp);
        }
    }
}

