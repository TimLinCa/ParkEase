
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Contracts.abstracts
{
    public abstract class ParkingLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty("_id")]
        public string Id { get; set; }
        public string AreaId { get; set; }
        public int Index { get; set; }
        public bool Status { get; set; }
        public DateTime Timestamp { get; set; }
        public string CamName { get; set; }
    }
}
