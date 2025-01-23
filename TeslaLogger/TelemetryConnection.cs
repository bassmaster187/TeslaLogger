using System;

namespace TeslaLogger
{
    class TelemetryConnection
    {
        public TelemetryParser parser;
        /*
        public static TelemetryConnection Instance(Car car)
        {
            var ret = TelemetryConnection.Instance(car, ApplicationSettings.Default.TelemetryServerType);
            return ret;
        }
        */
        public static TelemetryConnection Instance(Car car)
        {
            if (ApplicationSettings.Default.TelemetryServerType == "ZMQ")
            {
                return new TelemetryConnectionZMQ(car);
            }
            else
            {
                return new TelemetryConnectionWS(car);
            }
        }

        public virtual void StartConnection()
        {

        }

        public virtual void CloseConnection()
        {

        }

    }

}