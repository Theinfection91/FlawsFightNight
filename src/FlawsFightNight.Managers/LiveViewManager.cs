using Discord;
using Discord.WebSocket;
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
        public LiveViewManager(DiscordSocketClient discordSocketClient, DataManager dataManager, EmbedManager embedManager) : base("LiveViewManager", dataManager)
        {
            _client = discordSocketClient;
            _embedManager = embedManager;
            StartMatchesLiveViewTask();
        }

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
    }
}

