using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Converters
{
    public class RectFConverter : JsonConverter<RectF>
    {
        public override RectF ReadJson(JsonReader reader, Type objectType, RectF existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);
            Dictionary<string, object> dictionary = jObject.ToObject<Dictionary<string, object>>();
            RectF rect = new RectF(float.Parse(dictionary["X"].ToString()), float.Parse(dictionary["Y"].ToString()), float.Parse(dictionary["Width"].ToString()), float.Parse(dictionary["Height"].ToString()));
            return rect;
        }


        public override void WriteJson(JsonWriter writer, RectF value, JsonSerializer serializer)
        {
            RectF rect = (RectF)value;
        }
    }
}
