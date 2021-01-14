using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace TeslaLogger
{
    internal class CarSettings
    {
        /* TODO Class is not used anymore
         
        public int id;
        public string Name = "";
        public string Model = "";
        public string Battery = "";
        
        public bool AWD = false;
        public bool Performance = false;
        public string car_type = "";
        public string car_special_type = "";
        public string trim_badging = "";

        public static CarSettings ReadSettings()
        {
            CarSettings ret = null;
            TextReader tr = null;
            try
            {
                XmlSerializer s = new XmlSerializer(typeof(CarSettings));

                string filePath = FileManager.GetFilePath(TLFilename.CarSettings);

                if (filePath != string.Empty)
                {
                    tr = new StreamReader(filePath, Encoding.UTF8);

                    ret = (CarSettings)s.Deserialize(tr);
                }
            }
            catch (Exception e)
            {
                Logfile.Log($"ReadCarSettings Exception: {e.Message}");
                ret = new CarSettings();
            }
            finally
            {
                if (tr != null)
                {
                    tr.Close();
                }
            }

            return ret;
        }

        public void WriteSettings()
        {
            TextWriter tw = null;
            try
            {
                Logfile.Log("Write car settings");

                XmlSerializer s = new XmlSerializer(typeof(CarSettings));

                tw = new StreamWriter(FileManager.GetFilePath(TLFilename.CarSettings), false, Encoding.UTF8);

                s.Serialize(tw, this);
            }
            catch (Exception e)
            {
                Logfile.Log($"WriteCarSettings Exception: {e.Message}");
            }
            finally
            {
                if (tw != null)
                {
                    tw.Close();
                }
            }
        }
        */
    }
}
