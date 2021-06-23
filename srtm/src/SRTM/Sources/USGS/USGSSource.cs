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


using System;

namespace SRTM.Sources.USGS
{
    /// <summary>
    /// Defines an USGS source of data.
    /// </summary>
    public class USGSSource
    {
        /// <summary>
        /// The source of the data.
        /// </summary>

        // public const string SOURCE = @"https://dds.cr.usgs.gov/srtm/version2_1/SRTM3/";
        public const string SOURCE = @"https://srtm.kurviger.de/SRTM3/";

        /// <summary>
        /// The continents to try.
        /// </summary>
        public static string[] CONTINENTS = new string[]
        {
            "Eurasia",
            "North_America",
            "South_America",
            "Australia",
            "Islands",
            "Africa"
        };

        /// <summary>
        /// Gets the missing cell.
        /// </summary>
        public static bool GetMissingCell(string path, string name)
        {
            var filename = name + ".hgt.zip";
            var local = System.IO.Path.Combine(path, filename);

            Log($"Downloading {name} ...");
            foreach (var continent in CONTINENTS)
            {
                if (SourceHelpers.Download(local, SOURCE + continent + "/" + filename))
                {
                    return true;
                }
            }
            return false;
        }

        public static void Log(string text)
        {
            Console.WriteLine(DateTime.Now.ToString(ciDeDE) + " : " + text);
        }

        public static System.Globalization.CultureInfo ciDeDE = new System.Globalization.CultureInfo("de-DE");
    }
}