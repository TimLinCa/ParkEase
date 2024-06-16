using CommunityToolkit.Mvvm.Messaging.Messages;
using ParkEase.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Messages
{
    public class UserChangedMessage : ValueChangedMessage<User>
    {
        public UserChangedMessage(User user) : base(user) 
        { 

        }
    }
}
