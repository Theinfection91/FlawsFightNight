using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.SettingsCommands
{
    public class AddDebugAdminLogic : Logic
    {
        private ConfigManager _configManager;
        private EmbedManager _embedManager;
        public AddDebugAdminLogic(ConfigManager configManager, EmbedManager embedManager) : base("Add Debug Admin")
        {
            _configManager = configManager;
            _embedManager = embedManager;
        }

        public Embed AddDebugAdminProcess(ulong userId)
        {
            if (_configManager.IsDiscordIdInDebugAdminList(userId))
            {
                return _embedManager.ErrorEmbed(Name, "User is already a Debug Admin.");
            }
            else
            {
                _configManager.AddDiscordIdToDebugAdminList(userId);
                return _embedManager.ToDoEmbed("User has been added to the Debug Admin list. Need success embed.");
            }
        }
    }
}
