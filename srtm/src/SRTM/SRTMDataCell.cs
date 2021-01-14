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
using System.IO;

namespace SRTM
{
    /// <summary>
    /// SRTM data cell.
    /// </summary>
    /// <exception cref='FileNotFoundException'>
    /// Is thrown when a file path argument specifies a file that does not exist.
    /// </exception>
    /// <exception cref='ArgumentException'>
    /// Is thrown when an argument passed to a method is invalid.
    /// </exception>
    /// <exception cref='ArgumentOutOfRangeException'>
    /// Is thrown when an argument passed to a method is invalid because it is outside the allowable range of values as
    /// specified by the method.
    /// </exception>
    public class SRTMDataCell : ISRTMDataCell
    {
        #region Lifecycle

        /// <summary>
        /// Initializes a new instance of the <see cref="Alpinechough.Srtm.SrtmDataCell"/> class.
        /// </summary>
        /// <param name='filepath'>
        /// Filepath.
        /// </param>
        /// <exception cref='FileNotFoundException'>
        /// Is thrown when a file path argument specifies a file that does not exist.
        /// </exception>
        /// <exception cref='ArgumentException'>
        /// Is thrown when an argument passed to a method is invalid.
        /// </exception>
        public SRTMDataCell(string filepath)
        {
            if (!File.Exists(filepath))
                throw new FileNotFoundException("File not found.", filepath);

            var filename = Path.GetFileName(filepath);
            filename = filename.Substring(0, filename.IndexOf('.')).ToLower(); // Path.GetFileNameWithoutExtension(filepath).ToLower();
            var fileCoordinate = filename.Split(new[] { 'e', 'w' });
            if (fileCoordinate.Length != 2)
                throw new ArgumentException("Invalid filename.", filepath);

            fileCoordinate[0] = fileCoordinate[0].TrimStart(new[] { 'n', 's' });

            Latitude = int.Parse(fileCoordinate[0]);
            if (filename.Contains("s"))
                Latitude *= -1;

            Longitude = int.Parse(fileCoordinate[1]);
            if (filename.Contains("w"))
                Longitude *= -1;

            if (filepath.EndsWith(".zip"))
            {
                using (var stream = File.OpenRead(filepath))
                using (var archive = new System.IO.Compression.ZipArchive(stream))
                using (var memoryStream = new MemoryStream())
                {
                    using (var hgt = archive.Entries[0].Open())
                    {
                        hgt.CopyTo(memoryStream);
                        HgtData = memoryStream.ToArray();
                    }
                }
            }
            else
            {
                HgtData = File.ReadAllBytes(filepath);
            }

            switch (HgtData.Length)
            {
                case 1201 * 1201 * 2: // SRTM-3
                    PointsPerCell = 1201;
                    break;
                case 3601 * 3601 * 2: // SRTM-1
                    PointsPerCell = 3601;
                    break;
                default:
                    throw new ArgumentException("Invalid file size.", filepath);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the hgt data.
        /// </summary>
        /// <value>
        /// The hgt data.
        /// </value>
        private byte[] HgtData { get; set; }

        /// <summary>
        /// Gets or sets the points per cell.
        /// </summary>
        /// <value>
        /// The points per cell.
        /// </value>
        private int PointsPerCell { get; set; }

        /// <summary>
        /// Gets or sets the latitude of the srtm data file.
        /// </summary>
        /// <value>
        /// The latitude.
        /// </value>
        public int Latitude { get; private set; }

        /// <summary>
        /// Gets or sets the longitude of the srtm data file.
        /// </summary>
        /// <value>
        /// The longitude.
        /// </value>
        public int Longitude { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the elevation.
        /// </summary>
        /// <returns>
        /// The height. Null, if elevation is not available.
        /// </returns>
        /// <param name='latitude'></param>
        /// <param name='longitude'></param>
        /// <exception cref='Exception'>
        /// Represents errors that occur during application execution.
        /// </exception>
        public int? GetElevation(double latitude, double longitude)
        {
            int localLat = (int)((latitude - Latitude) * PointsPerCell);
            int localLon = (int)(((longitude - Longitude)) * PointsPerCell);
            int bytesPos = ((PointsPerCell - localLat - 1) * PointsPerCell * 2) + localLon * 2;

            if (bytesPos < 0 || bytesPos > PointsPerCell * PointsPerCell * 2)
                throw new ArgumentOutOfRangeException("Coordinates out of range.", "coordinates");

            if (bytesPos >= HgtData.Length)
                return null;

            if ((HgtData[bytesPos] == 0x80) && (HgtData[bytesPos + 1] == 0x00))
                return null;

            // Motorola "big-endian" order with the most significant byte first
            return (HgtData[bytesPos]) << 8 | HgtData[bytesPos + 1];
        }

        #endregion
    }
}