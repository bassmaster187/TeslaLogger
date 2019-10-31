using System;
using System.IO;
using System.Net.Http;

namespace SRTM.Sources
{
    public class SourceHelpers
    {
        /// <summary>
        /// Donwloads a remote file and stores the data in the local one.
        /// </summary>
        public static bool Download(string local, string remote, bool logErrors = true)
        {
            try
            {
                if (File.Exists(local))
                {
                    File.Delete(local);
                }

                var client = new HttpClient();
                using (var stream = client.GetStreamAsync(remote).Result)
                using (var outputStream = File.OpenWrite(local))
                {
                    stream.CopyTo(outputStream);
                }

                TeslaLogger.Logfile.Log("Download OK: " + remote);
                return true;
            }
            catch (Exception ex)
            {
                if (logErrors)
                {
                    TeslaLogger.Logfile.Log("Download failed: " + remote);
                    TeslaLogger.Logfile.ExceptionWriter(ex, "Download failed:\r\n" + remote +  "\r\n" + local);
                }
            }
            return false;
        }
    }
}
