// Copyright (c) 2014, Body Labs, Inc.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
//
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
// FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
// COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
// BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS
// OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED
// AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY
// WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Runtime.CompilerServices;

namespace Smithers.Visualization.Filters
{
    public enum DisplayFilterMode
    {
        GrayScale,
        Rainbow,
        SegmentRaw,
        SegmentGrayScale,
        SegmentRainbow,
    }

    public class DisplayFilter
    {

        private DisplayFilterMode _mode;
        private int _displayWidth;
        private int _displayHeight;
        private int _centerDepth;
        private int _rainbowMin;
        private int _rainbowMax;
        private int _bytesPerPixel;

        /// <summary>
        /// Set Pixel using rainbow transformed color
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="index"></param>
        /// <param name="color"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetPixel(byte[] pixels, int index, RainbowColor color)
        {
            pixels[index++] = color.Red;
            pixels[index++] = color.Green;
            pixels[index++] = color.Blue;
            pixels[index++] = 0xff;
        }

        /// <summary>
        /// Set Pixel using grayscale tranformed color
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="index"></param>
        /// <param name="intensity"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetPixel(byte[] pixels, int index, byte intensity, bool background)
        {
            pixels[index++] = background ? (byte)(intensity * 0.7) : intensity;
            pixels[index++] = intensity;
            pixels[index++] = background ? (byte)(intensity * 0.7) : intensity;
            pixels[index++] = 0xff;
        }

        public DisplayFilter(int bytesPerPixel)
        {
            _bytesPerPixel = bytesPerPixel;
        }

        public void Init(DisplayFilterMode mode, int width, int height, int centerDepth, int rainbowMax, int rainbowMin)
        {
            _mode = mode;

            _displayWidth = width;
            _displayHeight = height;

            _centerDepth = centerDepth;
            _rainbowMax = rainbowMax;
            _rainbowMin = rainbowMin;
        }

        /// <summary>
        /// Segmentation of the background and player by making background grayscale
        /// </summary>
        /// <param name="colorBuffer"></param>
        /// <param name="bodyData"></param>
        /// <param name="depthPoints"></param>
        public void SegmentColor(byte[] colorBuffer, byte[] bodyData, DepthSpacePoint[] depthPoints)
        {
            int i = 0;
            foreach (DepthSpacePoint depthPoint in depthPoints)
            {
                float _depthX = depthPoint.X;
                float _depthY = depthPoint.Y;

                bool isPlayer = true;

                if (float.IsInfinity(_depthX) || float.IsInfinity(_depthY))
                {
                    isPlayer = false;
                }
                else
                {
                    int depthX = (int)(_depthX);
                    int depthY = (int)(_depthY);
                    int depthIndex = (depthY * 512 + depthX);

                    if (bodyData[depthIndex] == 0xff)
                        isPlayer = false;
                }

                if (! isPlayer)
                {
                    byte R = colorBuffer[i];
                    byte G = colorBuffer[i + 1];
                    byte B = colorBuffer[i + 2];
                    byte intensity =  (byte)(0.21 * R + 0.72 * G + 0.07 * B);
                    colorBuffer[i] = intensity;
                    colorBuffer[i + 1] = intensity;
                    colorBuffer[i + 2] = intensity;
                }

                i += 4;
            }

        }

        /// <summary>
        /// Generic function for filling display buffer and transforming depth data to colorspace 
        /// </summary>
        /// <param name="sourceBuffer"></param>
        /// <param name="outputBuffer"></param>
        /// <param name="bodyData"></param>
        /// <param name="colorPoints"></param>
        public void Apply(ushort[] sourceBuffer, byte[] outputBuffer, byte[] bodyData, ColorSpacePoint [] colorPoints=null)
        {
            int displayIndex = 0;

            for (int sourceIndex = 0; sourceIndex < sourceBuffer.Length; ++sourceIndex)
            {
                if (colorPoints != null)
                {
                    ColorSpacePoint colorPoint = colorPoints[sourceIndex];

                    // make sure the depth pixel maps to a valid point in color space
                    if (float.IsInfinity(colorPoint.X) || float.IsInfinity(colorPoint.Y))
                        continue;

                    int colorX = (int)Math.Floor(colorPoint.X + 0.5);
                    int colorY = (int)Math.Floor(colorPoint.Y + 0.5);

                    if ((colorX < 0) || (colorX >= _displayWidth) || (colorY < 0) || (colorY >= _displayHeight))
                    {
                        continue;
                    }

                    displayIndex = ((colorY * _displayWidth) + colorX) * _bytesPerPixel;
                }

                ApplyColor(outputBuffer, displayIndex, sourceBuffer, sourceIndex, bodyData);

                if (colorPoints == null)
                    displayIndex += 4;
            }

        }

        /// <summary>
        /// Apply color to display buffer(optionally segmented by bodyData)
        /// </summary>
        /// <param name="outputBuffer"></param>
        /// <param name="outputIndex"></param>
        /// <param name="inputBuffer"></param>
        /// <param name="inputIndex"></param>
        /// <param name="bodyData"></param>
        public void ApplyColor(byte[] outputBuffer, int outputIndex, ushort[] inputBuffer, int inputIndex, byte[] bodyData = null)
        {
            ushort rawValue = inputBuffer[inputIndex];

            bool isPlayer;

            switch(_mode)
            { 
                case DisplayFilterMode.Rainbow:

                    SetPixel(outputBuffer, outputIndex, rawValue == 0 ? Rainbow.Black : Rainbow.RangedColor(rawValue, _rainbowMin, _rainbowMax));
                    break;

                case DisplayFilterMode.GrayScale:
                    SetPixel(outputBuffer, outputIndex, GrayScale.Intensity(rawValue), false);
                    break;

                case DisplayFilterMode.SegmentGrayScale:
                    isPlayer = bodyData[inputIndex] != 0xff;

                    if (isPlayer)
                    {
                        SetPixel(outputBuffer, outputIndex, GrayScale.Intensity(rawValue), true);
                    }
                    else
                    {
                        SetPixel(outputBuffer, outputIndex, GrayScale.Intensity(rawValue), false);
                    }
                    break;

                case DisplayFilterMode.SegmentRaw:
                    isPlayer = bodyData[inputIndex] != 0xff;

                    if (isPlayer)
                    {
                        SetPixel(outputBuffer, outputIndex, rawValue == 0 ? Rainbow.Black : Rainbow.RangedColor(rawValue, _centerDepth - 450, _centerDepth + 650));
                    }
                    break;

                case DisplayFilterMode.SegmentRainbow:
                    isPlayer = bodyData[inputIndex] != 0xff;

                    if (isPlayer)
                    {
                        SetPixel(outputBuffer, outputIndex, rawValue == 0 ? Rainbow.Black : Rainbow.RangedColor(rawValue, _centerDepth - 450, _centerDepth + 650));
                    }
                    else
                    {
                        SetPixel(outputBuffer, outputIndex, GrayScale.Intensity(rawValue), false);
                    }

                    break;

                default:
                    break;
            }
        }
    }
}
