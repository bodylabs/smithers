using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Smithers.Reading.Calibration
{
    // Maps points between depth, camera, and color spaces
    public struct CalibrationPoint
    {
        public DepthSpacePoint DepthPoint { get; set; }
        public ushort Depth { get; set; }
        public CameraSpacePoint CameraPoint { get; set; }
        public ColorSpacePoint ColorPoint { get; set; }
    }

    // A package of calibration data for a given sensor
    //
    // Currently just a collection mapping points in depth space to points in
    // color and camera space
    //
    public class CalibrationRecord
    {
        private List<CalibrationPoint> _depthToColorAndCamera = new List<CalibrationPoint>();

        public void AddCalibrationPoint(CalibrationPoint cpoint)
        {
            _depthToColorAndCamera.Add(cpoint);
        }

        public void Write(Stream stream)
        {
            CultureInfo cultureUS = CultureInfo.GetCultureInfo("en-US");

            using (TextWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("#XYZ depth_xyd rgb_xy");
                foreach (CalibrationPoint cpoint in this._depthToColorAndCamera)
                {                    
                    writer.WriteLine(
                        string.Format(cultureUS, "{0} {1} {2}   {3} {4} {5}   {6} {7}",
                        cpoint.CameraPoint.X, cpoint.CameraPoint.Y, cpoint.CameraPoint.Z,
                        cpoint.DepthPoint.X, cpoint.DepthPoint.Y, cpoint.Depth,
                        cpoint.ColorPoint.X, cpoint.ColorPoint.Y)
                    );
                }
            }
        }
    }
}
