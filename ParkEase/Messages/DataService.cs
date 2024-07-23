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

        private static string code;

        public static string GetCode()
        {
            return code;
        }

        public static void SetCode(string value)
        {
            code = value;
        }

        private static string email;

        public static string GetEmail()
        {
            return email;
        }

        public static void SetEmail(string value)
        {
            email = value;
        }

    }

}
