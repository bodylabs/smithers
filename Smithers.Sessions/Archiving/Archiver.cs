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

using Smithers.Sessions.Archiving.Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Sessions.Archiving
{
    public class ArchiveResult
    {
        public bool Success { get; set; }
        public Exception Exception { get; set; }
    }

    public class Archiver
    {
        bool _started;
        ArchiveResult _result;

        public bool Success { get { return _result == null ? false : _result.Success; } }
        public ArchiveResult Result { get { return _result; } }

        public async Task<ArchiveResult> PerformArchive(string scanDirectory, string target)
        {
            if (_started)
                throw new InvalidOperationException("Archiver was already started");

            _started = true;

            _result = await Task.Run<ArchiveResult>(() => this.Archive(scanDirectory, target));
            return _result;
        }

        private ArchiveResult Archive(string scanDirectory, string target)
        {
            try
            {
                string tempFile = Path.GetTempFileName();

                try
                {
                    Lzma.Compress(scanDirectory, tempFile);
                    File.Copy(tempFile, target);
                }
                finally
                {
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                }

                return new ArchiveResult { Success = true };
            }
            catch (Exception e)
            {
                return new ArchiveResult { Success = false, Exception = e };
            }
        }
    }
}
