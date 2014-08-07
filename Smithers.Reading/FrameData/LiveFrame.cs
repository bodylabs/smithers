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

namespace Smithers.Reading.FrameData
{
    // A collection of the frame data with caching convenience accessors.
    // Used for convenieince, and also to conserve memory and compute time
    // across multiple consumers: the CapturePage and the DiskCapturer.
    public class LiveFrame : IDisposable
    {
        // We use this to generate e.g. the greenscreen bitmap
        KinectSensor _sensor;
        Body[] _bodies;
        Body _firstBody;
        bool _disposed = false;

        public LiveFrame(KinectSensor sensor)
        {
            _sensor = sensor;
        }

        public void Dispose()
        {
            if (_disposed) return;

            if (this.NativeColorFrame != null)
            {
                this.NativeColorFrame.Dispose();
                this.NativeColorFrame = null;
            }
            if (this.NativeDepthFrame != null)
            {
                this.NativeDepthFrame.Dispose();
                this.NativeDepthFrame = null;
            }
            if (this.NativeInfraredFrame != null)
            {
                this.NativeInfraredFrame.Dispose();
                this.NativeInfraredFrame = null;
            }
            if (this.NativeBodyFrame != null)
            {
                this.NativeBodyFrame.Dispose();
                this.NativeBodyFrame = null;
            }
            if (this.NativeBodyIndexFrame != null)
            {
                this.NativeBodyIndexFrame.Dispose();
                this.NativeBodyIndexFrame = null;
            }

            GC.SuppressFinalize(this);
        }

        public ColorFrame NativeColorFrame { get; set; }
        public DepthFrame NativeDepthFrame { get; set; }
        public InfraredFrame NativeInfraredFrame { get; set; }
        public BodyFrame NativeBodyFrame { get; set; }
        public BodyIndexFrame NativeBodyIndexFrame { get; set; }
        public CoordinateMapper NativeCoordinateMapper { get { return _sensor.CoordinateMapper;  } }

        public Body[] Bodies
        {
            get
            {
                if (_bodies == null)
                {
                    var bodies = new Body[6];
                    if (this.NativeBodyFrame != null)
                    {
                        this.NativeBodyFrame.GetAndRefreshBodyData(bodies);
                        _bodies = bodies;
                    }
                }
                return _bodies;
            }
        }

        public IEnumerable<Body> TrackedBodies { get { return (this.Bodies ?? Enumerable.Empty<Body>()).Where(x => x.IsTracked); } }

        /// <summary>
        /// Return the closest body, giving priority to bodies near the center of the frame.
        /// </summary>
        /// <param name="bodies"></param>
        /// <returns></returns>
        private static Body ClosestBody(IEnumerable<Body> bodies)
        {
            if (bodies.Count() <= 1) return bodies.FirstOrDefault();

            Body closestBody = null;
            double minDistance = double.PositiveInfinity;

            foreach (Body body in bodies)
            {
                CameraSpacePoint spinePosition = body.Joints[JointType.SpineMid].Position;
                double distance = Math.Pow(spinePosition.Z, 2) + 0.5f * Math.Pow(spinePosition.X, 2);
                if (distance < minDistance)
                {
                    closestBody = body;
                    minDistance = distance;
                }
            }

            return closestBody;
        }

        public Body FirstBody
        {
            get
            {
                if (_firstBody == null)
                {
                    _firstBody = LiveFrame.ClosestBody(this.TrackedBodies);
                }
                return _firstBody;
            }
        }
    }
}
