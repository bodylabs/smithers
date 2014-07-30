using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Reading.Calibration
{
    public class Calibrator
    {
        private static CalibrationRecord Calibrate(KinectSensor sensor)
        {
            int width = sensor.DepthFrameSource.FrameDescription.Width;
            int height = sensor.DepthFrameSource.FrameDescription.Height;

            ushort minDepth = sensor.DepthFrameSource.DepthMinReliableDistance;
            ushort maxDepth = sensor.DepthFrameSource.DepthMaxReliableDistance;

            var result = new CalibrationRecord();
            int nextDepth = minDepth;
            int depthIncrement = 777;
            if (depthIncrement >= maxDepth - minDepth || (maxDepth - minDepth) % depthIncrement == 0)
                throw new ArgumentException("Pick an increment which is less than, and not divisible by, maxDepth - minDepth");

            // 0 to 512
            for (int depthX = 0; depthX < width; depthX += 3)
            {
                // 0 to 424
                for (int depthY = 0; depthY < height; depthY += 3)
                {
                    // 500 to 4500
                    DepthSpacePoint depthPoint = new DepthSpacePoint
                    {
                        X = depthX,
                        Y = depthY
                    };

                    ColorSpacePoint colorPoint = sensor.CoordinateMapper.MapDepthPointToColorSpace(depthPoint, (ushort)nextDepth);
                    CameraSpacePoint bodyPoint = sensor.CoordinateMapper.MapDepthPointToCameraSpace(depthPoint, (ushort)nextDepth);

                    CalibrationPoint cpoint = new CalibrationPoint()
                    {
                        DepthPoint = depthPoint,
                        Depth = (ushort)nextDepth,
                        CameraPoint = bodyPoint,
                        ColorPoint = colorPoint
                    };

                    result.AddCalibrationPoint(cpoint);

                    nextDepth += depthIncrement;
                    if (nextDepth >= maxDepth) nextDepth -= maxDepth - minDepth;
                }
            }

            return result;
        }

        public static Task<CalibrationRecord> CalibrateAsync(KinectSensor sensor)
        {
            return Task<CalibrationRecord>.Run(() => Calibrator.Calibrate(sensor));
        }

    }
}
