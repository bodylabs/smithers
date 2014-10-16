using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smithers.Reading.FrameData;

namespace Smithers.Reading
{
    public class FpsChangedEventArgs : EventArgs
    {
        public double Fps { get; set; }

        /// <summary>
        /// TimeStamp when fps changed, in ms.
        /// </summary>
        public double TimeStamp { get; set; }

        public FpsChangedEventArgs(double fps, double ts)
        {
            this.Fps = fps;
            this.TimeStamp = ts;
        }
    }

    public class FrameRateReporter : FrameReaderCallbacks
    {
        public double? StartTimeInMilliseconds { get; set; }
        public double InstantFrameRate { get; set; }

        public event EventHandler<FpsChangedEventArgs> FpsChanged;

        public FrameRateReporter()
        {
        }

        private double GetFps(double milliseconds)
        {
            return 1000.0 / milliseconds;
        }

        public double GetInstantFps(double currentTimeInMilliseconds)
        {
            if (this.StartTimeInMilliseconds.HasValue)
            {
                double fps = GetFps(currentTimeInMilliseconds - this.StartTimeInMilliseconds.Value);
                this.StartTimeInMilliseconds = currentTimeInMilliseconds;
                this.InstantFrameRate = fps;
                return fps;
            }

            return 0.0;
        }

        public void FrameArrived(LiveFrame frame)
        {

            if (FpsChanged != null)
            {
                TimeSpan colorFrameRelativeTime = frame.NativeColorFrame.RelativeTime;

                if (!this.StartTimeInMilliseconds.HasValue)
                {
                    this.StartTimeInMilliseconds = colorFrameRelativeTime.TotalMilliseconds;
                }

                FpsChanged(new object(), new FpsChangedEventArgs(this.GetInstantFps(frame.NativeColorFrame.RelativeTime.TotalMilliseconds), colorFrameRelativeTime.TotalMilliseconds));
                
            }
        }
    }
}
