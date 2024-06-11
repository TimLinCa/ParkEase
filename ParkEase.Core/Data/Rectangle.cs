using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ParkEase.Core.Data
{
    public class Rectangle
    {

        public int Index {  get; set; }

        public string Color {  get; set; }

        public RectF Rect {  get; set; }

        //red #E11919
        //green #009D00

        public Rectangle()
        {
            Color = "#009D00"; // Default color green
        }

        // Constructor with default color
        [JsonConstructor]
        public Rectangle(int index, RectF rect)
        {
            Index = index;
            Rect = rect;
            Color = "#009D00"; // Default color green
        }

        // Optional constructor to allow specifying a color
        public Rectangle(int index, RectF rect, string color)
        {
            Index = index;
            Rect = rect;
            Color = color;
        }
    }
}
