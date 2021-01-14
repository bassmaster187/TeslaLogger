// The MIT License (MIT)

// Copyright (c) 2017 Alpine Chough Software, Ben Abelshausen

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Serilog;
using System;

namespace SRTM.Tests.Functional
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .WriteTo.ColoredConsole(outputTemplate: "{Timestamp:HH:mm} [{Level}] ({Name:l}) {Message}{NewLine}{Exception}")
                .CreateLogger();
            Log.Logger = log;

            // https://dds.cr.usgs.gov/srtm/version2_1/SRTM3/
            var srtmData = new SRTMData(@"srtm-cache");

            int? elevationUlm = srtmData.GetElevation(48.197845, 9.954862);

            int? elevationInnsbruck = srtmData.GetElevation(47.267222, 11.392778);
            Console.WriteLine("Elevation of Innsbruck: {0}m", elevationInnsbruck);

            int? elevationLaPaz = srtmData.GetElevation(-16.5, -68.15);
            Console.WriteLine("Elevation of La Paz: {0}m", elevationLaPaz);

            int? elevationKathmandu = srtmData.GetElevation(27.702983735525862f, 85.2978515625f);
            Console.WriteLine("Elevation of Kathmandu {0}m", elevationLaPaz);

            int? elevationHanoi = srtmData.GetElevation(21.030673628606102f, 105.853271484375f);
            Console.WriteLine("Elevation of Ha Noi {0}m", elevationHanoi);

            // tries to get elevation from an empty cell.
            int? elevationSomeplace1 = srtmData.GetElevation(52.02237f, 2.55853224f);
            Console.WriteLine("Elevation of nowhere returns {0}", elevationSomeplace1);

            int? elevationNamibia1 = srtmData.GetElevation(-20, 19.89597);
            Console.WriteLine("Elevation of namibia1 returns {0}", elevationNamibia1);

            int? elevationRostock = srtmData.GetElevation(54.1298258, 12.0630578);
            Console.WriteLine("Elevation of Rostock returns {0}", elevationRostock);

            elevationRostock = srtmData.GetElevation(54.1238326, 12.0641476);
            Console.WriteLine("Elevation of Rostock returns {0}", elevationRostock);

            Console.WriteLine("Testing finished.");
            Console.ReadLine();
        }
    }
}
