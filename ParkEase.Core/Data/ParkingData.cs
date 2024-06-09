using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ParkEase.Core.Data
{
    public class ParkingData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty("_id")]
        public string Id { get; set; }

        public List<MapPoint> Points { get; set; }

        public string ParkingSpot { get; set; }
        public string ParkingTime { get; set; }
        public string ParkingFee { get; set; }
        public string ParkingCapacity { get; set; }
        public Roles Role { get; set; } = Roles.Administrator;

        //ShouldSerialize{PropertyName} well tell JsonConvert.SerializeObject if it skips the {PropertyName} property during serialization.
        public bool ShouldSerializeId()
        {
            return false;
        }
    }

    // This class represents a point on the map with latitude and longitude
    public class MapPoint : IEquatable<MapPoint>
    {
        public string Lat { get; set; }
        public string Lng { get; set; }


        // Determines whether the specified MapPoint is equal to the current MapPoint
        public bool Equals(MapPoint? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (other.Lat == this.Lat && other.Lng == this.Lng) return true;
            return false;
        }
    }
}
