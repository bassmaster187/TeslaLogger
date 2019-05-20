namespace TeslaLogger
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml.Serialization;

    public class CarSettings
    {
        public string Name = "";
        public string Model = "";
        public string Battery = "";
        public string Wh_TR = "0.190052356";
        public string DB_Wh_TR = "";
        public string DB_Wh_TR_count = "0";
        public bool AWD = false;
        public bool Performance = false;        

        public static CarSettings ReadSettings()
        {
            CarSettings ret = null;
            TextReader tr = null;
            try
            {
                XmlSerializer s = new XmlSerializer(typeof(CarSettings));

                var filePath = FileManager.GetFilePath(TLFilename.CarSettings);

                if (filePath != string.Empty)
                {
                    tr = new StreamReader(filePath, Encoding.UTF8);

                    ret = (CarSettings)s.Deserialize(tr);
                }
            }
            catch (Exception e)
            {
                Tools.Log($"ReadCarSettings Exception: {e.Message}");
                ret = new CarSettings();
            }
            finally
            {
                if (tr != null)
                    tr.Close();
            }

            return ret;
        }

        public void WriteSettings()
        {
            TextWriter tw = null;
            try
            {
                Tools.Log("Write car settings");

                XmlSerializer s = new XmlSerializer(typeof(CarSettings));

                tw = new StreamWriter(FileManager.GetFilePath(TLFilename.CarSettings), false, Encoding.UTF8);

                s.Serialize(tw, this);
            }
            catch (Exception e)
            {
                Tools.Log($"WriteCarSettings Exception: {e.Message}");
            }
            finally
            {
                if (tw != null)
                    tw.Close();
            }
        }
    }
}
