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
        private GitBackupManager _gitBackupManager;

        public AddDebugAdminLogic(ConfigManager configManager, EmbedManager embedManager, GitBackupManager gitBackupManager) : base("Add Debug Admin")
        {
            _configManager = configManager;
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
        }

        public Embed AddDebugAdminProcess(ulong userId)
        {
            if (_configManager.IsDiscordIdInDebugAdminList(userId))
            {
                return _embedManager.ErrorEmbed(Name, "User is already a Debug Admin.");
            }
            else
            {
                // This will also save the file
                _configManager.AddDiscordIdToDebugAdminList(userId);

                // Backup to git repo
                _gitBackupManager.CopyAndBackupFilesToGit();

                return _embedManager.DebugAdminAddSuccess(userId);
            }
        }
    }
}
