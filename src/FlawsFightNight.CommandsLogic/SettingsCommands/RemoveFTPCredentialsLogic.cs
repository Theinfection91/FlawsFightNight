using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.SettingsCommands
{
    public class RemoveFTPCredentialsLogic : Logic
    {
        private EmbedManager _embedManager;
        public RemoveFTPCredentialsLogic(EmbedManager embedManager) : base("Remove FTP Credentials")
        {
            _embedManager = embedManager;
        }
        public Embed RemoveFTPCredentialsProcess()
        {
            return _embedManager.ToDoEmbed();
        }
    }
}
