using FlawsFightNight.IO.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.IO.Handlers
{
    public class FTPCredentialHandler : AsyncDataHandler<FTPCredentialFile>
    {
        public FTPCredentialHandler() : base("ftp_credentials.json", "Credentials")
        {

        }
    }
}
