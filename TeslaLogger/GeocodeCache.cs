using System;
using System.Data;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    internal class GeocodeCache : IDisposable
    {
        private DataTable dt = new DataTable("cache");
        private static GeocodeCache _instance;
        private bool isDisposed;

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
#pragma warning disable CA3075 // Unsichere DTD-Verarbeitung in XML
                    _ = dt.ReadXml(FileManager.GetFilePath(TLFilename.GeocodeCache));
#pragma warning restore CA3075 // Unsichere DTD-Verarbeitung in XML
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
            return dr?["Value"].ToString();
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

        internal void ClearCache()
        {
            dt.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;
            if (disposing)
            {
                if (dt != null)
                {
                    dt.Dispose();
                }
            }
            isDisposed = true;
        }
    }
}
