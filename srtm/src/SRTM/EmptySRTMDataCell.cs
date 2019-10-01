using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SRTM
{
    public class EmptySRTMDataCell : ISRTMDataCell
    {
        public EmptySRTMDataCell(string filepath)
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
        }

        public int Latitude { get; private set; }

        public int Longitude { get; private set; }

        public int? GetElevation(double latitude, double longitude)
        {
            return null;
        }
    }
}
