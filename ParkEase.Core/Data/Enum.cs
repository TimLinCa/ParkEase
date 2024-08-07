﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Data
{
    public enum Roles
    {
        User,
        Administrator,
        Engineer,
        Developer
    }


    public enum AreaType
    {   
        Public,
		Private
	}   

    public enum TimeInterval
    {
        Hourly,
        Daily,
        Monthly,
    }

    public enum TravelMode
    {
        Driving,
        Walking,
        Bicycling,
        Transit
    }
}
