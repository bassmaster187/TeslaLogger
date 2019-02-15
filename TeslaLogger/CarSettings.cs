using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using System.IO;

namespace TeslaLogger
{
    public class CarSettings
    {
        public string Name = "";
        public string Model = "";
        public string Battery = "";
        public string Wh_TR = "0.190052356";
        public bool AWD = false;
        public bool Performance = false;
        public const string car_settings_filename = "car_settings.xml";

        public static CarSettings ReadSettings()
        {
            CarSettings ret = null;
            TextReader tr = null;
            try
            {
                XmlSerializer s = new XmlSerializer(typeof(CarSettings));
                tr = new StreamReader(car_settings_filename, System.Text.Encoding.UTF8);
                ret = (CarSettings)s.Deserialize(tr);
            }
            catch (Exception ex)
            {
                if (tr != null)
                    tr.Close();

                Tools.Log(ex.ToString());
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
                tw = new StreamWriter(car_settings_filename, false, System.Text.Encoding.UTF8);
                s.Serialize(tw, this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            finally
            {
                if (tw != null)
                    tw.Close();
            }
        }
    }
}
