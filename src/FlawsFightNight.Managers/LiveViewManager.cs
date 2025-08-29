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
            Console.WriteLine($"[DEBUG] Building standings for tournament: {tournament.Name}, Teams: {tournament.Teams.Count}");

            var standings = new RoundRobinStandings();

            // Build entries
            foreach (var team in tournament.Teams)
            {
                var entry = new StandingsEntry(team);
                standings.Entries.Add(entry);
                Console.WriteLine($"[DEBUG] Added team entry: {entry.TeamName}, Wins: {entry.Wins}, Score: {entry.TotalScore}");
            }

            // Initial sort by win/loss or points
            Console.WriteLine("[DEBUG] Sorting initial standings...");
            standings.SortStandings();
            Console.WriteLine("[DEBUG] After Sort:");
            foreach (var entry in standings.Entries)
                Console.WriteLine($"   Team: {entry.TeamName}, Wins: {entry.Wins}, Score: {entry.TotalScore}");

            // Detect ties
            List<string> tiedTeams = _matchManager.GetTiedTeams(tournament.MatchLog, tournament.IsDoubleRoundRobin);
            Console.WriteLine($"[DEBUG] Tied Teams Found: {string.Join(", ", tiedTeams)}");

            if (tiedTeams.Any())
            {
                // Resolve tie-breaker for the tied group
                string winner = _matchManager.ResolveTieBreaker(tiedTeams, tournament.MatchLog);
                Console.WriteLine($"[DEBUG] Tie-breaker winner: {winner}");

                // Force the tiebreaker winner to bubble up among equals
                standings.Entries = standings.Entries
                    .OrderByDescending(e => e.TeamName == winner) // winner comes first in tied group
                    .ThenByDescending(e => e.Wins)
                    .ThenByDescending(e => e.TotalScore) // or whatever secondary stat you track
                    .ToList();

                Console.WriteLine("[DEBUG] After Tie Resolution:");
                foreach (var entry in standings.Entries)
                    Console.WriteLine($"   Team: {entry.TeamName}, Wins: {entry.Wins}, Score: {entry.TotalScore}");
            }

            // Assign ranks after tie resolution
            Console.WriteLine("[DEBUG] Assigning final ranks...");
            for (int i = 0; i < standings.Entries.Count; i++)
            {
                standings.Entries[i].Rank = i + 1;
                Console.WriteLine($"   Team: {standings.Entries[i].TeamName}, Rank: {standings.Entries[i].Rank}");
            }

            Console.WriteLine("[DEBUG] Standings complete.");
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
                await Task.Delay(TimeSpan.FromSeconds(7));
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
                            //Console.WriteLine($"Sent new standings message for tournament {tournament.Name} in channel {channel.Name}.");
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
    }
}
