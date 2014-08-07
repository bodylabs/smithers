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
using System.IO;
using System.Linq;
using System.Text;

namespace Smithers.Serialization.Formats
{
    public class Blkd
    {
        public const byte LATEST_VERSION = 2;

        public byte Version { get; set; }
        public UInt16 Width { get; set; }
        public UInt16 Height { get; set; }
        public byte BytesPerPixel { get; set; }
        public byte[] Data { get; set; }

        public Blkd() { }

        public Blkd(UInt16 width, UInt16 height, byte bytesPerPixel, byte[] data) : this()
        {
            Width = width;
            Height = height;
            BytesPerPixel = bytesPerPixel;
            Data = data;
        }

        public static Blkd Load(string fileName)
        {
            if (!File.Exists(fileName)) return null;

            Blkd blkd = new Blkd();

            using (FileStream stream = new FileStream(fileName, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // 1. Magic number
                reader.ReadChars(4);

                // 2. Version
                blkd.Version = reader.ReadByte();

                // 3. Dimensions
                blkd.Width = reader.ReadUInt16();
                blkd.Height = reader.ReadUInt16();

                // 4. Bytes per pixel
                if (blkd.Version == 1)
                    blkd.BytesPerPixel = 2; // one short per pixel
                else if (blkd.Version == 2)
                    blkd.BytesPerPixel = reader.ReadByte();
                else
                    throw new InvalidDataException(string.Format("Unsupported version {0} -- we support 1 or 2", blkd.Version));

                // 5. Depth data
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    reader.BaseStream.CopyTo(memoryStream);

                    long expectedLength = blkd.Width * blkd.Height * blkd.BytesPerPixel;
                    if (memoryStream.Length != expectedLength)
                        throw new InvalidDataException(string.Format("Data is {0} bytes, expected {1}", memoryStream.Length, expectedLength));

                    blkd.Data = memoryStream.ToArray();
                }
            }

            return blkd;
        }

        public static void Save(Blkd blkd, Stream stream, byte version = LATEST_VERSION)
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // 1. Magic number
                writer.Write((char)'B');
                writer.Write((char)'L');
                writer.Write((char)'K');
                writer.Write((char)'D');

                // 2. Version
                blkd.Version = version;
                writer.Write(version);

                // 3. Dimensions
                writer.Write(blkd.Width);
                writer.Write(blkd.Height);

                // 4. Bytes per pixel
                if (version == 1)
                {
                    if (blkd.BytesPerPixel != 2)
                        throw new InvalidDataException(string.Format("BLKD version 1 supports exactly 2 bytes per pixel, not {0}", blkd.BytesPerPixel));
                }
                else if (version == 2)
                {
                    writer.Write(version);
                }
                else
                {
                    throw new InvalidDataException(string.Format("Unsupported version {0} -- we support 1 or 2", version));
                }

                // 4. Depth data
                long expectedLength = blkd.Width * blkd.Height * blkd.BytesPerPixel;
                if (blkd.Data.LongLength < expectedLength)
                    throw new InvalidDataException(string.Format("Data is {0} bytes, expected at least {1}", blkd.Data.LongLength, expectedLength));

                // Write only the desired number of bytes. This provides more flexibility, since
                // it allows reusing the buffer for data with different dimensions.
                if (expectedLength > int.MaxValue)
                    throw new ArgumentException(string.Format("FIXME, This doesn't handle data buffers longer than {0}", int.MaxValue));
                writer.Write(blkd.Data, 0, (int)expectedLength);

                writer.Close();
            }
        }

        public static void Save(Blkd blkd, string fileName, byte version = LATEST_VERSION)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Create))
            {
                Blkd.Save(blkd, stream, version);
                stream.Close();
            }
        }

        public void Save(Stream stream, byte version = LATEST_VERSION)
        {
            Save(this, stream, version);
        }

        public void Save(string fileName, byte version = LATEST_VERSION)
        {
            Save(this, fileName, version);
        }
    }
}
