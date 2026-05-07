using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class EditFTPServerNameHandler : CommandHandler
    {
        private readonly AdminConfigurationService _adminConfigService;
        private readonly DataContext _dataContext;
        private readonly EmbedFactory _embedFactory;

        public EditFTPServerNameHandler(AdminConfigurationService adminConfigurationService, DataContext dataContext, EmbedFactory embedFactory) : base("Edit FTP Server Name")
        {
            _adminConfigService = adminConfigurationService;
            _dataContext = dataContext;
            _embedFactory = embedFactory;
        }

        public async Task<Embed> Handle(string targetIPAddress, string newServerName)
        {
            if (string.IsNullOrEmpty(targetIPAddress))
            {
                return _embedFactory.ErrorEmbed(Name, "Target IP address cannot be empty.");
            }

            if (string.IsNullOrEmpty(newServerName) || newServerName.Length < 3)
            {
                return _embedFactory.ErrorEmbed(Name, "New server name must be at least 3 characters long.");
            }

            var ftpCredentials = _adminConfigService.GetFTPCredentials();
            if (ftpCredentials.Count == 0)
            {
                return _embedFactory.ErrorEmbed(Name, "No FTP credentials are currently set.");
            }
            if (!ftpCredentials.Any(c => c.IPAddress == targetIPAddress))
            {
                return _embedFactory.ErrorEmbed(Name, $"No FTP credentials found for IP address {targetIPAddress}.");
            }
            
            var credential = ftpCredentials.First(c => c.IPAddress == targetIPAddress);
            await _adminConfigService.RenameFTPCredentialServerName(credential, newServerName);

            // Filter results
            var statLogs = await _dataContext.GetAllStatLogs();
            for (int i = 0; i < statLogs.Count; i++)
            {
                var log = statLogs[i];
                if (log.IPAddress == targetIPAddress)
                {
                    log.ServerName = newServerName;
                    await _dataContext.SaveStatLogMatchResultFile(log);
                }
            }

            return _embedFactory.SuccessEmbed(Name, $"FTP server name for IP address {targetIPAddress} has been updated to {newServerName}.");
        }
    }
}
