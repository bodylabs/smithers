using Microsoft.Kinect;

namespace Smithers.Reading.FrameData.Extensions
{
    public static class PointFExtensions
    {
        public static float[] ToArray(this PointF point)
        {
            return new[] { point.X, point.Y };
        }
    }
}
