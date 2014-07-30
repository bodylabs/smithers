using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Reading.FrameData
{
    public static class Frame
    {
        public static readonly int COLOR_WIDTH = 1920;
        public static readonly int COLOR_HEIGHT = 1080;
        public static readonly int COLOR_PIXELS = COLOR_WIDTH * COLOR_HEIGHT;

        public static readonly int DEPTH_INFRARED_WIDTH = 512;
        public static readonly int DEPTH_INFRARED_HEIGHT = 424;
        public static readonly int DEPTH_INFRARED_PIXELS = DEPTH_INFRARED_WIDTH * DEPTH_INFRARED_HEIGHT;
    }
}
