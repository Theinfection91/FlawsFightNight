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
    public class RemoveDebugAdminHandler : CommandHandler
    {
        private AdminConfigurationService _adminConfigService;
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;

        public RemoveDebugAdminHandler(AdminConfigurationService adminConfigService, EmbedFactory embedFactory, GitBackupService gitBackupService) : base("Remove Debug Admin")
        {
            _adminConfigService = adminConfigService;
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
        }
        public async Task<Embed> RemoveDebugAdminProcess(ulong userId)
        {
            if (!_adminConfigService.IsDiscordIdInDebugAdminList(userId))
            {
                return _embedFactory.ErrorEmbed(Name, "User is not a Debug Admin.");
            }
            else
            {
                // This will also save the file
                await _adminConfigService.RemoveDiscordIdFromDebugAdminList(userId);

                // Backup to git repo
                _gitBackupService.EnqueueBackup();

                return _embedFactory.DebugAdminRemoveSuccess(userId);
            }
        }
    }
}
