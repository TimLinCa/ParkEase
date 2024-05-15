using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Model
{
    public class ParkEaseModel
    {
        public readonly bool developerMode;
        public ParkEaseModel(bool developerMode = true)
        {
            this.developerMode = developerMode;
        }
    }
}
