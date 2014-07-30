using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Serialization.Writers
{
    public abstract class JsonFrameWriter : MemoryFrameWriter
    {
        public JsonFrameWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override string FileExtension { get { return ".json"; } }

        protected abstract object Object { get; }

        public override void Write(Stream stream)
        {
            JsonSerializer serializer = new JsonSerializer();

            using (StreamWriter streamWriter = new StreamWriter(stream))
            using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter))
            {
                jsonWriter.Formatting = Formatting.Indented;
                serializer.Serialize(jsonWriter, this.Object);
            }
        }
    }

    public class SkeletonWriter : JsonFrameWriter
    {
        public SkeletonWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override SavedItemType Type { get { return SavedItemType.SKELETON; } }

        public override TimeSpan? Timestamp { get { return _frame.Skeleton.Item2; } }

        protected override object Object { get { return _frame.Skeleton.Item1; } }
    }
}
