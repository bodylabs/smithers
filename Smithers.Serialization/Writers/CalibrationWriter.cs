using Smithers.Reading.Calibration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Serialization.Writers
{
    public class CalibrationWriter : IWriter
    {
        CalibrationRecord _calibrationRecord;

        public CalibrationWriter(CalibrationRecord calibrationRecord)
        {
            _calibrationRecord = calibrationRecord;
        }

        public SavedItemType Type { get { return SavedItemType.CALIBRATION; } }

        public string FileExtension { get { return ".txt"; } }

        public TimeSpan? Timestamp { get { return null; } }

        public void Write(Stream stream)
        {
            _calibrationRecord.Write(stream);
        }
    }
}
