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
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Smithers.Visualization
{
    /// <summary>
    /// Convenience classes to help document the expected size of bitmap methods in Frame
    /// and BitmapBuilder.
    /// 
    /// Since WriteableBitmap is a sealed class, we must contain it instead of subclassing it.
    /// </summary>
    public abstract class FrameBitmap
    {
        public static readonly PixelFormat PIXEL_FORMAT_WPF = PixelFormats.Bgr32;
        public static readonly ColorImageFormat PIXEL_FORMAT_KINECT = ColorImageFormat.Bgra;
        public static readonly byte BYTES_PER_PIXEL = (byte)((PIXEL_FORMAT_WPF.BitsPerPixel + 7) / 8);

        WriteableBitmap _bitmap;

        protected FrameBitmap(int width, int height)
        {
            _bitmap = new WriteableBitmap(width, height, 96.0, 96.0, PIXEL_FORMAT_WPF, null);
        }

        public WriteableBitmap Bitmap { get { return _bitmap; } }
    }

    public class LargeFrameBitmap : FrameBitmap
    {
        public LargeFrameBitmap() : base(Frame.COLOR_WIDTH, Frame.COLOR_HEIGHT) { }
    }

    public class SmallFrameBitmap : FrameBitmap
    {
        public SmallFrameBitmap() : base(Frame.DEPTH_INFRARED_WIDTH, Frame.DEPTH_INFRARED_HEIGHT) { }
    }
}
