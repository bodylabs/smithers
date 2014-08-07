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
