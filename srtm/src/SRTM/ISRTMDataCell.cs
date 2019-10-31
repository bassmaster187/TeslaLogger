using System;
using System.Collections.Generic;
using System.Text;

namespace SRTM
{
    public interface ISRTMDataCell
    {
        int Latitude { get; }

        int Longitude { get; }

        int? GetElevation(double latitude, double longitude);
    }
}
