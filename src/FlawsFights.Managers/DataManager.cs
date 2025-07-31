using FlawsFightNight.Data.DataModels;
using FlawsFightNight.Data.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class DataManager
    {
        #region Fields and Constructor
        public string Name { get; set; } = "DataManager";

        // Discord Credential File
        public DiscordCredentialFile DiscordCredentialFile { get; private set; }
        private readonly DiscordCredentialHandler _discordCredentialHandler;

        #endregion
    }
}
