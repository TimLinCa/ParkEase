using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Data
{
    public class Rectangle
    {

        public int Index {  get; set; }

        public RectF Rect {  get; set; }

        public Rectangle(int index, RectF rect) 
        {
            Index = index;
            Rect = rect;
        }
    }
}
