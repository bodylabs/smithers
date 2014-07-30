using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Sessions.Archiving.Formats
{
    /// <summary>
    /// Utility class for compressing and decompressing directories.
    /// </summary>
    public static class Zip
    {
        /// <summary>
        /// Compresses the specified directory and generates a .zip file.
        /// </summary>
        /// <param name="directory">The full path to the desired directory.</param>
        /// <param name="file">The .zip file name, including path and extension.</param>
        public static void Compress(string directory, string file)
        {
            ZipFile.CreateFromDirectory(directory, file, System.IO.Compression.CompressionLevel.Optimal, true);
        }

        /// <summary>
        /// Extracts the contents of the specified .zip file to the specified directory.
        /// </summary>
        /// <param name="file">The .zip file name, including path and extension.</param>
        /// <param name="to">The full path to the desired directory.</param>
        public static void Extract(string file, string to)
        {
            ZipFile.ExtractToDirectory(file, to);
        }
    }
}
