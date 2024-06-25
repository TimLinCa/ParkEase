using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ParkEase.Messages
{
    public class PrivateIdChangedMessage : ValueChangedMessage<string>
    {
        public PrivateIdChangedMessage(string id) : base(id)
        {

        }
    }
}
