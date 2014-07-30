using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Visualization.Filters
{
    public class RainbowColor
    {
        public byte Red;
        public byte Green;
        public byte Blue;

        static public readonly RainbowColor Black = new RainbowColor();
        static public readonly RainbowColor White = new RainbowColor { Red = 255, Green = 255, Blue = 255 };
    }

    public class Rainbow
    {
        static private RainbowColor[] _cache;

        #region Constants

        static private readonly double Gamma = 0.80;
        static private readonly double IntensityMax = 255;
        static private readonly double WavelengthMin = 380.0;
        static private readonly double WavelengthMax = 781.0;

        static private readonly int Increments = 5000;
        static public readonly RainbowColor Black = new RainbowColor { };

        static Rainbow()
        {
            _cache = new RainbowColor[Increments];
            for (int i = 0; i < Increments; ++i)
            {
                _cache[i] = CreateRangedColor(i, 0, Increments);
            }
        }

        #endregion

        private static RainbowColor CreateRangedColor(double value, double rangeMin, double rangeMax)
        {
            double wavelength = WavelengthMin + ((value - rangeMin) / (rangeMax - rangeMin)) * (WavelengthMax - WavelengthMin);
            return WavelengthToColor(wavelength);
        }

        private static RainbowColor WavelengthToColor(double wavelength)
        {
            double factor;
            double red, green, blue;

            if ((wavelength >= 380) && (wavelength < 440))
            {
                red = -(wavelength - 440) / (440 - 380);
                green = 0.0;
                blue = 1.0;
            }
            else if ((wavelength >= 440) && (wavelength < 490))
            {
                red = 0.0;
                green = (wavelength - 440) / (490 - 440);
                blue = 1.0;
            }
            else if ((wavelength >= 490) && (wavelength < 510))
            {
                red = 0.0;
                green = 1.0;
                blue = -(wavelength - 510) / (510 - 490);
            }
            else if ((wavelength >= 510) && (wavelength < 580))
            {
                red = (wavelength - 510) / (580 - 510);
                green = 1.0;
                blue = 0.0;
            }
            else if ((wavelength >= 580) && (wavelength < 645))
            {
                red = 1.0;
                green = -(wavelength - 645) / (645 - 580);
                blue = 0.0;
            }
            else if ((wavelength >= 645) && (wavelength < 781))
            {
                red = 1.0;
                green = 0.0;
                blue = 0.0;
            }
            else
            {
                red = 0.0;
                green = 0.0;
                blue = 0.0;
            }

            // Let the intensity fall off near the vision limits

            if ((wavelength >= 380) && (wavelength < 420))
            {
                factor = 0.3 + 0.7 * (wavelength - 380) / (420 - 380);
            }
            else if ((wavelength >= 420) && (wavelength < 701))
            {
                factor = 1.0;
            }
            else if ((wavelength >= 701) && (wavelength < 781))
            {
                factor = 0.3 + 0.7 * (780 - wavelength) / (780 - 700);
            }
            else
            {
                factor = 0.0;
            }

            var color = new RainbowColor();

            // Don't want 0^x = 1 for x <> 0
            color.Red = red == 0.0 ? (byte)0 : (byte)Math.Round(IntensityMax * Math.Pow(red * factor, Gamma));
            color.Green = green == 0.0 ? (byte)0 : (byte)Math.Round(IntensityMax * Math.Pow(green * factor, Gamma));
            color.Blue = blue == 0.0 ? (byte)0 : (byte)Math.Round(IntensityMax * Math.Pow(blue * factor, Gamma));

            return color;
        }

        /// <summary>
        /// We cache these values, since they're a bit slow to compute.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="rangeMin"></param>
        /// <param name="rangeMax"></param>
        /// <returns></returns>
        public static RainbowColor RangedColor(double value, double rangeMin, double rangeMax)
        {
            if (value < rangeMin) return _cache.First();
            if (value > rangeMax) return _cache.Last();

            int closest = (int)(((value - rangeMin) / (rangeMax - rangeMin)) * (double)(Increments - 1));
            return _cache[closest];
        }

    }
}
