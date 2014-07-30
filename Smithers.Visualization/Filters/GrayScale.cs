using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Visualization.Filters
{
    class GrayScale
    {
        static private byte[] _cache;

        static private readonly int Increments = 65536;

        static GrayScale()
        {
            _cache = new byte[Increments];
            for (int i = 0; i < Increments; ++i)
            {
                _cache[i] = (byte)(255.0 * Math.Pow((double)(i >> 8) / 255, 0.4));
            }
        }

        public static byte Intensity(uint value)
        {
            if (value < 0) return _cache.First();
            if (value >= 65536) return _cache.Last();

            return _cache[value];
        }
    }
}
