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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Smithers.Serialization.Formats
{
    public class ObjExporter
    {

        CameraSpacePoint[] _vertices;
        uint[] _indices;

        public ObjExporter(CameraSpacePoint[] vertices, uint[] indices)
        {
            _vertices = vertices;
            _indices = indices;
            IncludeFaces = true;
            IncludeVertices = true;
        }

        public bool IncludeVertices { get; set; }
        public bool IncludeFaces { get; set; }

        /// <summary>
        /// Write mesh defined by CameraSpacePoint and indices to stream in OBJ format
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="indices"></param>
        /// <returns></returns>
        public void Save(Stream stream)
        {
            CultureInfo cultureUS = CultureInfo.GetCultureInfo("en-US");

            using (StreamWriter writer = new StreamWriter(stream))
            {
                if (IncludeVertices)
                {
                    foreach (CameraSpacePoint vertex in _vertices)
                    {
                        writer.WriteLine(string.Format(cultureUS,
                                                       "v {0} {1} {2}",
                                                       vertex.X,
                                                       vertex.Y,
                                                       vertex.Z));
                    }
                }

                if (IncludeFaces)
                {
                    for (int i = 0; i < _indices.Length; i += 3)
                    {
                        writer.WriteLine(string.Format(cultureUS,
                                                       "f {0} {1} {2}",
                                                       _indices[i] + 1,
                                                       _indices[i + 1] + 1,
                                                       _indices[i + 2] + 1));
                    }
                }
            }
        }
    }
}
