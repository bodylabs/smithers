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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Serialization
{
    class SavedItemTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SavedItemType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((value as SavedItemType).Name);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return SavedItemType.SavedItemTypeWithName((string)reader.Value);
        }
    }

    [JsonConverter(typeof(SavedItemTypeConverter))]
    public class SavedItemType
    {
        static readonly Dictionary<string, SavedItemType> _itemTypes = new Dictionary<string, SavedItemType>();

        // Singletons
        public static readonly SavedItemType CALIBRATION = new SavedItemType("Calibration");

        // Folders
        public static readonly SavedItemType COLOR_IMAGE = new SavedItemType("Color");
        public static readonly SavedItemType DEPTH_IMAGE = new SavedItemType("Depth");
        public static readonly SavedItemType DEPTH_MAPPING = new SavedItemType("DepthMapping");
        public static readonly SavedItemType INFRARED_IMAGE = new SavedItemType("Infrared");
        public static readonly SavedItemType SKELETON = new SavedItemType("Skeleton");
        public static readonly SavedItemType BODY_INDEX = new SavedItemType("BodyIndex");
        public static readonly SavedItemType HD_FACE = new SavedItemType("HDFace");

        public static SavedItemType SavedItemTypeWithName(string name)
        {
            if (_itemTypes.ContainsKey(name))
                return _itemTypes[name];
            else
                throw new ArgumentException(string.Format("Unrecognized type name: {0}", name));
        }

        string _name;

        public SavedItemType(string name)
        {
            _name = name;
            
            if (_itemTypes.ContainsKey(name))
                throw new ArgumentException("Duplicate type name");
            
            _itemTypes[name] = this;
        }

        public string Name { get { return _name; } }
    }
}
