// Copyright (c) 2014, Body Labs, Inc.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
//
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
// FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
// COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
// BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS
// OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED
// AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY
// WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Reading.Calibration
{
    /// <summary>
    /// This is for writing a file that can be used to extract the camera
    /// calibration using standard tools like OpenCV.
    /// </summary>
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
