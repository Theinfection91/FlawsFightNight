using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.SettingsCommands
{
    public class RemoveDebugAdminLogic : Logic
    {
        private ConfigManager _configManager;
        private EmbedManager _embedManager;
        public RemoveDebugAdminLogic(ConfigManager configManager, EmbedManager embedManager) : base("Remove Debug Admin")
        {
            _configManager = configManager;
            _embedManager = embedManager;
        }
        public Embed RemoveDebugAdminProcess(ulong userId)
        {
            if (!_configManager.IsDiscordIdInDebugAdminList(userId))
            {
                return _embedManager.ErrorEmbed(Name, "User is not a Debug Admin.");
            }
            else
            {
                _configManager.RemoveDiscordIdFromDebugAdminList(userId);
                return _embedManager.DebugAdminRemoveSuccess(userId);
            }
        }
    }
}
