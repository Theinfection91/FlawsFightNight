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
                    Console.WriteLine($"{DateTime.Now} - [ConfigManager] Incorrect Bot Token found in Discord Credentials\\discord_credentials.json");
                    Console.WriteLine($"{DateTime.Now} - [ConfigManager] Please enter your Bot Token now (This can be changed manually in Discord Credentials\\discord_credentials.json as well if entered incorrectly and a connection can not be established): ");
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
                    Console.WriteLine($"{DateTime.Now} - [ConfigManager] Incorrect Guild Id found in Discord Credentials\\discord_credentials.json");
                    Console.WriteLine($"{DateTime.Now} - [ConfigManager] Please set a valid Guild ID for SlashCommands.");
                    Console.WriteLine($"{DateTime.Now} - [ConfigManager] Select a guild from the list below: ");
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

        #region GitHub Config
        public bool IsGitPatTokenSet()
        {
            return _dataManager.GitHubCredentialFile.GitPatToken != "ENTER_GIT_PAT_TOKEN_HERE";
        }

        public void SetGitBackupProcess()
        {
            bool IsGitBackupProcessComplete = false;
            while (!IsGitBackupProcessComplete)
            {
                if (!IsGitPatTokenSet())
                {
                    Console.WriteLine($"{DateTime.Now} - [ConfigManager] Enter your Git PAT Token now if you want to have online backup storage through a GitHub repo you control.\nIf you wish to skip this feature for now, enter 0 for the PAT token.\nRefer to documentation for more help with the Git Backup Storage.");
                    string? gitPatToken = Console.ReadLine();
                    if (!gitPatToken.Equals("0") && gitPatToken.Length > 15)
                    {
                        _dataManager.GitHubCredentialFile.GitPatToken = gitPatToken;
                        Console.WriteLine($"{DateTime.Now} - [ConfigManager] Git PAT Token accepted. Now give the https url path to your Git repo. It will look something like this: https://github.com/YourUsername/YourGitStorageRepo.git");
                        string? gitUrlPath = Console.ReadLine();
                        _dataManager.GitHubCredentialFile.GitUrlPath = gitUrlPath;
                        Console.WriteLine($"{DateTime.Now} - [ConfigManager] Repo Url set to: {gitUrlPath}\nYou can manually change your token and url path in the Credentials/github_credentials.json file as well.");
                        _dataManager.SaveAndReloadGitHubCredentialFile();
                        IsGitBackupProcessComplete = true;
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now} - [ConfigManager] Git Backup Storage was not set up. You can manually change your token and url path in the Credentials/github_credentials.json file.");
                        IsGitBackupProcessComplete = true;
                    }
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now} - [ConfigManager] Non-default value found for GitPatToken in Credentials/github_credentials.json file. Skipping backup setup process. If you entered in the token or url incorrectly, you can manually change it in the Credentials/github_credentials.json file for now.");
                    IsGitBackupProcessComplete = true;
                }
            }
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
