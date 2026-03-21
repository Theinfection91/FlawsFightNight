using Discord;
using Discord.WebSocket;
using FlawsFightNight.IO.Models;
using FluentFTP;
using FluentFTP.Exceptions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Services
{
    public class FTPStatsService : BackgroundService
    {
        private readonly AdminConfigurationService _adminConfigService;
        private readonly GitBackupService _gitBackupService;
        private readonly UT2004StatsService _ut2004StatsService;

        private readonly DiscordSocketClient _discordClient;
        private Dictionary<FTPCredential, AsyncFtpClient> _ftpClients = new();
        private bool IsClientsConfigured = false;

        public FTPStatsService(AdminConfigurationService adminConfigService, DiscordSocketClient client, GitBackupService gitBackupService, UT2004StatsService ut2004StatsService)
        {
            _adminConfigService = adminConfigService;
            _gitBackupService = gitBackupService;
            _ut2004StatsService = ut2004StatsService;

            _discordClient = client;

            ConfigureFTPClients();
            _adminConfigService.FTPCredentialsChanged += OnFTPCredentialsChanged!;
        }

        private void OnFTPCredentialsChanged(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now} - [FTPStatsService] FTP credentials changed. Reconfiguring FTP clients...");
                ConfigureFTPClients();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Error reconfiguring FTP clients: {ex.Message}");
            }
        }

        public void ConfigureFTPClients()
        {
            if (!_adminConfigService.IsFTPCredentialsSet())
            {
                Console.WriteLine($"{DateTime.Now} - [FTPStatsService] FTP credentials are not set. Please rerun FTP setup and handle it in Console, not Discord.");
                return;
            }
            try
            {
                _ftpClients.Clear();
                var creds = _adminConfigService.GetFTPCredentials();
                foreach (var cred in creds)
                {
                    var client = new AsyncFtpClient(host: cred.IPAddress, user: cred.Username, pass: cred.Password, port: cred.Port);
                    // Configure TLS/SSL settings
                    client.Config.EncryptionMode = FtpEncryptionMode.Explicit; // or FtpEncryptionMode.Auto
                    client.Config.ValidateAnyCertificate = true; // Accept self-signed certificates (for local dev)
                    client.Config.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
                    // Configure listing parser
                    client.Config.ListingParser = FtpParser.Machine;
                    // Add data connection configuration to handle TLS properly
                    client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
                    client.Config.ConnectTimeout = 15000; // 15 seconds
                    client.Config.DataConnectionConnectTimeout = 15000;
                    client.Config.DataConnectionReadTimeout = 15000;
                    _ftpClients[cred] = client;
                    client.AutoConnect().Wait();
                }
                IsClientsConfigured = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Error configuring FTP clients: {ex.Message}");
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _ = RunLoop(stoppingToken);
            return Task.CompletedTask;
        }

        private async Task RunLoop(CancellationToken token)
        {
            // Wait for Discord to be ready
            var tcs = new TaskCompletionSource();
            Task ReadyHandler()
            {
                tcs.TrySetResult();
                return Task.CompletedTask;
            }
            _discordClient.Ready += ReadyHandler;
            await tcs.Task;
            _discordClient.Ready -= ReadyHandler;

            Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Starting service...");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!IsClientsConfigured)
                    {
                        Console.WriteLine($"{DateTime.Now} - [FTPStatsService] FTP clients are not configured. Skipping FTP processing...");
                        await Task.Delay(TimeSpan.FromSeconds(15), token);
                        continue;
                    }

                    foreach (var kvp in _ftpClients)
                    {
                        var cred = kvp.Key;
                        var client = kvp.Value;
                        if (!client.IsConnected)
                        {
                            Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Reconnecting to FTP server for {cred.ServerName}...");
                            await client.Connect(token);
                        }

                        //Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Connection status for {cred.ServerName}: {client.IsConnected}");
                        bool directoryExists = await ExecuteWithDataConnectionFallback(client, () => client.DirectoryExists(cred.UserLogsDirectoryPath, token), token);
                        if (!directoryExists)
                        {
                            Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Warning: Directory '{cred.UserLogsDirectoryPath}' does not exist on FTP server for {cred.ServerName}. Skipping...");
                            continue;
                        }

                        if (await ExecuteWithDataConnectionFallback(client, () => ContainsFreshLogs(client, cred.UserLogsDirectoryPath, token), token))
                        {
                            Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Fresh logs found for {cred.ServerName}! Processing...");
                            var items = await ExecuteWithDataConnectionFallback(client, () => client.GetListing(cred.UserLogsDirectoryPath, token), token);
                            var logFiles = items.Where(item => item.Name.EndsWith(".log", StringComparison.OrdinalIgnoreCase)).ToList();
                            int totalFiles = logFiles.Count;
                            int processedCount = 0;
                            int validCount = 0;
                            int ignoredCount = 0;
                            foreach (var item in logFiles)
                            {
                                if (token.IsCancellationRequested) break;
                                processedCount++;
                                if (await _ut2004StatsService.IsLogFileProcessed(item.Name))
                                {
                                    string message = $"[FTPStatsService] Progress for {cred.ServerName}: {processedCount}/{totalFiles} ({processedCount * 100 / totalFiles}%) - Skipped (already processed)";
                                    Console.Write($"\r{message.PadRight(100)}");
                                    continue;
                                }

                                byte[] fileBytes = await ExecuteWithDataConnectionFallback(client, () => client.DownloadBytes(item.FullName, token), token);

                                // DownloadBytes returns null on a silent FTP failure (no exception thrown).
                                // Skip and log rather than letting MemoryStream crash the entire batch.
                                if (fileBytes == null)
                                {
                                    ignoredCount++;
                                    string skipMessage = $"[FTPStatsService] Progress for {cred.ServerName}: {processedCount}/{totalFiles} ({processedCount * 100 / totalFiles}%) - Download returned null for '{item.Name}', skipping. Valid: {validCount}, Ignored: {ignoredCount}";
                                    Console.Write($"\r{skipMessage.PadRight(100)}");
                                    continue;
                                }

                                using (var fileStream = new MemoryStream(fileBytes))
                                {
                                    bool wasValid = await _ut2004StatsService.ProcessLogFile(fileStream, item.Name, cred.ServerName, cred.IPAddress);
                                    string message;
                                    if (wasValid)
                                    {
                                        validCount++;
                                        message = $"[FTPStatsService] Progress for {cred.ServerName}: {processedCount}/{totalFiles} ({processedCount * 100 / totalFiles}%) - Valid: {validCount}, Ignored: {ignoredCount}";
                                    }
                                    else
                                    {
                                        ignoredCount++;
                                        message = $"[FTPStatsService] Progress for {cred.ServerName}: {processedCount}/{totalFiles} ({processedCount * 100 / totalFiles}%) - Valid: {validCount}, Ignored: {ignoredCount}";
                                    }
                                    Console.Write($"\r{message.PadRight(100)}");
                                }
                                // Every 50 files, add a newline for better readability
                                //if (processedCount % 50 == 0)
                                //{
                                //    Console.WriteLine(); // Move to new line
                                //}
                            }
                            // Final summary on new line
                            Console.WriteLine();
                            Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Processing complete for {cred.ServerName}");

                            await _ut2004StatsService.SetupPlayerProfiles();
                            _gitBackupService.EnqueueBackup();
                        }
                        else
                        {
                            // Testing
                            //await _ut2004StatsService.RebuildPlayerProfiles();
                        }
                    }
                    // Testing
                    //await _ut2004StatsService.RebuildPlayerProfiles();
                }
                catch (FtpCommandException ftpEx)
                {
                    Console.WriteLine($"\n{DateTime.Now} - [FTPStatsService] FTP Command Error: {ftpEx.Message} (Response: {ftpEx.Message})");
                }
                catch (FtpException ftpEx)
                {
                    Console.WriteLine($"\n{DateTime.Now} - [FTPStatsService] FTP Error: {ftpEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n{DateTime.Now} - [FTPStatsService] Error: {ex}");
                }

                await Task.Delay(TimeSpan.FromSeconds(15), token);
            }
        }

        /// <summary>
        /// Executes the given FTP action and, on TLS/data-connection errors (e.g. "425"), retries with alternate data connection modes.
        /// This helps with servers that behave differently regarding EPSV/PASV and TLS session resumption.
        /// </summary>
        private async Task<T> ExecuteWithDataConnectionFallback<T>(AsyncFtpClient client, Func<Task<T>> action, CancellationToken token)
        {
            // Order: start with configured mode, then try PASV, EPSV, AutoPassive as fallbacks.
            var triedModes = new List<FtpDataConnectionType>
            {
                client.Config.DataConnectionType,
                FtpDataConnectionType.PASV,
                FtpDataConnectionType.EPSV,
                FtpDataConnectionType.AutoPassive
            }.Distinct().ToList();

            Exception lastEx = null;
            foreach (var mode in triedModes)
            {
                try
                {
                    client.Config.DataConnectionType = mode;
                    if (!client.IsConnected)
                    {
                        await client.Connect(token);
                    }
                    return await action();
                }
                catch (FtpCommandException ftpCmdEx) when (ftpCmdEx.Message?.Contains("425") == true || (ftpCmdEx.Message?.Contains("TLS session") == true))
                {
                    lastEx = ftpCmdEx;
                    //Console.WriteLine($"{DateTime.Now} - [FTPStatsService] FTP 425/TLS data error using {mode}. Will retry with fallback mode.");
                    try { await client.Disconnect(token); } catch { }
                    // continue to next mode
                }
                catch (FtpException ftpEx)
                {
                    // Non-command FTP error; attempt reconnect with alternate mode once.
                    lastEx = ftpEx;
                    //Console.WriteLine($"{DateTime.Now} - [FTPStatsService] FTP error using {mode}: {ftpEx.Message}. Will retry with fallback mode.");
                    try { await client.Disconnect(token); } catch { }
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    //Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Unexpected error using {mode}: {ex.Message}. Will retry with fallback mode.");
                    try { await client.Disconnect(token); } catch { }
                }
            }

            // All attempts failed; rethrow the last exception for upstream logging/handling
            if (lastEx is not null)
                throw lastEx;

            throw new Exception("FTP action failed with unknown error.");
        }

        private async Task<bool> ContainsFreshLogs(AsyncFtpClient ftpClient, string directory, CancellationToken token)
        {
            var items = await ftpClient.GetListing(directory, token);

            foreach (var item in items)
            {
                if (item.Type == FtpObjectType.File)
                {
                    if (item.Name.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!await _ut2004StatsService.IsLogFileProcessed(item.Name))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Stopping service and closing FTP connection...");

            try
            {
                foreach (var client in _ftpClients.Values)
                {
                    if (client.IsConnected)
                    {
                        await client.Disconnect(cancellationToken);
                    }
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Error during shutdown: {ex.Message}");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
