using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands
{
    public class RemoveFTPCredentialsLogic : CommandHandler
    {
        private readonly AdminConfigurationService _configManager;
        private readonly EmbedFactory _embedFactory;
        public RemoveFTPCredentialsLogic(AdminConfigurationService configManager, EmbedFactory embedFactory) : base("Remove FTP Credentials")
        {
            _configManager = configManager;
            _embedFactory = embedFactory;
        }
        public async Task<Embed> RemoveFTPCredentialsProcess(string ftpServerName)
        {
            if (!_configManager.IsFTPCredentialsSet())
            {
                return _embedFactory.ErrorEmbed("No FTP credentials are currently set.");
            }

            var ftpCredential = _configManager.GetFTPCredentials()!.FirstOrDefault(c => c.ServerName == ftpServerName);
            if (ftpCredential == null) 
            {
                return _embedFactory.ErrorEmbed("Invalid FTP credential server name.");
            }

            await _configManager.RemoveFTPCredential(ftpCredential);
            //return _embedFactory.GenericEmbed("FTP Credential Removed", $"FTP credential ({ftpCredential.Id} - {ftpCredential.ServerName} - {ftpCredential.Username} ({ftpCredential.IPAddress}:{ftpCredential.Port})) removed successfully.");
            return _embedFactory.GenericEmbed("FTP Credential Removed", $"FTP credential ({ftpCredential.ServerName} - ({ftpCredential.IPAddress}:{ftpCredential.Port})) removed successfully.", Color.Green);
        }
    }
}
