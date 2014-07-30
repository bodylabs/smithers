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

        public void Serialize(object data, string path)
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

            JsonSerializer serializer = new JsonSerializer();

            try
            {
                using (StreamWriter streamWriter = new StreamWriter(path))
                {
                    using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter))
                    {
                        jsonWriter.Formatting = Formatting.Indented;
                        serializer.Serialize(jsonWriter, data);
                    }
                }
            }
            catch (IOException)
            {
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
