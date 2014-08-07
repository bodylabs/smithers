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

namespace Smithers.Reading.FrameData
{

    [Serializable()]
    public class ScannerNotFoundException : System.Exception
    {
        public ScannerNotFoundException() : base() { }
        public ScannerNotFoundException(string message) : base(message) { }
        public ScannerNotFoundException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected ScannerNotFoundException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }

    public interface FrameReaderCallbacks
    {
        void FrameArrived(LiveFrame frame);
    }
    
    public class FrameReader
    {
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;


        List<FrameReaderCallbacks> _responders = new List<FrameReaderCallbacks>();

        #region Property
        public KinectSensor Sensor
        {
            get { return _sensor; }
        }

        public MultiSourceFrameReader NativeReader
        {
            get { return _reader; }
        }

        #endregion

        public FrameReader()
        {
            _sensor = KinectSensor.GetDefault();

            if (_sensor == null)
            {
                throw new ScannerNotFoundException("No valid plugged-in Kinect sensor found.");
            }

            if (!_sensor.IsOpen)
            {
                _sensor.Open();
            }

            _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);
            _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

        }

        public void AddResponder(FrameReaderCallbacks responder)
        {
            _responders.Add(responder);
        }

        protected void DispatchFrame(LiveFrame frame)
        {
            foreach (FrameReaderCallbacks responder in _responders)
            {
                responder.FrameArrived(frame);
            }
        }

        public void Dispose()
        {
            // Stop the reader, the body tracking, the sensor and release any resources.
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }

            if (_sensor != null && _sensor.IsOpen)
            {
                _sensor.Close();
                _sensor = null;
            }

            GC.SuppressFinalize(this);
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var result = new LiveFrame(_sensor);

            var multiFrame = e.FrameReference.AcquireFrame();

            if (multiFrame == null) return;

            result.NativeColorFrame = multiFrame.ColorFrameReference.AcquireFrame();
            result.NativeDepthFrame = multiFrame.DepthFrameReference.AcquireFrame();
            result.NativeInfraredFrame = multiFrame.InfraredFrameReference.AcquireFrame();
            result.NativeBodyFrame = multiFrame.BodyFrameReference.AcquireFrame();
            result.NativeBodyIndexFrame = multiFrame.BodyIndexFrameReference.AcquireFrame();

            // Dispose of the result, whether or not we dispatch it
            using (result)
            {
                if (result.NativeColorFrame != null && result.NativeDepthFrame != null && result.NativeInfraredFrame != null && result.NativeBodyFrame != null && result.NativeBodyIndexFrame != null)
                {
                    this.DispatchFrame(result);
                }
            }
        }

    }
}
