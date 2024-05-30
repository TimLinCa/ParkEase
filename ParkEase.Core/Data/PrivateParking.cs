using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
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
        public string Id { get; set; }

        [BsonElement("companyName")]
        public string CompanyName { get; set; }

        [BsonElement("address")]
        public string Address { get; set; }

        [BsonElement("city")]
        public string City { get; set; }

        public ParkingInfo ParkingInfo { get; set; }

        public List<FloorInfo> FloorInfo { get; set; }

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