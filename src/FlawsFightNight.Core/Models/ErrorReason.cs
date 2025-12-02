using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class ErrorReason
    {
        public string Info { get; set; }
        public ErrorReason(string reason)
        {
            Info = reason;
        }
    }
}
