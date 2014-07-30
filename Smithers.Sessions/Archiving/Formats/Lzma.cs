using SevenZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Smithers.Sessions.Archiving.Formats
{
    public static class Lzma
    {
        static Lzma()
        {
            string libraryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Externals\\7z.dll");
            SevenZipCompressor.SetLibraryPath(libraryPath);
        }

        public static void Compress(string directory, string file)
        {
            var compressor = new SevenZipCompressor();
            compressor.CompressDirectory(directory, file);
        }

        public static void Extract(string file, string to)
        {
            using (var extractor = new SevenZipExtractor(file))
            {
                extractor.ExtractArchive(to);
            }
        }
    }
}
