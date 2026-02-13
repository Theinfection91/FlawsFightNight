using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.DataModels
{
    public class FTPCredential
    {
        public string? IPAddress { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? UserLogsDirectory {  get; set; }

        public FTPCredential() { }
    }
}
