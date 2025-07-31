using FlawsFightNight.Data.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Handlers
{
    public class DiscordCredentialHandler : BaseDataHandler<DiscordCredentialFile>
    {
        public DiscordCredentialHandler() : base("discord_credentials.json", "Discord Credentials")
        {

        }
    }
}
