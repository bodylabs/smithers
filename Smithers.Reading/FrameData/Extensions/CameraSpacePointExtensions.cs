using Microsoft.Kinect;

namespace Smithers.Reading.FrameData.Extensions
{
    public static class CameraSpacePointExtensions
    {
        public static float[] ToArray(this CameraSpacePoint point)
        {
            return new[] { point.X, point.Y, point.Z };
        }
    }
}
