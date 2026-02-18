using Discord;
using Discord.WebSocket;
using FluentFTP;
using FluentFTP.Exceptions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class FTPStatsService : BackgroundService
    {
        private readonly ConfigManager _configManager;
        private readonly GitBackupManager _gitBackupManager;
        private readonly UT2004StatsManager _ut2004StatsManager;

        private readonly DiscordSocketClient _discordClient;
        private AsyncFtpClient? _ftpClient;

        public FTPStatsService(ConfigManager configManager, DiscordSocketClient client, GitBackupManager gitBackupManager, UT2004StatsManager uT2004StatsManager)
        {
            _configManager = configManager;
            _gitBackupManager = gitBackupManager;
            _ut2004StatsManager = uT2004StatsManager;

            _discordClient = client;
            ConfigureFTPClients();
        }

        private void ConfigureFTPClients()
        {
            // TODO Implement pulling creds from ConfigManager once they are finally being saved
            //var creds = _configManager.GetFTPCredentials();
            //_ftpClient = new(host: creds.Host, user: creds.Username, pass: creds.Password, port: creds.Port);
            _ftpClient = new(host: "127.0.0.1", user: "sho_ny", pass: "password1", port: 21);

            // Configure TLS/SSL settings
            _ftpClient.Config.EncryptionMode = FtpEncryptionMode.Explicit; // or FtpEncryptionMode.Auto
            _ftpClient.Config.ValidateAnyCertificate = true; // Accept self-signed certificates (for local dev)
            _ftpClient.Config.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;

            // Configure listing parser
            _ftpClient.Config.ListingParser = FtpParser.Machine;
            
            // Add data connection configuration to handle TLS properly
            _ftpClient.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
            _ftpClient.Config.ConnectTimeout = 15000; // 15 seconds
            _ftpClient.Config.DataConnectionConnectTimeout = 15000;
            _ftpClient.Config.DataConnectionReadTimeout = 15000;

            _ftpClient.AutoConnect();
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
                    // Ensure connection is alive
                    if (!_ftpClient.IsConnected)
                    {
                        Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Reconnecting to FTP server...");
                        await _ftpClient.Connect(token);
                    }

                    Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Connection status: {_ftpClient.IsConnected}");
                    
                    // TODO: Update this path to match your actual FTP directory structure
                    string chiDir = "/placeholderDir/anotherDir/UserLogs";
                    string nyDir = "/thisDir/anotherDir/oneMoreDir/UserLogs";

                    // Verify directory exists before processing
                    if (!await _ftpClient.DirectoryExists(nyDir, token))
                    {
                        Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Warning: Directory '{chiDir}' does not exist on FTP server. Skipping...");
                        await Task.Delay(TimeSpan.FromSeconds(30), token); // Wait longer if directory doesn't exist
                        continue;
                    }

                    if (await ContainsFreshLogs(nyDir, token))
                    {
                        Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Fresh logs found! Processing...");

                        var items = await _ftpClient.GetListing(nyDir, token);
                        var logFiles = items.Where(item => item.Name.EndsWith(".log", StringComparison.OrdinalIgnoreCase)).ToList();
                        
                        int totalFiles = logFiles.Count;
                        int processedCount = 0;
                        int validCount = 0;
                        int ignoredCount = 0;

                        foreach (var item in logFiles)
                        {
                            if (token.IsCancellationRequested) break;

                            processedCount++;
                            
                            if (await _ut2004StatsManager.IsLogFileProcessed(item.Name))
                            {
                                string message = $"[FTPStatsService] Progress: {processedCount}/{totalFiles} ({processedCount * 100 / totalFiles}%) - Skipped (already processed)";
                                Console.Write($"\r{message.PadRight(100)}");
                                continue;
                            }

                            // Properly dispose the stream after processing
                            await using (var fileStream = await _ftpClient.OpenRead(item.FullName, token: token))
                            {
                                bool wasValid = await _ut2004StatsManager.ProcessLogFile(fileStream, item.Name);
                                
                                string message;
                                if (wasValid)
                                {
                                    validCount++;
                                    message = $"[FTPStatsService] Progress: {processedCount}/{totalFiles} ({processedCount * 100 / totalFiles}%) - Valid: {validCount}, Ignored: {ignoredCount}";
                                }
                                else
                                {
                                    ignoredCount++;
                                    message = $"[FTPStatsService] Progress: {processedCount}/{totalFiles} ({processedCount * 100 / totalFiles}%) - Valid: {validCount}, Ignored: {ignoredCount}";
                                }
                                Console.Write($"\r{message.PadRight(100)}");
                            }

                            // Every 50 files, add a newline for better readability
                            if (processedCount % 50 == 0)
                            {
                                Console.WriteLine(); // Move to new line
                            }
                        }

                        // Final summary on new line
                        Console.WriteLine();
                        Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Processing complete! Valid: {validCount}, Ignored: {ignoredCount}, Total: {totalFiles}");

                        //var allStats = await _ut2004StatsManager.GetAllProcessedStatLogs();
                        //Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Total stat logs in database: {allStats.Count}");

                        await _ut2004StatsManager.SetupPlayerProfiles();
                        await _gitBackupManager.CopyAndBackupFilesToGitAsync();
                    }
                }
                catch (FtpCommandException ftpEx)
                {
                    Console.WriteLine($"\n{DateTime.Now} - [FTPStatsService] FTP Command Error: {ftpEx.Message} (Response: {ftpEx.Message})");
                }
                catch (FtpException ftpEx)
                {
                    Console.WriteLine($"\n{DateTime.Now} - [FTPStatsService] FTP Error: {ftpEx.Message}");
                    // Try to reconnect on FTP errors
                    try
                    {
                        if (_ftpClient.IsConnected)
                        {
                            await _ftpClient.Disconnect(token);
                        }
                    }
                    catch { /* Ignore disconnect errors */ }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n{DateTime.Now} - [FTPStatsService] Error: {ex}");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
        }

        private async Task<bool> ContainsFreshLogs(string directory, CancellationToken token)
        {
            var items = await _ftpClient.GetListing(directory, token);

            foreach (var item in items)
            {
                if (item.Type == FtpObjectType.File)
                {
                    if (item.Name.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!await _ut2004StatsManager.IsLogFileProcessed(item.Name))
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
                if (_ftpClient?.IsConnected == true)
                {
                    await _ftpClient.Disconnect(cancellationToken);
                }
                _ftpClient?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - [FTPStatsService] Error during shutdown: {ex.Message}");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
