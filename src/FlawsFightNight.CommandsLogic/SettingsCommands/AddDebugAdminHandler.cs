using Discord;
using FlawsFightNight.Commands;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands
{
    public class AddDebugAdminHandler : CommandHandler
    {
        private AdminConfigurationService _configManager;
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;

        public AddDebugAdminHandler(AdminConfigurationService configManager, EmbedFactory embedFactory, GitBackupService gitBackupService) : base("Add Debug Admin")
        {
            _configManager = configManager;
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
        }

        public async Task<Embed> AddDebugAdminProcess(ulong userId)
        {
            if (_configManager.IsDiscordIdInDebugAdminList(userId))
            {
                return _embedFactory.ErrorEmbed(Name, "User is already a Debug Admin.");
            }
            else
            {
                // This will also save the file
                await _configManager.AddDiscordIdToDebugAdminList(userId);

                // Backup to git repo
                _gitBackupService.EnqueueBackup();

                return _embedFactory.DebugAdminAddSuccess(userId);
            }
        }
    }
}
