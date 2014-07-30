using Microsoft.Kinect;

namespace Smithers.Reading.FrameData.Extensions
{
    public static class Vector4Extensions
    {
        public static float[] ToArray(this Vector4 vector)
        {
            return new[] { vector.X, vector.Y, vector.Z, vector.W };
        }

        public static string ToString(this Vector4 vector)
        {
            return vector.X + " " + vector.Y + " " + vector.Z + " " + vector.W;
        }
    }
}
