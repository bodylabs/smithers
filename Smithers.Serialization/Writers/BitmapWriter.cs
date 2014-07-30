using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Smithers.Serialization.Writers
{
    public abstract class PngBitmapWriter : MemoryFrameWriter
    {
        public PngBitmapWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override string FileExtension { get { return ".png"; } }

        protected abstract BitmapSource BitmapSource { get; }

        public override void Write(Stream stream)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(this.BitmapSource));
            encoder.Save(stream);
        }
    }

    public abstract class JpegBitmapWriter : MemoryFrameWriter
    {
        public JpegBitmapWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override string FileExtension { get { return ".jpg"; } }

        protected abstract BitmapSource BitmapSource { get; }

        public override void Write(Stream stream)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(this.BitmapSource));
            encoder.Save(stream);
        }
    }

    public class DepthFrameWriter : PngBitmapWriter
    {
        public DepthFrameWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override SavedItemType Type { get { return SavedItemType.DEPTH_IMAGE; } }

        public override TimeSpan? Timestamp { get { return _frame.Depth.Item2; } }

        protected override BitmapSource BitmapSource { get { return _frame.Depth.Item1; } }
    }

    public class InfraredFrameWriter : PngBitmapWriter
    {
        public InfraredFrameWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override SavedItemType Type { get { return SavedItemType.INFRARED_IMAGE; } }

        public override TimeSpan? Timestamp { get { return _frame.Infrared.Item2; } }

        protected override BitmapSource BitmapSource { get { return _frame.Infrared.Item1; } }
    }

    public class ColorFrameWriter : JpegBitmapWriter
    {
        public ColorFrameWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override SavedItemType Type { get { return SavedItemType.COLOR_IMAGE; } }

        public override TimeSpan? Timestamp { get { return _frame.Color.Item2; } }

        protected override BitmapSource BitmapSource { get { return _frame.Color.Item1; } }
    }

    public class BodyIndexFrameWriter : PngBitmapWriter
    {
        public BodyIndexFrameWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override SavedItemType Type { get { return SavedItemType.BODY_INDEX; } }

        public override TimeSpan? Timestamp { get { return _frame.BodyIndex.Item2; } }

        protected override BitmapSource BitmapSource { get { return _frame.BodyIndex.Item1; } }
    }
}
