﻿using CommunityToolkit.Mvvm.ComponentModel;
using ParkEase.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Model
{
    public partial class ParkEaseModel
    { 
        public readonly bool developerMode;
        
        public User User { get; set; }

        public string PrivateMapId { get; set; }
        public ParkEaseModel(bool developerMode = true)
        {
            this.developerMode = developerMode;
        }
    }
}
