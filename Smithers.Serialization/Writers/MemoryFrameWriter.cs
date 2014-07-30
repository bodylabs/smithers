using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Serialization.Writers
{
    public abstract class MemoryFrameWriter : IWriter
    {
        protected MemoryFrame _frame;
        protected FrameSerializer _serializer;

        public MemoryFrameWriter(MemoryFrame frame, FrameSerializer serializer)
        {
            _frame = frame;
            _serializer = serializer;
        }

        public abstract SavedItemType Type { get; }

        public abstract string FileExtension { get; }

        public abstract TimeSpan? Timestamp { get; }

        public abstract void Write(Stream stream);
    }
}
