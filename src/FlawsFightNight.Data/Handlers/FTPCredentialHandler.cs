using FlawsFightNight.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Handlers
{
    public class FTPCredentialHandler : BaseDataHandler<FTPCredentialFile>
    {
        public FTPCredentialHandler() : base("ftp_credentials.json", "Credentials")
        {

        }
    }
}
