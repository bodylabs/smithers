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
using Smithers.Reading.FrameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Smithers.Visualization
{
    public class ProjectionMode
    {
        public static readonly ProjectionMode COLOR_IMAGE = new ProjectionMode(Frame.COLOR_WIDTH, Frame.COLOR_HEIGHT, 1063.23, 1063.84, false);
        public static readonly ProjectionMode DEPTH_IMAGE = new ProjectionMode(Frame.DEPTH_INFRARED_WIDTH, Frame.DEPTH_INFRARED_HEIGHT, 360, 360, true);
        public static readonly ProjectionMode INFRARED_IMAGE = DEPTH_IMAGE;

        public double Width { get; private set; }
        public double Height { get; private set; }
        public double Alpha { get; private set; }
        public double Beta { get; private set; }

        private bool _useDepthMapper;

        ProjectionMode(double width, double height, double alpha, double beta, bool useDepthMapper)
        {
            this.Width = width;
            this.Height = height;
            this.Alpha = alpha;
            this.Beta = beta;
            _useDepthMapper = useDepthMapper;
        }

        /// <summary>
        /// Project the camera space point using the given sensor coordinate mapper.
        /// </summary>
        /// <param name="inPoint"></param>
        /// <param name="coordinateMapper"></param>
        /// <returns></returns>
        public Point ProjectCameraPoint(CameraSpacePoint inPoint, CoordinateMapper coordinateMapper)
        {
            if (_useDepthMapper)
            {
                DepthSpacePoint depthPoint = coordinateMapper.MapCameraPointToDepthSpace(inPoint);
                return new Point(depthPoint.X, depthPoint.Y);
            }
            else
            {
                ColorSpacePoint colorPoint = coordinateMapper.MapCameraPointToColorSpace(inPoint);
                return new Point(colorPoint.X, colorPoint.Y);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left">Min x in pixels</param>
        /// <param name="right">Max x in pixels</param>
        /// <param name="bottom">Min y in pixels</param>
        /// <param name="top">Max y in pixels</param>
        /// <param name="near">Near clipping plane in world units</param>
        /// <param name="far">Far clipping plane in world units</param>
        /// <param name="alpha">Supposed field of view width of camera</param>
        /// <param name="beta">Supposed field of view height of camera</param>
        /// <returns></returns>
        private static Matrix3D CreatePerspectiveProjectionMatrix(double left, double right, double bottom, double top, double near, double far, double alpha, double beta)
        {
            Matrix3D projectionMatrix = new Matrix3D();

            projectionMatrix.M11 = 2.0 * alpha / (right - left);
            projectionMatrix.M22 = 2.0 * beta / (top - bottom);
            projectionMatrix.M31 = -0.01;
            projectionMatrix.M32 = 0.03;
            projectionMatrix.M33 = (far + near) / (near - far);
            projectionMatrix.M34 = -1.0;
            projectionMatrix.OffsetZ = near * far / (near - far);
            projectionMatrix.M44 = 0;

            return projectionMatrix;
        }

        /// <summary>
        /// Get a perspective camera for this projection.
        /// </summary>
        /// <param name="near"></param>
        /// <param name="far"></param>
        /// <returns></returns>
        public MatrixCamera CreatePerspectiveCamera(double near, double far)
        {
            Matrix3D projectionMatrix = CreatePerspectiveProjectionMatrix(0, this.Width, 0, this.Height, near, far, this.Alpha, this.Beta);
            return new MatrixCamera(Matrix3D.Identity, projectionMatrix);
        }
    }
}
