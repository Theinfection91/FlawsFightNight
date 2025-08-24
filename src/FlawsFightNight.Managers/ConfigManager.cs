using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace FlawsFightNight.Managers
{
    public class ConfigManager : BaseDataDriven
    {
        private DiscordSocketClient _client;
        public ConfigManager(DiscordSocketClient client, DataManager dataManager) : base("ConfigManager", dataManager)
        {
            _client = client;
        }

        #region Discord Config
        public void SetDiscordTokenProcess()
        {
            bool IsBotTokenProcessComplete = false;
            while (!IsBotTokenProcessComplete)
            {
                if (!IsValidBotTokenSet())
                {
                    Console.WriteLine($"{DateTime.Now} ConfigManager - Incorrect Bot Token found in Discord Credentials\\discord_credentials.json");
                    Console.WriteLine($"{DateTime.Now} SettingsManager - Please enter your Bot Token now (This can be changed manually in Discord Credentials\\discord_credentials.json as well if entered incorrectly and a connection can not be established): ");
                    string? botToken = Console.ReadLine();
                    if (IsValidBotToken(botToken))
                    {
                        SetDiscordToken(botToken);
                        IsBotTokenProcessComplete = true;
                    }
                    else
                    {
                        IsBotTokenProcessComplete = false;
                    }
                }
                else
                {
                    IsBotTokenProcessComplete = true;
                }
            }
        }

        public bool IsValidBotTokenSet()
        {
            return !string.IsNullOrEmpty(GetDiscordToken()) && GetDiscordToken() != "ENTER_BOT_TOKEN_HERE" && IsValidBotToken(GetDiscordToken());
        }

        public bool IsValidBotToken(string botToken)
        {
            return botToken.Length >= 59;
        }

        public void SetGuildIdProcess()
        {
            bool IsGuildIdProcessComplete = false;
            while (!IsGuildIdProcessComplete)
            {
                if (!IsGuildIdSet())
                {
                    Console.WriteLine($"{DateTime.Now} ConfigManager - Incorrect Guild Id found in Discord Credentials\\discord_credentials.json");
                    Console.WriteLine($"{DateTime.Now} ConfigManager - Please set a valid Guild ID for SlashCommands.");
                    Console.WriteLine($"{DateTime.Now} ConfigManager - Select a guild from the list below: ");
                    foreach (var guild in _client.Guilds)
                    {
                        Console.WriteLine($"Guild: {guild.Name} (ID: {guild.Id})");
                    }
                    string guildIdString = Console.ReadLine();
                    if (guildIdString != null)
                    {
                        if (ulong.TryParse(guildIdString.Trim(), out ulong guildId))
                        {
                            if (IsGuildIdValidBool(guildId))
                            {
                                SetGuildId(guildId);
                                IsGuildIdProcessComplete = true;
                            }
                            else
                            {
                                IsGuildIdProcessComplete = false;
                            }
                        }
                    }
                }
                else
                {
                    IsGuildIdProcessComplete = true;
                }
            }
        }

        public bool IsGuildIdSet()
        {
            return GetGuildId() != 0 && IsGuildIdValid();
        }

        public bool IsGuildIdValid()
        {
            return GetGuildId() >= 15;
        }

        public bool IsGuildIdValidBool(ulong guildId)
        {
            return guildId >= 15;
        }

        public string GetCommandPrefix()
        {
            return _dataManager.DiscordCredentialFile.CommandPrefix;
        }

        public void SetCommandPrefix(string prefix)
        {
            _dataManager.DiscordCredentialFile.CommandPrefix = prefix;
            _dataManager.SaveDiscordCredentialFile();
        }

        public string GetDiscordToken()
        {
            return _dataManager.DiscordCredentialFile.DiscordBotToken;
        }

        public void SetDiscordToken(string discordToken)
        {
            _dataManager.DiscordCredentialFile.DiscordBotToken = discordToken;
            _dataManager.SaveDiscordCredentialFile();
        }

        public ulong GetGuildId()
        {
            return _dataManager.DiscordCredentialFile.GuildId;
        }

        public void SetGuildId(ulong guildId)
        {
            _dataManager.DiscordCredentialFile.GuildId = guildId;
            _dataManager.SaveDiscordCredentialFile();
        }
        #endregion

        #region Permissions Config
        public bool IsDiscordIdInDebugAdminList(ulong discordId)
        {
            foreach (ulong id in _dataManager.PermissionsConfigFile.DebugAdminList)
            {
                if (id.Equals(discordId))
                {
                    return true;
                }
            }
            return false;
        }

        public void AddDiscordIdToDebugAdminList(ulong discordId)
        {
            _dataManager.PermissionsConfigFile.DebugAdminList.Add(discordId);
            _dataManager.SaveAndReloadPermissionsConfigFile();
        }

        public void RemoveDiscordIdFromDebugAdminList(ulong discordId)
        {
            _dataManager.PermissionsConfigFile.DebugAdminList.Remove(discordId);
            _dataManager.SaveAndReloadPermissionsConfigFile();
        }
        #endregion
    }
}
