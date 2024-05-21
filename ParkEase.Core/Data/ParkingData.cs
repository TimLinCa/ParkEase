using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ParkEase.Core.Data
{
    public class ParkingData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ParkingId { get; set; }

        public string ParkingSpot { get; set; }
        public string ParkingTime { get; set; }
        public string ParkingFee { get; set; }
        public string ParkingCapacity { get; set; }
        public Roles Role { get; set; } = Roles.Administrator;
    }
}
