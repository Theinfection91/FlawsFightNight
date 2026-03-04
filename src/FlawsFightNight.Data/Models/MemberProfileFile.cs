using FlawsFightNight.Core.Attributes;
using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.IO.Models
{
    [SafeForSerialization]
    public class MemberProfileFile
    {
        public MemberProfile MemberProfile { get; set; }
        public MemberProfileFile() { }
    }
}
