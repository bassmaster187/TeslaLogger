using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeslaLogger
{
    class GeocodeCache
    {
        DataTable dt = new DataTable("cache");
        static GeocodeCache _instance;

        public static GeocodeCache Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GeocodeCache();
                }

                return _instance;
            }
        }

        public GeocodeCache()
        {
            DataColumn lat = dt.Columns.Add("lat", typeof(double));
            DataColumn lng = dt.Columns.Add("lng", typeof(double));
            dt.Columns.Add("Value");

            dt.PrimaryKey = new DataColumn[] { lat, lng };

            try
            {
                if (System.IO.File.Exists(FileManager.GetFilePath(TLFilename.GeocodeCache)))
                {
                    dt.ReadXml(FileManager.GetFilePath(TLFilename.GeocodeCache));
                    Logfile.Log("GeocodeCache Items: " + dt.Rows.Count);
                }
                else
                {
                    Logfile.Log(FileManager.GetFilePath(TLFilename.GeocodeCache) + " Not found!");
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "");
            }
        }

        public string Search(double lat, double lng)
        {
            DataRow dr = dt.Rows.Find(new object[] {lat, lng });
            if (dr == null)
                return null;

            return dr["Value"].ToString();
        }


        public void Insert(double lat, double lng, string value)
        {
            try
            {
                DataRow dr = dt.NewRow();
                dr["lat"] = lat;
                dr["lng"] = lng;
                dr["value"] = value;
                dt.Rows.Add(dr);

                Logfile.Log("GeocodeCache:Insert");
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.Message);
            }
        }

        public void Write()
        {
            try
            {
                dt.WriteXml(FileManager.GetFilePath(TLFilename.GeocodeCache));
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.Message);
            }
        }
    }
}
