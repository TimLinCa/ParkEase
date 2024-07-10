using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using ParkEase.Core.Contracts.abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Data
{
    public class PrivateLog : ParkingLog
    {
        public string Floor { get; set; }
        public int LotId { get; set; }
    }
}
