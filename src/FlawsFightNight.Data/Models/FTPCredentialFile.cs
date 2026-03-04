using FlawsFightNight.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.IO.Models
{
    [SafeForSerialization]
    public class FTPCredentialFile
    {
        public List<FTPCredential> FTPCredentials { get; set; } = [];
        public FTPCredentialFile() { }
    }
}
