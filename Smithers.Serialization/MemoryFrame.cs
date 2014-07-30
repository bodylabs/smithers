using Smithers.Reading.FrameData;
using Smithers.Serialization.Formats;
using System;
using System.Windows.Media.Imaging;

namespace Smithers.Serialization
{
    /// <summary>
    /// Represents a serialized frame in memory.
    /// 
    /// A relatively heavyweight object, intended to be reused.
    /// </summary>
    public class MemoryFrame
    {
        // Buffer storage
        byte[] _bufferDepthMapping = new byte[Frame.DEPTH_INFRARED_PIXELS * FrameSerializer.DEPTH_MAPPING_BYTES_PER_PIXEL];
        byte[] _bufferDepth = new byte[Frame.DEPTH_INFRARED_PIXELS * FrameSerializer.DEPTH_INFRARED_BYTES_PER_PIXEL];
        byte[] _bufferInfrared = new byte[Frame.DEPTH_INFRARED_PIXELS * FrameSerializer.DEPTH_INFRARED_BYTES_PER_PIXEL];
        byte[] _bufferColor = new byte[Frame.COLOR_PIXELS * FrameSerializer.COLOR_BYTES_PER_PIXEL];
        byte[] _bufferBodyIndex = new byte[Frame.DEPTH_INFRARED_PIXELS * FrameSerializer.BODY_INDEX_BYTES_PER_PIXEL];

        // BLKD handle (underlying storage uses corresponding buffer above)
        Tuple<Blkd, TimeSpan> _depthMapping;

        // Bitmap handles (underlying storage uses corresponding buffers above)
        Tuple<BitmapSource, TimeSpan> _depth;
        Tuple<BitmapSource, TimeSpan> _infrared;
        Tuple<BitmapSource, TimeSpan> _color;
        Tuple<BitmapSource, TimeSpan> _bodyIndex;

        // Serialized skeleton
        Tuple<object, TimeSpan> _skeleton;

        /// <summary>
        /// We call Freeze() so we can write these bitmaps to disk from other threads.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="serializer"></param>
        public void Update(LiveFrame frame, FrameSerializer serializer)
        {
            // (1) Depth mapping
            _depthMapping = serializer.CaptureMappedFrame(frame, _bufferDepthMapping);

            // (2) Depth
            _depth = serializer.CaptureDepthFrameBitmap(frame, _bufferDepth);
            _depth.Item1.Freeze();

            // (3) Infrared
            _infrared = serializer.CaptureInfraredFrameBitmap(frame, _bufferInfrared);
            _infrared.Item1.Freeze();

            // (4) Skeleton
            _skeleton = serializer.SerializeSkeletonData(frame);

            // (5) Color
            _color = serializer.CaptureColorFrameBitmap(frame, _bufferColor);
            _color.Item1.Freeze();

            // (6) Body index
            _bodyIndex = serializer.CaptureBodyIndexFrameBitmap(frame, _bufferBodyIndex);
            _bodyIndex.Item1.Freeze();
        }

        public void Clear()
        {
            _depthMapping = null;
            _depth = null;
            _infrared = null;
            _color = null;
            _bodyIndex = null;
            _skeleton = null;
        }

        public Tuple<Blkd, TimeSpan> MappedDepth { get { return _depthMapping; } }
        public Tuple<BitmapSource, TimeSpan> Depth { get { return _depth; } }
        public Tuple<BitmapSource, TimeSpan> Infrared { get { return _infrared; } }
        public Tuple<BitmapSource, TimeSpan> Color { get { return _color; } }
        public Tuple<BitmapSource, TimeSpan> BodyIndex { get { return _bodyIndex; } }
        public Tuple<object, TimeSpan> Skeleton { get { return _skeleton; } }
    }
}
