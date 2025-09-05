using Discord;
using Discord.WebSocket;
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
        private MatchManager _matchManager;

        // Tasks
        private Task _watchdogTask;
        private Task _matchesLiveViewTask;
        private Task _standingsLiveViewTask;
        private Task _teamsLiveViewTask;
        private CancellationTokenSource _cts = new();

        // Watchdog timestamps
        private DateTime _lastMatchesUpdate = DateTime.MinValue;
        private DateTime _lastStandingsUpdate = DateTime.MinValue;
        private DateTime _lastTeamsUpdate = DateTime.MinValue;

        public LiveViewManager(DiscordSocketClient discordSocketClient, DataManager dataManager, EmbedManager embedManager, MatchManager matchManager) : base("LiveViewManager", dataManager)
        {
            _client = discordSocketClient;
            _embedManager = embedManager;
            _matchManager = matchManager;

            StartMatchesLiveViewTask();
            StartStandingsLiveViewTask();
            StartTeamsLiveViewTask();

            StartWatchdogTask();
        }

        #region Watchdog
        public void StartWatchdogTask()
        {
            _watchdogTask = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), _cts.Token);

                    bool matchesDead = _matchesLiveViewTask?.IsFaulted ?? true;
                    bool standingsDead = _standingsLiveViewTask?.IsFaulted ?? true;
                    bool teamsDead = _teamsLiveViewTask?.IsFaulted ?? true;

                    // also check if they haven't updated in >1 min
                    bool matchesHung = (DateTime.UtcNow - _lastMatchesUpdate) > TimeSpan.FromMinutes(1);
                    bool standingsHung = (DateTime.UtcNow - _lastStandingsUpdate) > TimeSpan.FromMinutes(1);
                    bool teamsHung = (DateTime.UtcNow - _lastTeamsUpdate) > TimeSpan.FromMinutes(1);

                    if (matchesDead || standingsDead || teamsDead || matchesHung || standingsHung || teamsHung)
                    {
                        Console.WriteLine($"{DateTime.Now} [Watchdog] Detected dead/hung task. Restarting all live view tasks...");
                        await RestartAllAsync();
                    }
                }
            }, _cts.Token);
        }
        #endregion

        #region Matches LiveView
        public void StartMatchesLiveViewTask()
        {
            //Task.Run(() => RunMatchesUpdateTaskAsync());
            _matchesLiveViewTask = RunMatchesUpdateTaskAsync(_cts.Token);
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

            if (_dataManager.TournamentsDatabaseFile.Tournaments.Count == 0)
            {
                //Console.WriteLine("No tournaments found. No need to post to matches channels.");
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
                    continue;
                }

                // Get the channel from the client
                var channel = _client.GetChannel(tournament.MatchesChannelId) as IMessageChannel;

                if (channel == null)
                {
                    //Console.WriteLine($"Channel with ID {tournament.MatchesChannelId} not found for tournament {tournament.Name}. Skipping.");
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
                        _dataManager.SaveAndReloadTournamentsDatabase();
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
                    _dataManager.SaveAndReloadTournamentsDatabase();
                }
            }
        }
        #endregion

        #region Standings LiveView
        public void StartStandingsLiveViewTask()
        {
            //Task.Run(() => RunStandingsUpdateTaskAsync());
            _standingsLiveViewTask = RunStandingsUpdateTaskAsync(_cts.Token);
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

            if (_dataManager.TournamentsDatabaseFile.Tournaments.Count == 0)
            {
                //Console.WriteLine("No tournaments found. No need to post to standings channels.");
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
                    continue;
                }

                var channel = _client.GetChannel(tournament.StandingsChannelId) as IMessageChannel;
                if (channel == null)
                {
                    //Console.WriteLine($"Channel with ID {tournament.StandingsChannelId} not found for tournament {tournament.Name}. Skipping.");
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
                                _dataManager.SaveAndReloadTournamentsDatabase();
                            }
                        }
                        else
                        {
                            var newMessage = await channel.SendMessageAsync(embed: standingsEmbed);
                            tournament.StandingsMessageId = newMessage.Id;

                            // Update the last update timestamp
                            _lastStandingsUpdate = DateTime.UtcNow;

                            //Console.WriteLine($"{newMessage.Id} Sent new standings message for tournament {tournament.Name} in channel {channel.Name}.");
                            _dataManager.SaveAndReloadTournamentsDatabase();
                        }
                        break;

                    default:
                        //Console.WriteLine($"Tournament type {tournament.Type} not supported for standings live view. Skipping.");
                        break;
                }
            }
        }
        #endregion

        #region Teams LiveView
        public void StartTeamsLiveViewTask()
        {
            //Task.Run(() => RunTeamsUpdateTaskAsync());
            _teamsLiveViewTask = RunTeamsUpdateTaskAsync(_cts.Token);
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
            if (_dataManager.TournamentsDatabaseFile.Tournaments.Count == 0)
            {
                //Console.WriteLine("No tournaments found. No need to post to teams channels.");
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
                    continue;
                }
                // Get the channel from the client
                var channel = _client.GetChannel(tournament.TeamsChannelId) as IMessageChannel;
                if (channel == null)
                {
                    //Console.WriteLine($"Channel with ID {tournament.TeamsChannelId} not found for tournament {tournament.Name}. Skipping.");
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
                        _dataManager.SaveAndReloadTournamentsDatabase();
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
                    _dataManager.SaveAndReloadTournamentsDatabase();
                }
            }
        }
        #endregion

        #region Helpers
        public async Task RestartAllAsync()
        {
            // Cancel all current tasks
            _cts.Cancel();
            try
            {
                await Task.WhenAll(new[] { _matchesLiveViewTask, _standingsLiveViewTask, _teamsLiveViewTask }
            .Where(t => t != null)); ;
            }
            catch (TaskCanceledException) { } // expected on cancel

            // Reset the CancellationTokenSource
            _cts.Dispose();
            _cts = new CancellationTokenSource();

            // Restart tasks with a fresh token
            StartMatchesLiveViewTask();
            StartStandingsLiveViewTask();
            StartTeamsLiveViewTask();
        }
        #endregion
    }
}
