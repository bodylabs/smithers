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
using Smithers.Reading.FrameData.Extensions;
using Smithers.Serialization;
using Smithers.Serialization.Formats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Smithers.Serialization
{
    public class FrameSerializer
    {
        public static readonly PixelFormat COLOR_PIXEL_FORMAT_WPF = PixelFormats.Bgr32;
        public static readonly ColorImageFormat COLOR_PIXEL_FORMAT_KINECT = ColorImageFormat.Bgra;
        public static readonly byte COLOR_BYTES_PER_PIXEL = (byte)((COLOR_PIXEL_FORMAT_WPF.BitsPerPixel + 7) / 8);

        public static readonly byte DEPTH_INFRARED_BYTES_PER_PIXEL = 2;
        public static readonly byte DEPTH_MAPPING_BYTES_PER_PIXEL = 16;
        public static readonly byte BODY_INDEX_BYTES_PER_PIXEL = 1;

        ushort[] _depthData = new ushort[Frame.DEPTH_INFRARED_PIXELS];
        ColorSpacePoint[] _colorPoints = new ColorSpacePoint[Frame.DEPTH_INFRARED_PIXELS];
        CameraSpacePoint[] _cameraSpacePoints = new CameraSpacePoint[Frame.DEPTH_INFRARED_PIXELS];
        ushort[] _smallBuffer = new ushort[Frame.DEPTH_INFRARED_PIXELS];
        byte[] _teenyBuffer = new byte[Frame.DEPTH_INFRARED_PIXELS];

        /// <summary>
        /// Densely store depth to color mapping as BLKD.
        /// 
        /// Returns the number of shorts written to buffer.
        /// </summary>
        /// <param name="frame">KinectScanner.Reading.Frame</param>
        /// <param name="filename">filename to store the mapping</param>
        /// <returns></returns>
        public Tuple<Blkd, TimeSpan> CaptureMappedFrame(LiveFrame frame, byte[] buffer)
        {
            DepthFrame depthFrame = frame.NativeDepthFrame;
            CoordinateMapper mapper = frame.NativeCoordinateMapper;

            if (buffer.Length != Frame.DEPTH_INFRARED_PIXELS * DEPTH_MAPPING_BYTES_PER_PIXEL)
                throw new ArgumentException(string.Format("Buffer length is {0} but {1} is needed", buffer.LongLength, Frame.DEPTH_INFRARED_PIXELS * DEPTH_MAPPING_BYTES_PER_PIXEL));

            depthFrame.CopyFrameDataToArray(_depthData);
            mapper.MapDepthFrameToColorSpace(_depthData, _colorPoints);
            mapper.MapDepthFrameToCameraSpace(_depthData, _cameraSpacePoints);

            Array.Clear(buffer, 0, buffer.Length);
            int count = 0;
            for (int i = 0; i < Frame.DEPTH_INFRARED_PIXELS; ++i)
            {
                ColorSpacePoint colorPoint = _colorPoints[i];
                CameraSpacePoint cameraPoint = _cameraSpacePoints[i];

                // make sure the depth pixel maps to a valid point in color space
                short colorX = (short)Math.Floor(colorPoint.X + 0.5);
                short colorY = (short)Math.Floor(colorPoint.Y + 0.5);

                if (colorX < 0 || colorX >= Frame.COLOR_WIDTH || colorY < 0 || colorY >= Frame.COLOR_HEIGHT)
                {
                    colorX = -1;
                    colorY = -1;
                }

                // Little endian === lowest order bytes at lower addresses
                buffer[count++] = (byte)(colorX >> 0);
                buffer[count++] = (byte)(colorX >> 8);

                buffer[count++] = (byte)(colorY >> 0);
                buffer[count++] = (byte)(colorY >> 8);

                float[] cameraPointValues = new float[] { cameraPoint.X, cameraPoint.Y, cameraPoint.Z };
                System.Buffer.BlockCopy(cameraPointValues, 0, buffer, count, 12);
                count += 12;
            }

            Blkd result = new Blkd
            {
                Width = (UInt16)Frame.DEPTH_INFRARED_WIDTH,
                Height = (UInt16)Frame.DEPTH_INFRARED_HEIGHT,
                BytesPerPixel = DEPTH_MAPPING_BYTES_PER_PIXEL,
                Version = 2,
                Data = buffer
            };
            return new Tuple<Blkd, TimeSpan>(result, depthFrame.RelativeTime);
        }

        private static BitmapSource BufferCaptureBitmapHelper(Array data, int width, int height, int bytesPerPixel, byte[] outBuffer)
        {
            long bytes = bytesPerPixel * width * height;

            if (outBuffer.Length < bytes)
                throw new ArgumentException(string.Format("Buffer is too short, at least {0} needed", bytes));

            if (bytes > int.MaxValue)
                throw new ArgumentException(string.Format("FIXME, Buffer.BlockCopy doesn't handle blocks longer than {0}", int.MaxValue));

            System.Buffer.BlockCopy(data, 0, outBuffer, 0, (int)bytes);

            return BitmapSource.Create(
                width,
                height,
                96,
                96,
                bytesPerPixel == 1 ? PixelFormats.Gray8 : PixelFormats.Gray16,
                bytesPerPixel == 1 ? BitmapPalettes.Gray16 : BitmapPalettes.Gray16,
                outBuffer,
                width * bytesPerPixel
            );
        }

        private static BitmapSource CreateColorBitmap(byte[] buffer, int width, int height)
        {
            long bytes = width * height * COLOR_BYTES_PER_PIXEL;

            if (buffer.Length != bytes)
                throw new ArgumentException(string.Format("Buffer is incorrect length, expected {0}", bytes));

            return BitmapSource.Create(
                width,
                height,
                96,
                96,
                COLOR_PIXEL_FORMAT_WPF,
                null,
                buffer,
                width * COLOR_BYTES_PER_PIXEL
            );
        }

        public Tuple<BitmapSource, TimeSpan> CaptureInfraredFrameBitmap(LiveFrame frame, byte[] buffer)
        {
            InfraredFrame infraredFrame = frame.NativeInfraredFrame;

            int width = infraredFrame.FrameDescription.Width;
            int height = infraredFrame.FrameDescription.Height;

            infraredFrame.CopyFrameDataToArray(_smallBuffer);

            BitmapSource result = BufferCaptureBitmapHelper(_smallBuffer, width, height, 2, buffer);
            return new Tuple<BitmapSource, TimeSpan>(result, infraredFrame.RelativeTime);
        }

        public Tuple<BitmapSource, TimeSpan> CaptureDepthFrameBitmap(LiveFrame frame, byte[] buffer)
        {
            DepthFrame depthFrame = frame.NativeDepthFrame;

            int width = depthFrame.FrameDescription.Width;
            int height = depthFrame.FrameDescription.Height;

            depthFrame.CopyFrameDataToArray(_smallBuffer);

            // Multiply all values by 8 to make the frames more previewable
            for (int i = 0; i < _smallBuffer.Length; ++i)
                _smallBuffer[i] <<= 3;

            BitmapSource result = BufferCaptureBitmapHelper(_smallBuffer, width, height, 2, buffer);
            return new Tuple<BitmapSource, TimeSpan>(result, depthFrame.RelativeTime);
        }

        public Tuple<BitmapSource, TimeSpan> CaptureBodyIndexFrameBitmap(LiveFrame frame, byte[] buffer)
        {
            BodyIndexFrame bodyIndexFrame = frame.NativeBodyIndexFrame;

            int width = bodyIndexFrame.FrameDescription.Width;
            int height = bodyIndexFrame.FrameDescription.Height;

            bodyIndexFrame.CopyFrameDataToArray(_teenyBuffer);

            BitmapSource result = BufferCaptureBitmapHelper(_teenyBuffer, width, height, 1, buffer);
            return new Tuple<BitmapSource, TimeSpan>(result, bodyIndexFrame.RelativeTime);
        }

        private void ValidateBuffer(byte[] bytes, int width, int height, byte bytesPerPixel)
        {
            if (bytes.Length != width * height * bytesPerPixel)
                throw new ArgumentException(string.Format("Buffer length doesn't match expected {0}x{1}x{2}", width, height, bytesPerPixel));
        }

        /// <summary>
        /// This method is similar to BitmapBuilder.buildColorBitmap. However, that method uses
        /// LargeFrameBitmap which encapsulates WriteableBitmap, and a WriteableBitmap can't be
        /// used on a different thread from the one which created it. It can't even be cloned,
        /// or used to create a new WriteableBitmap on a different thread.
        /// 
        /// So we provide this separate interface.
        /// 
        /// TODO: Examine this class and BitmapBuilder for overlaps, and determine if some
        /// consolidation is appropriate. Note that the methods here all provide raw data,
        /// whereas many of the methods in BitmapBuilder involve some processing.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Tuple<BitmapSource, TimeSpan> CaptureColorFrameBitmap(LiveFrame frame, byte[] buffer)
        {
            ValidateBuffer(buffer, Frame.COLOR_WIDTH, Frame.COLOR_HEIGHT, COLOR_BYTES_PER_PIXEL);

            ColorFrame colorFrame = frame.NativeColorFrame;

            colorFrame.CopyConvertedFrameDataToArray(buffer, ColorImageFormat.Bgra);

            BitmapSource result = CreateColorBitmap(buffer, Frame.COLOR_WIDTH, Frame.COLOR_HEIGHT);
            return new Tuple<BitmapSource, TimeSpan>(result, colorFrame.RelativeTime);
        }

        private static object SerializeJoint(Body skeleton, JointType joint)
        {
            Joint bodyJoint = skeleton.Joints[joint];
            JointOrientation orientation = skeleton.JointOrientations[joint];

            return new
            {
                Position = bodyJoint.Position.ToArray(),
                State = bodyJoint.TrackingState.ToString(),
                Rotation = orientation.Orientation.ToArray()
            };
        }

        public static object SerializeBody(Body skeleton, bool first = false)
        {
            Dictionary<string, object> joints = new Dictionary<string, object>();
            foreach (JointType jointType in Enum.GetValues(typeof(JointType)).Cast<JointType>())
            {
                joints[jointType.ToString()] = SerializeJoint(skeleton, jointType);
            }

            return new
            {
                First = first,
                Lean = skeleton.Lean.ToArray(),
                ClippedEdges = new
                {
                    Bottom = (skeleton.ClippedEdges & FrameEdges.Bottom) == FrameEdges.Bottom,
                    Left = (skeleton.ClippedEdges & FrameEdges.Left) == FrameEdges.Left,
                    Top = (skeleton.ClippedEdges & FrameEdges.Top) == FrameEdges.Top,
                    Right = (skeleton.ClippedEdges & FrameEdges.Right) == FrameEdges.Right
                },
                Joints = joints
            };
        }

        public Tuple<object, TimeSpan> SerializeSkeletonData(LiveFrame frame)
        {
            List<object> serializedBodies = new List<object>();
            Body firstBody = frame.FirstBody;
            if (firstBody != null)
            {
                serializedBodies.Add(SerializeBody(firstBody, true));
            }
            foreach (Body body in frame.TrackedBodies)
            {
                if (body == firstBody) continue;
                serializedBodies.Add(SerializeBody(body));
            }

            object result = new
            {
                FloorClipPlane = frame.NativeBodyFrame.FloorClipPlane.ToArray(),
                Bodies = serializedBodies
            };
            return new Tuple<object, TimeSpan>(result, frame.NativeBodyFrame.RelativeTime);
        }
    }
}
