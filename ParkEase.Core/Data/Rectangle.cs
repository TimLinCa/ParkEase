using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Data
{
    public class Rectangle
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public int id;

        public string Index {  get; set; }

        public RectF Rect {  get; set; }
    }
}
