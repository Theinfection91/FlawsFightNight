using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands
{
    public class RemoveFTPCredentialsHandler : CommandHandler
    {
        private readonly AdminConfigurationService _adminConfigService;
        private readonly EmbedFactory _embedFactory;
        public RemoveFTPCredentialsHandler(AdminConfigurationService adminConfigService, EmbedFactory embedFactory) : base("Remove FTP Credentials")
        {
            _adminConfigService = adminConfigService;
            _embedFactory = embedFactory;
        }
        public async Task<Embed> RemoveFTPCredentialsProcess(string ftpServerName)
        {
            if (!_adminConfigService.IsFTPCredentialsSet())
            {
                return _embedFactory.ErrorEmbed("No FTP credentials are currently set.");
            }

            var ftpCredential = _adminConfigService.GetFTPCredentials()!.FirstOrDefault(c => c.ServerName == ftpServerName);
            if (ftpCredential == null) 
            {
                return _embedFactory.ErrorEmbed("Invalid FTP credential server name.");
            }

            await _adminConfigService.RemoveFTPCredential(ftpCredential);
            //return _embedFactory.GenericEmbed("FTP Credential Removed", $"FTP credential ({ftpCredential.Id} - {ftpCredential.ServerName} - {ftpCredential.Username} ({ftpCredential.IPAddress}:{ftpCredential.Port})) removed successfully.");
            return _embedFactory.GenericEmbed("FTP Credential Removed", $"FTP credential ({ftpCredential.ServerName} - ({ftpCredential.IPAddress}:{ftpCredential.Port})) removed successfully.", Color.Green);
        }
    }
}
