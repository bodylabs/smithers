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
