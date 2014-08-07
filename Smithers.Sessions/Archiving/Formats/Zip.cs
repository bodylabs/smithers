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
