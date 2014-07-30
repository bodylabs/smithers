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
