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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SRTM
{
    /// <summary>
    /// SRTM Data.
    /// </summary>
    /// <exception cref='DirectoryNotFoundException'>
    /// Is thrown when part of a file or directory argument cannot be found.
    /// </exception>
    public class SRTMData : ISRTMData
    {
        private const int RETRIES = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="Alpinechough.Srtm.SrtmData"/> class.
        /// </summary>
        /// <param name='dataDirectory'>
        /// Data directory.
        /// </param>
        /// <exception cref='DirectoryNotFoundException'>
        /// Is thrown when part of a file or directory argument cannot be found.
        /// </exception>
        public SRTMData(string dataDirectory)
        {
            if (!Directory.Exists(dataDirectory))
                Directory.CreateDirectory(dataDirectory);

            DataDirectory = dataDirectory;
            DataCells = new List<ISRTMDataCell>();
        }

        /// <summary>
        /// A delegate to get missing cells.
        /// </summary>
        public delegate bool GetMissingCellDelegate(string path, string name);

        /// <summary>
        /// Gets or sets the missing cell delegate.
        /// </summary>
        public GetMissingCellDelegate GetMissingCell { get; set; } = Sources.USGS.USGSSource.GetMissingCell;
        
        /// <summary>
        /// Gets or sets the data directory.
        /// </summary>
        /// <value>
        /// The data directory.
        /// </value>
        public string DataDirectory { get; private set; }

        /// <summary>
        /// Gets or sets the SRTM data cells.
        /// </summary>
        /// <value>
        /// The SRTM data cells.
        /// </value>
        private List<ISRTMDataCell> DataCells { get; set; }
        
        #region Public methods

        /// <summary>
        /// Unloads all SRTM data cells.
        /// </summary>
        public void Unload()
        {
            DataCells.Clear();
        }

        /// <summary>
        /// Gets the elevation.
        /// </summary>
        /// <returns>
        /// The height. Null, if elevation is not available.
        /// </returns>
        /// <param name='coordinates'>
        /// Coordinates.
        /// </param>
        /// <exception cref='Exception'>
        /// Represents errors that occur during application execution.
        /// </exception>
        public int? GetElevation(double latitude, double longitude)
        {
            int cellLatitude = (int)Math.Floor(Math.Abs(latitude));
            if (latitude < 0)
            {
                cellLatitude *= -1;
                if (cellLatitude != latitude)
                { // if exactly equal, keep the current tile.
                    cellLatitude -= 1; // because negative so in bottom tile
                }
            }

            int cellLongitude = (int)Math.Floor(Math.Abs(longitude));
            if (longitude < 0)
            {
                cellLongitude *= -1;
                if (cellLongitude != longitude)
                { // if exactly equal, keep the current tile.
                    cellLongitude -= 1; // because negative so in left tile
                }
            }

            var dataCell = DataCells.Where(dc => dc.Latitude == cellLatitude && dc.Longitude == cellLongitude).FirstOrDefault();
            if (dataCell != null)
                return dataCell.GetElevation(latitude, longitude);

            string filename = string.Format("{0}{1:D2}{2}{3:D3}",
                cellLatitude < 0 ? "S" : "N",
                Math.Abs(cellLatitude),
                cellLongitude < 0 ? "W" : "E",
                Math.Abs(cellLongitude));

            var filePath = Path.Combine(DataDirectory, filename + ".hgt");
            var zipFilePath = Path.Combine(DataDirectory, filename + ".hgt.zip");
            var txtFilePath = Path.Combine(DataDirectory, filename + ".txt");
            var count = -1;

            if (!File.Exists(filePath) && !File.Exists(zipFilePath) && !File.Exists(txtFilePath) &&
                this.GetMissingCell != null)
            {
                this.GetMissingCell(DataDirectory, filename);
            }
            else if(File.Exists(txtFilePath) && this.GetMissingCell != null)
            {
                var txtFile = File.ReadAllText(txtFilePath);
                if (!int.TryParse(txtFile, out count))
                {
                    File.Delete(txtFilePath);
                    count = -1;
                }
                else if(count < RETRIES)
                {
                    if (this.GetMissingCell(DataDirectory, filename))
                    {
                        File.Delete(txtFilePath);
                    }
                }
            }
            
            if (File.Exists(filePath))
            {
                dataCell = new SRTMDataCell(filePath);
            }
            else if(File.Exists(zipFilePath))
            {
                dataCell = new SRTMDataCell(zipFilePath);
            }
            else
            {
                if (count < 0)
                {
                    File.WriteAllText(txtFilePath, "1");
                    return null;
                }
                else if (count < RETRIES)
                {
                    count++;
                    File.WriteAllText(txtFilePath, count.ToString());
                    return null;
                }
                else
                {
                    dataCell = new EmptySRTMDataCell(txtFilePath);
                }
            }
            
            // add to cells.
            DataCells.Add(dataCell);

            // return requested elevation.
            return dataCell.GetElevation(latitude, longitude);
        }

        #endregion
    }
}