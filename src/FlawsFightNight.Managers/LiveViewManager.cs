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

        // Semaphores
        private SemaphoreSlim _standingsSemaphore = new(1, 1);
        private SemaphoreSlim _matchesSemaphore = new(1, 1);
        private SemaphoreSlim _teamsSemaphore = new(1, 1);

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

                        // Also check if they haven't updated in >3 min
                        bool matchesHung = (DateTime.UtcNow - _lastMatchesUpdate) > TimeSpan.FromMinutes(3);
                        bool standingsHung = (DateTime.UtcNow - _lastStandingsUpdate) > TimeSpan.FromMinutes(3);
                        bool teamsHung = (DateTime.UtcNow - _lastTeamsUpdate) > TimeSpan.FromMinutes(3);

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
                    await Task.Delay(TimeSpan.FromSeconds(30), token);
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
            await _matchesSemaphore.WaitAsync();
            try
            {
                if (_dataManager.TournamentsDatabaseFile.Tournaments.Count == 0)
                {
                    _lastMatchesUpdate = DateTime.UtcNow;
                    return;
                }

                foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
                {
                    if (tournament == null || tournament.MatchesChannelId == 0)
                        continue;

                    var channel = _client.GetChannel(tournament.MatchesChannelId) as IMessageChannel;
                    if (channel == null)
                    {
                        tournament.MatchesChannelId = 0;
                        await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                        await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                        continue;
                    }

                    var embed = _embedManager.MatchesLiveViewResolver(tournament);

                    // Try to update an existing message
                    if (tournament.MatchesMessageId != 0)
                    {
                        var existing = await channel.GetMessageAsync(tournament.MatchesMessageId) as IUserMessage;
                        if (existing != null)
                        {
                            await existing.ModifyAsync(m => m.Embed = embed);
                            _lastMatchesUpdate = DateTime.UtcNow;
                            continue;
                        }
                    }

                    // Existing message missing OR no message ID set → send a new message
                    var newMessage = await channel.SendMessageAsync(embed: embed);
                    tournament.MatchesMessageId = newMessage.Id;
                    _lastMatchesUpdate = DateTime.UtcNow;

                    await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                    await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} [SendMatchesToChannelAsync] Exception: {ex}");
            }
            finally
            {
                _matchesSemaphore.Release();
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
                    await Task.Delay(TimeSpan.FromSeconds(25), token);
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
            await _standingsSemaphore.WaitAsync();
            try
            {
                if (_dataManager.TournamentsDatabaseFile.Tournaments.Count == 0)
                {
                    _lastStandingsUpdate = DateTime.UtcNow;
                    return;
                }

                foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
                {
                    if (tournament == null || tournament.StandingsChannelId == 0)
                        continue;

                    var channel = _client.GetChannel(tournament.StandingsChannelId) as IMessageChannel;
                    if (channel == null)
                    {
                        tournament.StandingsChannelId = 0;
                        await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                        await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                        continue;
                    }

                    var embed = tournament.Type switch
                    {
                        TournamentType.Ladder => _embedManager.LadderStandingsLiveView(tournament),
                        TournamentType.RoundRobin => _embedManager.RoundRobinStandingsLiveView(tournament),
                        _ => null
                    };

                    if (embed == null)
                        continue;

                    if (tournament.StandingsMessageId != 0)
                    {
                        var existing = await channel.GetMessageAsync(tournament.StandingsMessageId) as IUserMessage;
                        if (existing != null)
                        {
                            await existing.ModifyAsync(m => m.Embed = embed);
                            _lastStandingsUpdate = DateTime.UtcNow;
                            continue;
                        }
                    }

                    var newMsg = await channel.SendMessageAsync(embed: embed);
                    tournament.StandingsMessageId = newMsg.Id;
                    _lastStandingsUpdate = DateTime.UtcNow;

                    await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                    await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} [SendStandingsToChannelAsync] Exception: {ex}");
            }
            finally
            {
                _standingsSemaphore.Release();
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
                    await Task.Delay(TimeSpan.FromSeconds(35), token);
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
            await _teamsSemaphore.WaitAsync();
            try
            {
                if (_dataManager.TournamentsDatabaseFile.Tournaments.Count == 0)
                {
                    _lastTeamsUpdate = DateTime.UtcNow;
                    return;
                }

                foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
                {
                    if (tournament == null || tournament.TeamsChannelId == 0)
                        continue;

                    var channel = _client.GetChannel(tournament.TeamsChannelId) as IMessageChannel;
                    if (channel == null)
                    {
                        tournament.TeamsChannelId = 0;
                        await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                        await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                        continue;
                    }

                    var embed = _embedManager.TeamsLiveView(tournament);

                    if (tournament.TeamsMessageId != 0)
                    {
                        var existing = await channel.GetMessageAsync(tournament.TeamsMessageId) as IUserMessage;
                        if (existing != null)
                        {
                            await existing.ModifyAsync(m => m.Embed = embed);
                            _lastTeamsUpdate = DateTime.UtcNow;
                            continue;
                        }
                    }

                    var newMsg = await channel.SendMessageAsync(embed: embed);
                    tournament.TeamsMessageId = newMsg.Id;
                    _lastTeamsUpdate = DateTime.UtcNow;

                    await Task.Run(() => _dataManager.SaveAndReloadTournamentsDatabase());
                    await Task.Run(() => _gitBackupManager.CopyAndBackupFilesToGit());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} [SendTeamsToChannelAsync] Exception: {ex}");
            }
            finally
            {
                _teamsSemaphore.Release();
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

                    var oldTask = _teamsLiveViewTask;

                    // Wait max 2 seconds for it to finish
                    var timeoutTask = Task.Delay(2000);
                    await Task.WhenAny(oldTask, timeoutTask);

                    // At this point we consider the old task dead
                    _teamsCts.Dispose();
                    _teamsCts = new CancellationTokenSource();

                    StartTeamsLiveViewTask();
                    break;
            }
        }
        #endregion
    }
}
