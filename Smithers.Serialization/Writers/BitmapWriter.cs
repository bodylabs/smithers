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

using Smithers.Serialization.Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Smithers.Serialization.Writers
{
    public abstract class PngBitmapWriter<TMemoryFrame, TFrameSerializer> : MemoryFrameWriter<TMemoryFrame, TFrameSerializer>
    {
        public PngBitmapWriter(TMemoryFrame frame, TFrameSerializer serializer) : base(frame, serializer) { }

        public override string FileExtension { get { return ".png"; } }

        protected abstract BitmapSource BitmapSource { get; }

        public override void Write(Stream stream)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(this.BitmapSource));
            encoder.Save(stream);
        }
    }

    public abstract class JpegBitmapWriter<TMemoryFrame, TFrameSerializer> : MemoryFrameWriter<TMemoryFrame, TFrameSerializer>
    {
        public JpegBitmapWriter(TMemoryFrame frame, TFrameSerializer serializer) : base(frame, serializer) { }

        public override string FileExtension { get { return ".jpg"; } }

        protected abstract BitmapSource BitmapSource { get; }

        public override void Write(Stream stream)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(this.BitmapSource));
            encoder.Save(stream);
        }
    }

    public abstract class BlkdFrameWriter<TMemoryFrame, TFrameSerializer> : MemoryFrameWriter<TMemoryFrame, TFrameSerializer>
    {
        public BlkdFrameWriter(TMemoryFrame frame, TFrameSerializer serializer) : base(frame, serializer) { }

        public override string FileExtension { get { return ".blkd"; } }

        protected abstract Blkd BlkdSource { get; }

        public override void Write(Stream stream)
        {
            Smithers.Serialization.Formats.Blkd.Save(this.BlkdSource, stream);
        }
    }

    public class DepthFrameWriter : PngBitmapWriter<MemoryFrame, FrameSerializer>
    {
        public DepthFrameWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override SavedItemType Type { get { return SavedItemType.DEPTH_IMAGE; } }

        public override TimeSpan? Timestamp { get { return _frame.Depth.Item2; } }

        protected override BitmapSource BitmapSource { get { return _frame.Depth.Item1; } }
    }

    public class InfraredFrameWriter : PngBitmapWriter<MemoryFrame, FrameSerializer>
    {
        public InfraredFrameWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override SavedItemType Type { get { return SavedItemType.INFRARED_IMAGE; } }

        public override TimeSpan? Timestamp { get { return _frame.Infrared.Item2; } }

        protected override BitmapSource BitmapSource { get { return _frame.Infrared.Item1; } }
    }

    public class ColorFrameWriter : JpegBitmapWriter<MemoryFrame, FrameSerializer>
    {
        public ColorFrameWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override SavedItemType Type { get { return SavedItemType.COLOR_IMAGE; } }

        public override TimeSpan? Timestamp { get { return _frame.Color.Item2; } }

        protected override BitmapSource BitmapSource { get { return _frame.Color.Item1; } }
    }

    public class BodyIndexFrameWriter : PngBitmapWriter<MemoryFrame, FrameSerializer>
    {
        public BodyIndexFrameWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override SavedItemType Type { get { return SavedItemType.BODY_INDEX; } }

        public override TimeSpan? Timestamp { get { return _frame.BodyIndex.Item2; } }

        protected override BitmapSource BitmapSource { get { return _frame.BodyIndex.Item1; } }
    }
}
