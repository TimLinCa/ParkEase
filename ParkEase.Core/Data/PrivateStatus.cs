using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Data
{
    public class PrivateStatus
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string AreaId { get; set; }
        public int Index { get; set; }
        public bool Status { get; set; }
        public string Floor { get; set; }
        public int LotId { get; set; }
    }
}
