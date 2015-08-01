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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Smithers.Sessions
{
    public class JSONHelper
    {
        #region Singleton

        private static volatile JSONHelper instance;
        private static object _syncRoot = new Object();

        private JSONHelper() { }

        public static JSONHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new JSONHelper();
                        }
                    }
                }

                return instance;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Serializing json from a dictionary
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string Serialize(Dictionary<string, string> dict)
        {
            return JsonConvert.SerializeObject(dict, Formatting.Indented);
        }

        public string Serialize(object data)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        /// <summary>
        /// This serialization helper will create the json file if not exists
        /// and throw any exception encountered
        /// </summary>
        /// <param name="data"></param>
        /// <param name="path"></param>
        public void Serialize(object data, string path)
        {
            if (!File.Exists(path))
            {
                string directory = Path.GetDirectoryName(path);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            JsonSerializer serializer = new JsonSerializer();

            using (StreamWriter streamWriter = new StreamWriter(path))
            {
                using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter))
                {
                    jsonWriter.Formatting = Formatting.Indented;
                    serializer.Serialize(jsonWriter, data);
                }
            }

        }

        /// <summary>
        /// Deserialize Helper
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public T DeserializeObject<T>(string path) where T : new()
        {
            if (!File.Exists(path))
            {
                string directory = Path.GetDirectoryName(path);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    File.Create(path);
                }
            }

            using (TextReader file = File.OpenText(path))
            {
                JsonSerializer serializer = new JsonSerializer();
                T result = (T)serializer.Deserialize(file, typeof(T));

                if (result == null)
                {
                    result = new T();
                }

                return result;
            }
            
        }

        public List<T> Deserialize<T>(string path)
        {
            if (!File.Exists(path))
            {
                string directory = Path.GetDirectoryName(path);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    File.Create(path);
                }
            }

            try
            {
                using (TextReader file = File.OpenText(path))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    List<T> scans = (List<T>)serializer.Deserialize(file, typeof(List<T>));

                    if (scans == null)
                    {
                        scans = new List<T>();
                    }

                    return scans;
                }
            }
            catch (IOException)
            {
                return new List<T>();
            }
        }

        public Dictionary<string, string> CreateDictionary(string json)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        #endregion
    }
}
