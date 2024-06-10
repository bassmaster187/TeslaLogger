using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace TLUpdate
{
    public class Tools
    {
        public Tools()
        {
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target, string excludeFile = null)
        {
            if (source != null && target != null)
            {
                try
                {
                    foreach (DirectoryInfo dir in source.GetDirectories())
                    {
                        CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
                    }

                    foreach (FileInfo file in source.GetFiles())
                    {
                        if (excludeFile != null && file.Name == excludeFile)
                        {
                            Console.WriteLine($" *** CopyFilesRecursively: skip {excludeFile}");
                        }
                        else
                        {
                            string p = Path.Combine(target.FullName, file.Name);
                            Console.WriteLine(" *** Copy '" + file.FullName + "' to '" + p + "'");
                            File.Copy(file.FullName, p, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(" *** CopyFilesRecursively Exception: " + ex.ToString());
                }
            }
            else
            {
                Console.WriteLine($" *** CopyFilesRecursively: source or target is null - source:{source} target:{target}");
            }
        }

        public static void CopyFile(string srcFile, string directory)
        {
            try
            {
                Console.WriteLine(" *** Copy '" + srcFile + "' to '" + directory + "'");
                File.Copy(srcFile, directory, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(" *** CopyFile Exception: " + ex.ToString());
            }
        }

        public static string ExecMono(string cmd, string param, bool logging = true, bool stderr2stdout = false, int timeout = 0)
        {
            Console.WriteLine(" *** Exec_mono: " + cmd + " " + param);

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
                        Console.WriteLine(" ***  " + line);
                    }

                    sb.AppendLine(line);
                    line = proc.StandardError.ReadToEnd().Replace('\r', '\n');

                    if (logging && line.Length > 0)
                    {
                        if (stderr2stdout)
                        {
                            Console.WriteLine(" ***  " + line);
                        }
                        else
                        {
                            Console.WriteLine(" *** Error: " + line);
                        }
                    }

                    sb.AppendLine(line);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(" *** Exception " + cmd + " " + ex.Message);
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
                Console.WriteLine(" *** Exception " + ex.Message);
            }

            return false;
        }
    }
}

