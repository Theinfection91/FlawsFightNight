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
        public LiveViewManager(DiscordSocketClient discordSocketClient, DataManager dataManager, EmbedManager embedManager, MatchManager matchManager) : base("LiveViewManager", dataManager)
        {
            _client = discordSocketClient;
            _embedManager = embedManager;
            StartMatchesLiveViewTask();
            StartStandingsLiveViewTask();
            StartTeamsLiveViewTask();
            _matchManager = matchManager;
        }

        #region Matches LiveView
        public void StartMatchesLiveViewTask()
        {
            Task.Run(() => RunMatchesUpdateTaskAsync());
        }

        private async Task RunMatchesUpdateTaskAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(11));
                await SendMatchesToChannelAsync();
            }
        }

        private async Task SendMatchesToChannelAsync()
        {
            // Placeholder for sending match updates to a Discord channel
            //Console.WriteLine($"{DateTime.Now} - Sending match updates to channel...");
            await Task.CompletedTask;

            if (_dataManager.TournamentsDatabaseFile.Tournaments.Count == 0)
            {
                //Console.WriteLine("No tournaments found. No need to post to matches channels.");
                await Task.CompletedTask;
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
                        //Console.WriteLine($"Updated matches message for tournament {tournament.Name} in channel {channel.Name}.");
                    }
                    else
                    {
                        // If the message doesn't exist, send a new one
                        var newMessage = await channel.SendMessageAsync(embed: matchesEmbed);
                        tournament.MatchesMessageId = newMessage.Id;
                        //Console.WriteLine($"Sent new matches message for tournament {tournament.Name} in channel {channel.Name}.");
                        _dataManager.SaveAndReloadTournamentsDatabase();
                    }
                }
                else
                {
                    // If no message ID is set, send a new message
                    var newMessage = await channel.SendMessageAsync(embed: matchesEmbed);
                    tournament.MatchesMessageId = newMessage.Id;
                    //Console.WriteLine($"Sent new matches message for tournament {tournament.Name} in channel {channel.Name}.");
                    _dataManager.SaveAndReloadTournamentsDatabase();
                }
            }
        }
        #endregion

        #region Standings LiveView
        public RoundRobinStandings GetRoundRobinStandings(Tournament tournament)
        {
            //Console.WriteLine($"[DEBUG] Building standings for tournament: {tournament.Name}, Teams: {tournament.Teams.Count}");

            var standings = new RoundRobinStandings();

            // Build entries
            foreach (var team in tournament.Teams)
            {
                var entry = new StandingsEntry(team);
                standings.Entries.Add(entry);
                //Console.WriteLine($"[DEBUG] Added team entry: {entry.TeamName}, Wins: {entry.Wins}, Losses: {entry.Losses}, Score: {entry.TotalScore}");
            }

            // Initial sort (by wins/score/etc.)
            standings.SortStandings();

            //Console.WriteLine("[DEBUG] After initial sort:");
            foreach (var entry in standings.Entries)
            {
                //Console.WriteLine($"   {entry.TeamName}: {entry.Wins}-{entry.Losses}, {entry.TotalScore} pts");
            }

            // Group by full record (Wins + Losses)
            var groupedByRecord = standings.Entries
                //.Where(e => !(e.Wins == 0 && e.Losses == 0))
                .GroupBy(e => new { e.Wins, e.Losses })
                .OrderByDescending(g => g.Key.Wins)   // more wins first
                .ThenBy(g => g.Key.Losses);           // fewer losses first

            var resolvedList = new List<StandingsEntry>();

            foreach (var group in groupedByRecord)
            {
                var tiedTeams = group.Select(e => e.TeamName).ToList();

                if (tiedTeams.Count > 1)
                {
                    //Console.WriteLine($"[DEBUG] Tie detected in {group.Key.Wins}-{group.Key.Losses} group: {string.Join(", ", tiedTeams)}");

                    // Keep resolving until all tied teams are ranked
                    var remaining = new List<string>(tiedTeams);
                    while (remaining.Count > 0)
                    {
                        (string, string) tieBreakerResult = tournament.TieBreakerRule.ResolveTie(remaining, tournament.MatchLog);
                        string winner = tieBreakerResult.Item2;
                        var winnerEntry = group.First(e => e.TeamName == winner);

                        resolvedList.Add(winnerEntry);
                        remaining.Remove(winner);

                        //Console.WriteLine($"[DEBUG] -> Placed {winner} at next rank, {remaining.Count} left in tie group");
                    }
                }
                else
                {
                    resolvedList.AddRange(group);
                }
            }

            // Assign ranks after resolution
            for (int i = 0; i < resolvedList.Count; i++)
                resolvedList[i].Rank = i + 1;

            standings.Entries = resolvedList;

            // Update ranks in original entries as well
            foreach (var entry in standings.Entries)
            {
                //if (entry.Wins == 0 && entry.Losses == 0)
                //{
                //    continue;
                //}
                var original = tournament.Teams.First(t => t.Name == entry.TeamName);
                original.Rank = entry.Rank;
                tournament.Teams = tournament.Teams.OrderBy(t => t.Rank).ToList();
            }
            
            //_dataManager.SaveAndReloadTournamentsDatabase();

            //Console.WriteLine("[DEBUG] Final Standings:");
            foreach (var entry in standings.Entries)
            {
                //Console.WriteLine($"   Rank {entry.Rank}: {entry.TeamName} ({entry.Wins}-{entry.Losses}, {entry.TotalScore} pts)");
            }

            return standings;
        }

        public void StartStandingsLiveViewTask()
        {
            Task.Run(() => RunStandingsUpdateTaskAsync());
        }

        private async Task RunStandingsUpdateTaskAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                await SendStandingsToChannelAsync();
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
                        var standings = GetRoundRobinStandings(tournament);

                        if (standings == null)
                        {
                            //Console.WriteLine("Standings came back null.");
                            continue;
                        }

                        var standingsEmbed = _embedManager.RoundRobinStandingsLiveView(tournament, standings);
                        ulong messageId = tournament.StandingsMessageId;

                        if (messageId != 0)
                        {
                            var message = await channel.GetMessageAsync(messageId) as IUserMessage;
                            if (message != null)
                            {
                                await message.ModifyAsync(msg => msg.Embed = standingsEmbed);
                                //Console.WriteLine($"Updated standings message for tournament {tournament.Name} in channel {channel.Name}.");
                                _dataManager.SaveAndReloadTournamentsDatabase();
                            }
                            else
                            {
                                var newMessage = await channel.SendMessageAsync(embed: standingsEmbed);
                                tournament.StandingsMessageId = newMessage.Id;
                                //Console.WriteLine($"Sent new standings message for tournament {tournament.Name} in channel {channel.Name}.");
                                _dataManager.SaveAndReloadTournamentsDatabase();
                            }
                        }
                        else
                        {
                            var newMessage = await channel.SendMessageAsync(embed: standingsEmbed);
                            tournament.StandingsMessageId = newMessage.Id;
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
            Task.Run(() => RunTeamsUpdateTaskAsync());
        }

        private async Task RunTeamsUpdateTaskAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(7));
                await SendTeamsToChannelAsync();
            }
        }

        private async Task SendTeamsToChannelAsync()
        {
            if (_dataManager.TournamentsDatabaseFile.Tournaments.Count == 0)
            {
                //Console.WriteLine("No tournaments found. No need to post to teams channels.");
                await Task.CompletedTask;
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
                // Grab standings for correct ranks
                RoundRobinStandings standings = GetRoundRobinStandings(tournament);
                // Get the embed for the teams live view
                var teamsEmbed = _embedManager.TeamsLiveView(tournament, standings);
                ulong messageId = tournament.TeamsMessageId;
                if (messageId != 0)
                {
                    // Try to get the existing message
                    var message = await channel.GetMessageAsync(messageId) as IUserMessage;
                    if (message != null)
                    {
                        // Edit the existing message with the new embed
                        await message.ModifyAsync(msg => msg.Embed = teamsEmbed);
                        //Console.WriteLine($"Updated teams message for tournament {tournament.Name} in channel {channel.Name}.");
                    }
                    else
                    {
                        // If the message doesn't exist, send a new one
                        var newMessage = await channel.SendMessageAsync(embed: teamsEmbed);
                        tournament.TeamsMessageId = newMessage.Id;
                        //Console.WriteLine($"Sent new teams message for tournament {tournament.Name} in channel {channel.Name}.");
                        _dataManager.SaveAndReloadTournamentsDatabase();
                    }
                }
                else
                {
                    // If no message ID is set, send a new message
                    var newMessage = await channel.SendMessageAsync(embed: teamsEmbed);
                    tournament.TeamsMessageId = newMessage.Id;
                    //Console.WriteLine($"Sent new teams message for tournament {tournament.Name} in channel {channel.Name}.");
                    _dataManager.SaveAndReloadTournamentsDatabase();
                }
            }
        }
        #endregion
    }
}
