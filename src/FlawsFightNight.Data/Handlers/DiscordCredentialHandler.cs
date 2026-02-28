using FlawsFightNight.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Handlers
{
    public class DiscordCredentialHandler : AsyncDataHandler<DiscordCredentialFile>
    {
        public DiscordCredentialHandler() : base(PathOption.Credentials, "discord_credentials.json")
        {

        }
    }
}
