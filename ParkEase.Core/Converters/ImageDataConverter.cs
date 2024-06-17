using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ParkEase.Core.Converters
{
    public class ImageDataConverter : JsonConverter<byte[]>
    {
        public override byte[]? ReadJson(JsonReader reader, Type objectType, byte[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);
            Dictionary<string, object> dictionary = jObject.ToObject<Dictionary<string, object>>();
            string binarySting = dictionary["Data"].ToString();
            byte[] bytes = Convert.FromBase64String(binarySting);

            return bytes;
        }

        public override void WriteJson(JsonWriter writer, byte[]? value, JsonSerializer serializer)
        {
            byte[] imageData = (byte[])value;
        }
    }
}
