using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.SettingsCommands
{
    public class RemoveFTPCredentialsLogic : Logic
    {
        private readonly AdminConfigurationService _configManager;
        private readonly EmbedFactory _embedManager;
        public RemoveFTPCredentialsLogic(AdminConfigurationService configManager, EmbedFactory embedManager) : base("Remove FTP Credentials")
        {
            _configManager = configManager;
            _embedManager = embedManager;
        }
        public async Task<Embed> RemoveFTPCredentialsProcess(string ftpServerName)
        {
            if (!_configManager.IsFTPCredentialsSet())
            {
                return _embedManager.ErrorEmbed("No FTP credentials are currently set.");
            }

            var ftpCredential = _configManager.GetFTPCredentials()!.FirstOrDefault(c => c.ServerName == ftpServerName);
            if (ftpCredential == null) 
            {
                return _embedManager.ErrorEmbed("Invalid FTP credential server name.");
            }

            await _configManager.RemoveFTPCredential(ftpCredential);
            //return _embedManager.GenericEmbed("FTP Credential Removed", $"FTP credential ({ftpCredential.Id} - {ftpCredential.ServerName} - {ftpCredential.Username} ({ftpCredential.IPAddress}:{ftpCredential.Port})) removed successfully.");
            return _embedManager.GenericEmbed("FTP Credential Removed", $"FTP credential ({ftpCredential.ServerName} - ({ftpCredential.IPAddress}:{ftpCredential.Port})) removed successfully.", Color.Green);
        }
    }
}
