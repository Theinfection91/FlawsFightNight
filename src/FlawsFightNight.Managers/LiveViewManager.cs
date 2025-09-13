using Discord;
using Discord.WebSocket;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class LiveViewManager : BaseDataDriven
    {
        private DiscordSocketClient _client;
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;


        // Tasks
        private Task _watchdogTask;
        private Task _matchesLiveViewTask;
        private Task _standingsLiveViewTask;
        private Task _teamsLiveViewTask;

        // Cancellation tokens
        private CancellationTokenSource _matchesCts = new();
        private CancellationTokenSource _standingsCts = new();
        private CancellationTokenSource _teamsCts = new();

        // Watchdog timestamps
        private DateTime _lastMatchesUpdate = DateTime.UtcNow;
        private DateTime _lastStandingsUpdate = DateTime.UtcNow;
        private DateTime _lastTeamsUpdate = DateTime.UtcNow;

        private SemaphoreSlim _semaphore = new(1, 1);

        //private static readonly bool _testMode = true;

        public LiveViewManager(DiscordSocketClient discordSocketClient, DataManager dataManager, EmbedManager embedManager, GitBackupManager gitBackupManager) : base("LiveViewManager", dataManager)
        {
            _client = discordSocketClient;
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;

            StartMatchesLiveViewTask();
            StartStandingsLiveViewTask();
            StartTeamsLiveViewTask();

            StartWatchdogTask();
        }

        #region Watchdog
        public void StartWatchdogTask()
        {
            Console.WriteLine($"{DateTime.Now} [LiveView - Watchdog] Starting watchdog task...");
            _watchdogTask = Task.Run(async () =>
            {
                while (true) // watchdog runs until process exit
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30));

                        bool matchesDead = _matchesLiveViewTask?.IsFaulted ?? true;
                        bool standingsDead = _standingsLiveViewTask?.IsFaulted ?? true;
                        bool teamsDead = _teamsLiveViewTask?.IsFaulted ?? true;

                        // Also check if they haven't updated in >1 min
                        bool matchesHung = (DateTime.UtcNow - _lastMatchesUpdate) > TimeSpan.FromMinutes(1);
                        bool standingsHung = (DateTime.UtcNow - _lastStandingsUpdate) > TimeSpan.FromMinutes(1);
                        bool teamsHung = (DateTime.UtcNow - _lastTeamsUpdate) > TimeSpan.FromMinutes(1);

                        if (matchesDead || matchesHung)
                        {
                            Console.WriteLine($"{DateTime.Now} [LiveView - Watchdog] MatchesLiveViewTask is dead/hung. Restarting...");
                            await RestartSpecificChannelAsync(LiveViewChannelType.Matches);
                        }
                        if (standingsDead || standingsHung)
                        {
                            Console.WriteLine($"{DateTime.Now} [LiveView - Watchdog] StandingsLiveViewTask is dead/hung. Restarting...");
                            await RestartSpecificChannelAsync(LiveViewChannelType.Standings);
                        }
                        if (teamsDead || teamsHung)
                        {
                            Console.WriteLine($"{DateTime.Now} [LiveView - Watchdog] TeamsLiveViewTask is dead/hung. Restarting...");
                            await RestartSpecificChannelAsync(LiveViewChannelType.Teams);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{DateTime.Now} [LiveView - Watchdog] Exception: {ex}");
                    }
                }
            });
        }
        #endregion

        #region Matches LiveView
        public void StartMatchesLiveViewTask()
        {
            //Task.Run(() => RunMatchesUpdateTaskAsync());
            _matchesLiveViewTask = Task.Run(() => RunMatchesUpdateTaskAsync(_matchesCts.Token));
        }

        private async Task RunMatchesUpdateTaskAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(13), token);
                    await SendMatchesToChannelAsync();
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} [MatchesLiveViewTask] Exception: {ex}");
            }
        }

        private async Task SendMatchesToChannelAsync()
        {
            //Console.WriteLine($"{DateTime.Now} - Sending match updates to channel...");
            try
            {
                await _semaphore.WaitAsync(); // Ensure only one update at a time

                //if (_testMode && Random.Shared.Next(0, 5) == 0)
                //{
                //    throw new Exception("TestMode: Randomly simulated failure.");
                //}

                if (_dataManager.TournamentsDatabaseFile.Tournaments.Count == 0)
                {
                    //Console.WriteLine("No tournaments found. No need to post to matches channels.");
                    _lastMatchesUpdate = DateTime.UtcNow;
                    return;
                }

                foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
                {
                    if (tournament == null)
                    {
                        //Console.WriteLine("Tournament is null. Skipping.");
                        continue;
                    }

                    if (tournament.MatchesChannelId == 0)
                    {
                        //Console.WriteLine($"Tournament {tournament.Name} has no Matches Channel ID set. Skipping.");
                        _lastMatchesUpdate = DateTime.UtcNow;
                        continue;
                    }

                    // Get the channel from the client
                    var channel = _client.GetChannel(tournament.MatchesChannelId) as IMessageChannel;

                    if (channel == null)
                    {
                        //Console.WriteLine($"Channel with ID {tournament.MatchesChannelId} not found for tournament {tournament.Name}. Skipping.");
                        tournament.MatchesChannelId = 0;
                        await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                        await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                        continue;
                    }

                    // Get the embed for the matches live view
                    var matchesEmbed = _embedManager.MatchesLiveView(tournament);

                    ulong messageId = tournament.MatchesMessageId;
                    if (messageId != 0)
                    {
                        // Try to get the existing message
                        var message = await channel.GetMessageAsync(messageId) as IUserMessage;
                        if (message != null)
                        {
                            // Edit the existing message with the new embed
                            await message.ModifyAsync(msg => msg.Embed = matchesEmbed);

                            // Update the last update timestamp
                            _lastMatchesUpdate = DateTime.UtcNow;

                            //Console.WriteLine($"Updated matches message for tournament {tournament.Name} in channel {channel.Name}.");
                        }
                        else
                        {
                            // If the message doesn't exist, send a new one
                            var newMessage = await channel.SendMessageAsync(embed: matchesEmbed);
                            tournament.MatchesMessageId = newMessage.Id;

                            // Update the last update timestamp
                            _lastMatchesUpdate = DateTime.UtcNow;

                            //Console.WriteLine($"Sent new matches message for tournament {tournament.Name} in channel {channel.Name}.");
                            await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                            await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                        }
                    }
                    else
                    {
                        // If no message ID is set, send a new message
                        var newMessage = await channel.SendMessageAsync(embed: matchesEmbed);
                        tournament.MatchesMessageId = newMessage.Id;

                        // Update the last update timestamp
                        _lastMatchesUpdate = DateTime.UtcNow;

                        //Console.WriteLine($"Sent new matches message for tournament {tournament.Name} in channel {channel.Name}.");
                        await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                        await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} [SendMatchesToChannelAsync] Exception: {ex}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
        #endregion

        #region Standings LiveView
        public void StartStandingsLiveViewTask()
        {
            //Task.Run(() => RunStandingsUpdateTaskAsync());
            _standingsLiveViewTask = Task.Run(() => RunStandingsUpdateTaskAsync(_standingsCts.Token));
        }

        private async Task RunStandingsUpdateTaskAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(11), token);
                    await SendStandingsToChannelAsync();
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} [StandingsLiveViewTask] Exception: {ex}");
            }

        }

        private async Task SendStandingsToChannelAsync()
        {
            //Console.WriteLine($"{DateTime.Now} - Sending standings updates to channel...");
            try
            {
                await _semaphore.WaitAsync();

                if (_dataManager.TournamentsDatabaseFile.Tournaments.Count == 0)
                {
                    //Console.WriteLine("No tournaments found. No need to post to standings channels.");
                    _lastStandingsUpdate = DateTime.UtcNow;
                    return;
                }

                foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
                {
                    if (tournament == null)
                    {
                        //Console.WriteLine("Tournament is null. Skipping.");
                        continue;
                    }
                    if (tournament.StandingsChannelId == 0)
                    {
                        //Console.WriteLine($"Tournament {tournament.Name} has no Standings Channel ID set. Skipping.");
                        _lastStandingsUpdate = DateTime.UtcNow;
                        continue;
                    }

                    var channel = _client.GetChannel(tournament.StandingsChannelId) as IMessageChannel;
                    if (channel == null)
                    {
                        //Console.WriteLine($"Channel with ID {tournament.StandingsChannelId} not found for tournament {tournament.Name}. Skipping.");
                        tournament.StandingsChannelId = 0;
                        await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                        await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                        continue;
                    }

                    switch (tournament.Type)
                    {
                        case Core.Enums.TournamentType.RoundRobin:
                            var standingsEmbed = _embedManager.RoundRobinStandingsLiveView(tournament);
                            ulong messageId = tournament.StandingsMessageId;

                            if (messageId != 0)
                            {
                                var message = await channel.GetMessageAsync(messageId) as IUserMessage;
                                if (message != null)
                                {
                                    await message.ModifyAsync(msg => msg.Embed = standingsEmbed);

                                    // Update the last update timestamp
                                    _lastStandingsUpdate = DateTime.UtcNow;

                                    //Console.WriteLine($"Updated standings message for tournament {tournament.Name} in channel {channel.Name}.");
                                }
                                else
                                {
                                    var newMessage = await channel.SendMessageAsync(embed: standingsEmbed);
                                    tournament.StandingsMessageId = newMessage.Id;

                                    // Update the last update timestamp
                                    _lastStandingsUpdate = DateTime.UtcNow;

                                    //Console.WriteLine($"Sent new standings message for tournament {tournament.Name} in channel {channel.Name}.");
                                    await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                                    await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                                }
                            }
                            else
                            {
                                var newMessage = await channel.SendMessageAsync(embed: standingsEmbed);
                                tournament.StandingsMessageId = newMessage.Id;

                                // Update the last update timestamp
                                _lastStandingsUpdate = DateTime.UtcNow;

                                //Console.WriteLine($"{newMessage.Id} Sent new standings message for tournament {tournament.Name} in channel {channel.Name}.");
                                await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                                await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                            }
                            break;

                        default:
                            //Console.WriteLine($"Tournament type {tournament.Type} not supported for standings live view. Skipping.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} [SendStandingsToChannelAsync] Exception: {ex}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
        #endregion

        #region Teams LiveView
        public void StartTeamsLiveViewTask()
        {
            //Task.Run(() => RunTeamsUpdateTaskAsync());
            _teamsLiveViewTask = Task.Run(() => RunTeamsUpdateTaskAsync(_teamsCts.Token));
        }

        private async Task RunTeamsUpdateTaskAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    //Console.WriteLine($"{DateTime.Now} [TeamsLiveViewTask] Running teams live view update...");
                    await Task.Delay(TimeSpan.FromSeconds(15), token);
                    await SendTeamsToChannelAsync();
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} [TeamsLiveViewTask] Exception: {ex}");
            }
        }

        private async Task SendTeamsToChannelAsync()
        {
            try
            {
                await _semaphore.WaitAsync();

                if (_dataManager.TournamentsDatabaseFile.Tournaments.Count == 0)
                {
                    //Console.WriteLine("No tournaments found. No need to post to teams channels.");
                    _lastTeamsUpdate = DateTime.UtcNow;
                    return;
                }

                foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
                {
                    if (tournament == null)
                    {
                        //Console.WriteLine("Tournament is null. Skipping.");
                        continue;
                    }
                    if (tournament.TeamsChannelId == 0)
                    {
                        //Console.WriteLine($"Tournament {tournament.Name} has no Teams Channel ID set. Skipping.");
                        _lastTeamsUpdate = DateTime.UtcNow;
                        continue;
                    }
                    // Get the channel from the client
                    var channel = _client.GetChannel(tournament.TeamsChannelId) as IMessageChannel;
                    if (channel == null)
                    {
                        //Console.WriteLine($"Channel with ID {tournament.TeamsChannelId} not found for tournament {tournament.Name}. Skipping.");
                        tournament.TeamsChannelId = 0;
                        await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                        await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                        continue;
                    }
                    // Get the embed for the teams live view
                    var teamsEmbed = _embedManager.TeamsLiveView(tournament);
                    ulong messageId = tournament.TeamsMessageId;
                    if (messageId != 0)
                    {
                        // Try to get the existing message
                        var message = await channel.GetMessageAsync(messageId) as IUserMessage;
                        if (message != null)
                        {
                            // Edit the existing message with the new embed
                            await message.ModifyAsync(msg => msg.Embed = teamsEmbed);

                            // Update the last update timestamp
                            _lastTeamsUpdate = DateTime.UtcNow;

                            //Console.WriteLine($"Updated teams message for tournament {tournament.Name} in channel {channel.Name}.");
                        }
                        else
                        {
                            // If the message doesn't exist, send a new one
                            var newMessage = await channel.SendMessageAsync(embed: teamsEmbed);
                            tournament.TeamsMessageId = newMessage.Id;

                            // Update the last update timestamp
                            _lastTeamsUpdate = DateTime.UtcNow;

                            //Console.WriteLine($"Sent new teams message for tournament {tournament.Name} in channel {channel.Name}.");
                            await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                            await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                        }
                    }
                    else
                    {
                        // If no message ID is set, send a new message
                        var newMessage = await channel.SendMessageAsync(embed: teamsEmbed);
                        tournament.TeamsMessageId = newMessage.Id;

                        // Update the last update timestamp
                        _lastTeamsUpdate = DateTime.UtcNow;

                        //Console.WriteLine($"Sent new teams message for tournament {tournament.Name} in channel {channel.Name}.");
                        await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                        await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} [SendTeamsToChannelAsync] Exception: {ex}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
        #endregion

        #region Helpers
        public async Task RestartSpecificChannelAsync(LiveViewChannelType liveViewChannelType)
        {
            switch (liveViewChannelType)
            {
                case LiveViewChannelType.Matches:
                    _matchesCts.Cancel();
                    try
                    {
                        if (_matchesLiveViewTask != null)
                            await _matchesLiveViewTask;
                    }
                    catch (TaskCanceledException) { } // expected on cancel
                    _matchesCts.Dispose();
                    _matchesCts = new CancellationTokenSource();
                    StartMatchesLiveViewTask();
                    break;
                case LiveViewChannelType.Standings:
                    _standingsCts.Cancel();
                    try
                    {
                        if (_standingsLiveViewTask != null)
                            await _standingsLiveViewTask;
                    }
                    catch (TaskCanceledException) { } // expected on cancel
                    _standingsCts.Dispose();
                    _standingsCts = new CancellationTokenSource();
                    StartStandingsLiveViewTask();
                    break;
                case LiveViewChannelType.Teams:
                    _teamsCts.Cancel();
                    try
                    {
                        if (_teamsLiveViewTask != null)
                            await _teamsLiveViewTask;
                    }
                    catch (TaskCanceledException) { } // expected on cancel
                    _teamsCts.Dispose();
                    _teamsCts = new CancellationTokenSource();
                    StartTeamsLiveViewTask();
                    break;
                default:
                    Console.WriteLine($"{DateTime.Now} [RestartSpecificChannelAsync] Unknown LiveViewChannelType: {liveViewChannelType}");
                    break;
            }
        }
        #endregion
    }
}
