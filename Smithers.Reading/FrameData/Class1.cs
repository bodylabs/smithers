using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Reading.FrameData
{
    class MockLiveFrame : LiveFrame
    {
        public ColorFrame NativeColorFrame { get; set; }
        public DepthFrame NativeDepthFrame { get; set; }
        public InfraredFrame NativeInfraredFrame { get; set; }
        public BodyFrame NativeBodyFrame { get; set; }
        public BodyIndexFrame NativeBodyIndexFrame { get; set; }
        public CoordinateMapper NativeCoordinateMapper { get { return _sensor.CoordinateMapper; } }
    }
}
