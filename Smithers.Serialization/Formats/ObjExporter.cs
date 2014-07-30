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
