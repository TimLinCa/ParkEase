using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using ParkEase.Core.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Data
{
    public class PrivateParking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty("_id")]
        public string Id { get; set; }

        public string CompanyName { get; set; }

        public string Address { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string CreatedBy {  get; set; }

        public ParkingInfo ParkingInfo { get; set; }

        public List<FloorInfo> FloorInfo { get; set; } = new List<FloorInfo>();

        public bool ShouldSerializeId()
        {
            return false;
        }
    }

    public class ParkingInfo
    {
        public double Fee { get; set; }
        public int LimitedHour { get; set; }
    }

    public class FloorInfo
    {
        public string Floor { get; set; }
        public List<Rectangle> Rectangles { get; set; }
        
        [JsonConverter(typeof(ImageDataConverter))]
        public byte[] ImageData { get; set; }

        public FloorInfo(string floor, List<Rectangle> rectangles, byte[] imageData)
        {
            Floor = floor;
            Rectangles = rectangles;
            ImageData = imageData;
        }
    }
}