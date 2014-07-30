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
