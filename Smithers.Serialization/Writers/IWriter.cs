using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Serialization.Writers
{
    public interface IWriter
    {
        SavedItemType Type { get; }

        /// <summary>
        /// The recommended file extension, including the ".".
        /// e.g. ".png"
        /// </summary>
        string FileExtension { get; }

        TimeSpan? Timestamp { get; }

        void Write(Stream stream);
    }
}
