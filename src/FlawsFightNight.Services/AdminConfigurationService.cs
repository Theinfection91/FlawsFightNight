using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using FlawsFightNight.IO.Models;

namespace FlawsFightNight.Services
{
    public class AdminConfigurationService : BaseDataDriven
    {
        private DiscordSocketClient _client;

        private bool _ftpDebugMode = true;
        public event EventHandler? FTPCredentialsChanged;
        public event EventHandler? CancelFTPSetupProcess;
        private CancellationTokenSource? _ftpSetupCts;
        public AdminConfigurationService(DiscordSocketClient client, DataContext dataManager) : base("AdminConfigurationService", dataManager)
        {
            _client = client;

            //AddFTPCredential(CreateFTPCredential("Chicago Server", "127.0.0.1", 21, "sho_chi", "password1", "/placeholderDir/anotherDir/UserLogs")?.Result)?.Wait();
            //AddFTPCredential(CreateFTPCredential("New York Server", "127.0.0.1", 21, "sho_ny", "password1", "/thisDir/anotherDir/oneMoreDir/UserLogs")?.Result)?.Wait();  
        }

        #region Discord Config
        public async Task SetDiscordTokenProcess()
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
                        await SetDiscordToken(botToken);
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

        public async Task SetGuildIdProcess()
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
                                await SetGuildId(guildId);
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
            return _dataContext.DiscordCredentialFile.CommandPrefix;
        }

        public async Task SetCommandPrefix(string prefix)
        {
            _dataContext.DiscordCredentialFile.CommandPrefix = prefix;
            await _dataContext.SaveDiscordCredentialFile();
        }

        public string GetDiscordToken()
        {
            return _dataContext.DiscordCredentialFile.DiscordBotToken;
        }

        public async Task SetDiscordToken(string discordToken)
        {
            _dataContext.DiscordCredentialFile.DiscordBotToken = discordToken;
            await _dataContext.SaveDiscordCredentialFile();
        }

        public ulong GetGuildId()
        {
            return _dataContext.DiscordCredentialFile.GuildId;
        }

        public async Task SetGuildId(ulong guildId)
        {
            _dataContext.DiscordCredentialFile.GuildId = guildId;
            await _dataContext.SaveDiscordCredentialFile();
        }
        #endregion

        #region GitHub Config
        public bool IsGitPatTokenSet()
        {
            return _dataContext.GitHubCredentialFile.GitPatToken != "ENTER_GIT_PAT_TOKEN_HERE";
        }

