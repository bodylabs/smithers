using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;

namespace Smithers.Reading.FrameData.Mock
{
    public class MockLiveFrame : LiveFrame
    {
        public MockLiveFrame() : base(KinectSensor.GetDefault())
        {
            
        }
        public MockColorFrame NativeColorFrame { get; set; }
        public MockDepthFrame NativeDepthFrame { get; set; }
        public MockInfraredFrame NativeInfraredFrame { get; set; }
        public MockBodyFrame NativeBodyFrame { get; set; }
        public MockBodyIndexFrame NativeBodyIndexFrame { get; set; }
        public CoordinateMapper NativeCoordinateMapper { get { return KinectSensor.GetDefault().CoordinateMapper; } }

    }
}
