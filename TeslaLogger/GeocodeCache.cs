using System;
using System.Data;
using Exceptionless;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    internal class GeocodeCache
    {
        private DataTable dt = new DataTable("cache");
        private static GeocodeCache _instance;

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
            lock (this)
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
                    ex.ToExceptionless().FirstCarUserID().Submit();
                    Logfile.ExceptionWriter(ex, "");
                }
            }
        }

        public string Search(double lat, double lng)
        {
            lock (this)
            {
                DataRow dr = dt.Rows.Find(new object[] { lat, lng });
                return dr?["Value"].ToString();
            }
        }


        public void Insert(double lat, double lng, string value)
        {
            lock (this)
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
                catch (ConstraintException cex)
                {
                    if (cex.HResult != -2146232022)  // Column 'lat, lng' is constrained to be unique.  Value 'xx, xx' is already present.
                    {
                        cex.ToExceptionless().FirstCarUserID().Submit();
                    }

                    Logfile.Log(cex.Message);
                }
                catch (Exception ex)
                {
                    ex.ToExceptionless().FirstCarUserID().Submit();
                    Logfile.Log(ex.Message);
                }
            }
        }

        public void Write()
        {
            lock (this)
            {
                try
                {
                    dt.WriteXml(FileManager.GetFilePath(TLFilename.GeocodeCache));
                }
                catch (Exception ex)
                {
                    ex.ToExceptionless().FirstCarUserID().Submit();
                    Logfile.Log(ex.Message);
                }
            }
        }

        internal void ClearCache()
        {
            lock (this)
            {
                dt.Clear();
            }
        }
    }
}
