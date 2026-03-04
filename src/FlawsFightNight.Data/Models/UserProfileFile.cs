using FlawsFightNight.Core.Attributes;
using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Models
{
    [SafeForSerialization]
    public class UserProfileFile
    {
        public UserProfile UserProfile { get; set; }
        public UserProfileFile() { }
    }
}
