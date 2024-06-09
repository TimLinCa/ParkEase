using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        [BsonElement("companyName")]
        public string CompanyName { get; set; }

        [BsonElement("address")]
        public string Address { get; set; }

        [BsonElement("city")]
        public string City { get; set; }

        [BsonElement("createdBy")]
        public string CreatedBy {  get; set; }

        public ParkingInfo ParkingInfo { get; set; }

        public List<FloorInfo> FloorInfo { get; set; }

        public bool ShouldSerializeId()
        {
            return false;
        }
    }

    public class ParkingInfo
    {
        public double Fee { get; set; }
        public double LimitedHour { get; set; }
    }

    public class FloorInfo
    {
        public string Floor { get; set; }
        public List<Rectangle> Rectangles { get; set; }

        public int NumberOfLot { get; set; }

        public byte[] ImageData { get; set; }

        public FloorInfo(string floor, List<Rectangle> rectangles, int numberOfLot, byte[] imageData)
        {
            Floor = floor;
            Rectangles = rectangles;
            NumberOfLot = numberOfLot;
            ImageData = imageData;
        }
    }
}