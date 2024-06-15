﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
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

        public string City { get; set; }

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
        public int LimitedHour { get; set; }
    }

    public class FloorInfo
    {
        public string Floor { get; set; }
        public List<Rectangle> Rectangles { get; set; }

        public byte[] ImageData { get; set; }

        public FloorInfo(string floor, List<Rectangle> rectangles, byte[] imageData)
        {
            Floor = floor;
            Rectangles = rectangles;
            ImageData = imageData;
        }
    }
}