using Smithers.Reading.FrameData;
using Smithers.Visualization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Smithers.Visualization
{
    public enum CameraMode
    {
        Color,
        Depth,
        Infrared,
        InfraredDepth,
        ColorDepth
    }

    public class CameraImagePresenter : FrameReaderCallbacks
    {
        LargeFrameBitmap _bitmapLarge1 = new LargeFrameBitmap();
        LargeFrameBitmap _bitmapLarge2 = new LargeFrameBitmap();
        SmallFrameBitmap _bitmapSmall1 = new SmallFrameBitmap();
        SmallFrameBitmap _bitmapSmall2 = new SmallFrameBitmap();

        BitmapBuilder _bitmapBuilder = new BitmapBuilder();

        public Image CameraPrimary { get; private set; }
        public Image CameraSecondary { get; private set; }
        public CameraMode CameraMode { get; set; }
        public bool Enabled { get; set; }

        public CameraImagePresenter(Image cameraPrimary, Image cameraSecondary)
        {
            this.CameraPrimary = cameraPrimary;
            this.CameraSecondary = cameraSecondary;

            this.EnsureImageSources(null, 1.0, null, 1.0);

            this.Enabled = true;
        }

        private void EnsureImageSources(FrameBitmap primary, double primaryOpacity, FrameBitmap secondary, double secondaryOpacity)
        {
            ImageSource primarySource = primary == null ? null : primary.Bitmap;
            ImageSource secondarySource = secondary == null ? null : secondary.Bitmap;

            if (this.CameraPrimary.Source != primarySource)
                this.CameraPrimary.Source = primarySource;

            if (this.CameraSecondary.Source != secondarySource)
                this.CameraSecondary.Source = secondarySource;

            if (this.CameraPrimary.Opacity != primaryOpacity)
                this.CameraPrimary.Opacity = primaryOpacity;

            if (this.CameraSecondary.Opacity != secondaryOpacity)
                this.CameraSecondary.Opacity = secondaryOpacity;

            if (secondarySource == null)
                this.CameraSecondary.Visibility = System.Windows.Visibility.Collapsed;
            else
                this.CameraSecondary.Visibility = System.Windows.Visibility.Visible;
        }

        public void FrameArrived(LiveFrame frame)
        {
            if (!this.Enabled) return;

            FrameBitmap primary = null;
            FrameBitmap secondary = null;
            double primaryOpacity = 1.0;
            double secondaryOpacity = 1.0;

            switch (this.CameraMode)
            {
                case CameraMode.Color:
                    _bitmapBuilder.BuildColorBitmap(frame.NativeColorFrame, _bitmapLarge1, true);
                    primary = _bitmapLarge1;
                    break;
                case CameraMode.Depth:
                    _bitmapBuilder.BuildDepthBitmap(frame.NativeDepthFrame, _bitmapSmall1, true);
                    primary = _bitmapSmall1;
                    break;
                case CameraMode.Infrared:
                    _bitmapBuilder.BuildInfraredBitmap(frame.NativeInfraredFrame, _bitmapSmall1, true);
                    primary = _bitmapSmall1;
                    break;
                case CameraMode.ColorDepth:
                    throw new NotImplementedException("Camera mode not implemented");
                case CameraMode.InfraredDepth:
                    throw new NotImplementedException("Camera mode not implemented");
                default:
                    throw new ArgumentException("Unrecognized camera mode");
            }

            this.EnsureImageSources(primary, primaryOpacity, secondary, secondaryOpacity);
        }
    }
}
