using System;

namespace TeslaLogger
{
    class TelemetryConnection
    {
        public TelemetryParser parser;

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