using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Messages
{
    public class DataService
    {

        private static string sharedId;

        public static string GetId()
        {
            return sharedId;
        }

        public static void SetId(string id)
        {
            sharedId = id;
        }

        private static Location sharedLocation;
        public static Location GetLocation() 
        {
            return sharedLocation;
        }
        public static void SetLocation(Location location)
        {
            sharedLocation = location;
        }

    }

}
