using Microsoft.Kinect;
using Smithers.Reading.FrameData;
using Smithers.Visualization.Filters;
using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Visualization
{
    public class BitmapBuilder
    {
        ushort[] _depthData = new ushort[Frame.DEPTH_INFRARED_PIXELS];
        ushort[] _infraredData = new ushort[Frame.DEPTH_INFRARED_PIXELS];
        byte[] _depthPixels = new byte[Frame.DEPTH_INFRARED_PIXELS * FrameBitmap.BYTES_PER_PIXEL];
        byte[] _infraredPixels = new byte[Frame.DEPTH_INFRARED_PIXELS * FrameBitmap.BYTES_PER_PIXEL];
        DisplayFilter _displayFilter = new DisplayFilter(FrameBitmap.BYTES_PER_PIXEL);

        protected void ValidateBitmap(WriteableBitmap bitmap, int width, int height)
        {
            if (bitmap.PixelWidth != width || bitmap.PixelHeight != height)
                throw new ArgumentException(string.Format("Bitmap dimensions don't match expected {0}x{1}", width, height));
            if (bitmap.Format != FrameBitmap.PIXEL_FORMAT_WPF)
                throw new ArgumentException("Bitmap format is not expected Bgr32");
        }

        public void BuildColorBitmap(ColorFrame colorFrame, LargeFrameBitmap bitmap, bool withLock)
        {
            WriteableBitmap outBitmap = bitmap.Bitmap;
            ValidateBitmap(outBitmap, Frame.COLOR_WIDTH, Frame.COLOR_HEIGHT);

            if (withLock) outBitmap.Lock();

            // Direct copy
            colorFrame.CopyConvertedFrameDataToIntPtr(outBitmap.BackBuffer, (uint)(Frame.COLOR_PIXELS * FrameBitmap.BYTES_PER_PIXEL), ColorImageFormat.Bgra);

            if (withLock)
            {
                outBitmap.AddDirtyRect(new Int32Rect(0, 0, Frame.COLOR_WIDTH, Frame.COLOR_HEIGHT));
                outBitmap.Unlock();
            }
        }

        /// <summary>
        /// Build depth bitmap.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="bitmap"></param>
        /// <param name="withLock"></param>
        /// <param name="segmentation"></param>
        public void BuildDepthBitmap(DepthFrame depthFrame, SmallFrameBitmap bitmap, bool withLock)
        {
            depthFrame.CopyFrameDataToArray(_depthData);

            _displayFilter.Init(
                DisplayFilterMode.Rainbow,
                Frame.DEPTH_INFRARED_WIDTH,
                Frame.DEPTH_INFRARED_HEIGHT,
                0,
                depthFrame.DepthMaxReliableDistance,
                depthFrame.DepthMinReliableDistance
            );

            Array.Clear(_depthPixels, 0, _depthPixels.Length);
            _displayFilter.Apply(_depthData, _depthPixels, null);

            WriteableBitmap outBitmap = bitmap.Bitmap;
            ValidateBitmap(outBitmap, Frame.DEPTH_INFRARED_WIDTH, Frame.DEPTH_INFRARED_HEIGHT);
            CopyToDisplay(outBitmap, _depthPixels, withLock);
        }

        public void BuildInfraredBitmap(InfraredFrame infraredFrame, SmallFrameBitmap bitmap, bool withLock)
        {
            infraredFrame.CopyFrameDataToArray(_infraredData);

            _displayFilter.Init(
                DisplayFilterMode.GrayScale,
                Frame.DEPTH_INFRARED_WIDTH,
                Frame.DEPTH_INFRARED_HEIGHT,
                0,
                int.MaxValue,
                int.MinValue
            );

            Array.Clear(_infraredPixels, 0, _infraredPixels.Length);
            _displayFilter.Apply(_infraredData, _infraredPixels, null);

            WriteableBitmap outBitmap = bitmap.Bitmap;
            ValidateBitmap(outBitmap, Frame.DEPTH_INFRARED_WIDTH, Frame.DEPTH_INFRARED_HEIGHT);
            CopyToDisplay(outBitmap, _infraredPixels, withLock);
        }

        /// <summary>
        /// Copy to display bitmap
        /// </summary>
        /// <param name="outputBuffer"></param>
        /// <param name="bytesNeeded"></param>
        /// <param name="withLock"></param>
        public static void CopyToDisplay(WriteableBitmap displayBitmap, byte[] outputBuffer, bool withLock)
        {
            displayBitmap.WritePixels(new Int32Rect(0, 0, displayBitmap.PixelWidth, displayBitmap.PixelHeight), outputBuffer,
                                      displayBitmap.PixelWidth * FrameBitmap.BYTES_PER_PIXEL, 0);
        }
    }
}
