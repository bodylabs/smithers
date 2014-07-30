using Smithers.Serialization.Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Serialization.Writers
{
    public abstract class BlkdWriter : MemoryFrameWriter
    {
        public BlkdWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override string FileExtension { get { return ".blkd"; } }

        protected abstract Blkd Blkd { get; }

        public override void Write(Stream stream)
        {
            this.Blkd.Save(stream);
        }
    }

    public class DepthMappingWriter : BlkdWriter
    {
        public DepthMappingWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override SavedItemType Type { get { return SavedItemType.DEPTH_MAPPING; } }

        public override TimeSpan? Timestamp { get { return _frame.MappedDepth.Item2; } }

        protected override Blkd Blkd { get { return _frame.MappedDepth.Item1; } }
    }
}