        public async Task SetGitBackupProcess()
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
                        _dataContext.GitHubCredentialFile.GitPatToken = gitPatToken;
                        Console.WriteLine($"{DateTime.Now} - [ConfigManager] Git PAT Token accepted. Now give the https url path to your Git repo. It will look something like this: https://github.com/YourUsername/YourGitStorageRepo.git");
                        string? gitUrlPath = Console.ReadLine();
                        _dataContext.GitHubCredentialFile.GitUrlPath = gitUrlPath;
                        Console.WriteLine($"{DateTime.Now} - [ConfigManager] Repo Url set to: {gitUrlPath}\nYou can manually change your token and url path in the Credentials/github_credentials.json file as well.");
                        await _dataContext.SaveAndReloadGitHubCredentialFile();
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
            foreach (ulong id in _dataContext.PermissionsConfigFile.DebugAdminList)
            {
                if (id.Equals(discordId))
                {
                    return true;
                }
            }
            return false;
        }

        public async Task AddDiscordIdToDebugAdminList(ulong discordId)
        {
            _dataContext.PermissionsConfigFile.DebugAdminList.Add(discordId);
            await _dataContext.SaveAndReloadPermissionsConfigFile();
        }

        public async Task RemoveDiscordIdFromDebugAdminList(ulong discordId)
        {
            _dataContext.PermissionsConfigFile.DebugAdminList.Remove(discordId);
            await _dataContext.SaveAndReloadPermissionsConfigFile();
        }
        #endregion

        #region FTP Config
        public void NotifyFTPCredentialsChanged()
        {
            try
            {
                FTPCredentialsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - [ConfigManager] Error notifying FTP credentials change: {ex.Message}");

            }
        }

        public void NotifyCancelFTPSetupProcess()
        {
            try
            {
                // Signal any subscribers
                CancelFTPSetupProcess?.Invoke(this, EventArgs.Empty);

                // Cancel the currently running FTP setup (if any)
                try
                {
                    _ftpSetupCts?.Cancel();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now} - [ConfigManager] Error cancelling FTP setup token: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - [ConfigManager] Error notifying cancel FTP setup process: {ex.Message}");
            }
        }

        public bool IsFTPCredentialsSet()
        {
            return _dataContext.FTPCredentialFile.FTPCredentials.Count > 0;
        }

        public async Task AddFTPCredential(FTPCredential credential)
        {
            foreach (var existingCredential in _dataContext.FTPCredentialFile.FTPCredentials)
            {
                if (existingCredential.ServerName.Equals(credential.ServerName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"{DateTime.Now} - [ConfigManager] An FTP credential with the server name '{credential.ServerName}' already exists. Please choose a different server name.");
                    return;
                }
                if (existingCredential.IPAddress != null && credential.IPAddress != null && existingCredential.IPAddress.Equals(credential.IPAddress, StringComparison.OrdinalIgnoreCase) && _ftpDebugMode == false)
                {
                    Console.WriteLine($"{DateTime.Now} - [ConfigManager] An FTP credential with the IP address '{credential.IPAddress}' already exists. Please choose a different IP address.");
                    return;
                }
            }
            _dataContext.FTPCredentialFile.FTPCredentials.Add(credential);
            await _dataContext.SaveAndReloadFTPCredentialFile();
            NotifyFTPCredentialsChanged();
        }

        public async Task<FTPCredential>? CreateFTPCredential(string serverName, string? ipAddress, int port, string? username, string? password, string? userLogsDirectoryPath)
        {
            var newCredential = new FTPCredential()
            {
                Id = _dataContext.FTPCredentialFile.FTPCredentials.Count + 1,
                ServerName = serverName,
                IPAddress = ipAddress,
                Port = port,
                Username = username,
                Password = password,
                UserLogsDirectoryPath = userLogsDirectoryPath
            };
            return newCredential;
        }

        public List<FTPCredential>? GetFTPCredentials()
        {
            return _dataContext.FTPCredentialFile.FTPCredentials;
        }

        public async Task RemoveFTPCredential(FTPCredential credential)
        {
            _dataContext.FTPCredentialFile.FTPCredentials.Remove(credential);
            await _dataContext.SaveAndReloadFTPCredentialFile();
            NotifyFTPCredentialsChanged();
        }

        public async Task FTPSetupProcess(bool isUserInit = false)
        {
            if (!IsFTPCredentialsSet() || isUserInit)
            {
                _ftpSetupCts = new CancellationTokenSource();
                var token = _ftpSetupCts.Token;

                Console.WriteLine($"{DateTime.Now} - [ConfigManager] No FTP credentials found in Credentials/ftp_credentials.json file. If you want to use FTP features, please enter in your FTP credential information now.");
                Console.WriteLine($"{DateTime.Now} - [ConfigManager] If you want to skip this for now, simply enter 0 for the server name when prompted.");
                bool IsFTPSetupComplete = false;

                // Replace the previous ReadLineCancelableAsync implementation with this polling-based, cancellation-aware reader.
                // It uses Console.KeyAvailable + Console.ReadKey so the read can be stopped immediately when the CTS is cancelled.
                static async Task<string?> ReadLineCancelableAsync(CancellationToken ct)
                {
                    var sb = new StringBuilder();
                    try
                    {
                        while (!ct.IsCancellationRequested)
                        {
                            // Wait until a key is available or cancellation requested
                            while (!Console.KeyAvailable)
                            {
                                // small delay to yield and let cancellation be observed
                                await Task.Delay(50, ct);
                            }

                            // A key is available, read it
                            var keyInfo = Console.ReadKey(intercept: true);

                            if (keyInfo.Key == ConsoleKey.Enter)
                            {
                                Console.WriteLine(); // echo newline
                                return sb.ToString();
                            }
                            else if (keyInfo.Key == ConsoleKey.Backspace)
                            {
                                if (sb.Length > 0)
                                {
                                    // remove last char from buffer and erase from console
                                    sb.Length--;
                                    Console.Write("\b \b");
                                }
                            }
                            else
                            {
                                // normal character
                                sb.Append(keyInfo.KeyChar);
                                Console.Write(keyInfo.KeyChar); // echo
                            }
                        }
                    }
                    catch (OperationCanceledException) { /* fall through */ }

                    // If cancelled, throw so callers can handle consistently
                    throw new OperationCanceledException(ct);
                }

                try
                {
                    while (!IsFTPSetupComplete)
                    {
                        Console.WriteLine($"{DateTime.Now} - [ConfigManager] Enter a server name for this FTP credential (This is just a name to identify the credential, it does not have to match anything on the actual FTP server): ");
                        string? serverName;
                        try
                        {
                            serverName = await ReadLineCancelableAsync(token);
                        }
                        catch (OperationCanceledException)
                        {
                            Console.WriteLine($"{DateTime.Now} - [ConfigManager] FTP setup cancelled before server name entry.");
                            break;
                        }

                        if (serverName != null && serverName.Equals("0"))
                        {
                            Console.WriteLine($"{DateTime.Now} - [ConfigManager] FTP setup skipped. You can manually add your FTP credentials later by editing the Credentials/ftp_credentials.json file or by using the AddFTPCredential method in this ConfigManager class.");
                            IsFTPSetupComplete = true;
                        }
                        else
                        {
                            Console.WriteLine($"{DateTime.Now} - [ConfigManager] Enter the IP address for this FTP credential (This should be the actual IP address of the FTP server): ");
                            string? ipAddress;
                            try
                            {
                                ipAddress = await ReadLineCancelableAsync(token);
                            }
                            catch (OperationCanceledException)
                            {
                                Console.WriteLine($"{DateTime.Now} - [ConfigManager] FTP setup cancelled while reading IP address.");
                                break;
                            }

                            Console.WriteLine($"{DateTime.Now} - [ConfigManager] Enter the port number for this FTP credential (This should be the actual port number of the FTP server, default is usually 21): ");
                            int port;
                            try
                            {
                                var portStr = await ReadLineCancelableAsync(token);
                                port = int.TryParse(portStr, out var p) ? p : 21;
                            }
                            catch (OperationCanceledException)
                            {
                                Console.WriteLine($"{DateTime.Now} - [ConfigManager] FTP setup cancelled while reading port.");
                                break;
                            }

                            Console.WriteLine($"{DateTime.Now} - [ConfigManager] Enter the username for this FTP credential (This should be the actual username for the FTP server): ");
                            string? username;
                            try
                            {
                                username = await ReadLineCancelableAsync(token);
                            }
                            catch (OperationCanceledException)
                            {
                                Console.WriteLine($"{DateTime.Now} - [ConfigManager] FTP setup cancelled while reading username.");
                                break;
                            }

                            Console.WriteLine($"{DateTime.Now} - [ConfigManager] Enter the password for this FTP credential (This should be the actual password for the FTP server): ");
                            string? password;
                            try
                            {
                                password = await ReadLineCancelableAsync(token);
                            }
                            catch (OperationCanceledException)
                            {
                                Console.WriteLine($"{DateTime.Now} - [ConfigManager] FTP setup cancelled while reading password.");
                                break;
                            }

                            Console.WriteLine($"{DateTime.Now} - [ConfigManager] Enter the user logs directory path for this FTP credential (This should be the directory path on the FTP server where user logs are stored): ");
                            string? userLogsDirectoryPath;
                            try
                            {
                                userLogsDirectoryPath = await ReadLineCancelableAsync(token);
                            }
                            catch (OperationCanceledException)
                            {
                                Console.WriteLine($"{DateTime.Now} - [ConfigManager] FTP setup cancelled while reading user logs directory.");
                                break;
                            }

                            var newCredential = await CreateFTPCredential(serverName!, ipAddress, port, username, password, userLogsDirectoryPath);
                            if (newCredential != null)
                            {
                                await AddFTPCredential(newCredential);
                                Console.WriteLine($"{DateTime.Now} - [ConfigManager] FTP credential for server '{serverName}' added successfully.");
                            }

                            Console.WriteLine($"{DateTime.Now} - [ConfigManager] Do you want to add another FTP credential? (y/n): ");
                            try
                            {
                                string? addAnother = await ReadLineCancelableAsync(token);
                                if (addAnother != null && addAnother.Equals("y", StringComparison.OrdinalIgnoreCase))
                                {
                                    IsFTPSetupComplete = false;
                                }
                                else
                                {
                                    Console.WriteLine($"{DateTime.Now} - [ConfigManager] FTP setup complete. You can always add more credentials later by running the FTP Setup Discord Command then come back to the console.");
                                    IsFTPSetupComplete = true;
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                Console.WriteLine($"{DateTime.Now} - [ConfigManager] FTP setup cancelled while asking to add another.");
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    try { _ftpSetupCts?.Dispose(); } catch { }
                    _ftpSetupCts = null;
                }
            }
        }
        #endregion
    }
}
