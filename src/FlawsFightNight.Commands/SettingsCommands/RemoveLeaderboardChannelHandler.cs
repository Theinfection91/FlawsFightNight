using Discord;
using FlawsFightNight.Services;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands
{
    public class RemoveLeaderboardChannelHandler : CommandHandler
    {
        private readonly DataContext _dataContext;
        private readonly EmbedFactory _embedFactory;

        public RemoveLeaderboardChannelHandler(DataContext dataContext, EmbedFactory embedFactory)
            : base("Remove Leaderboard Channel")
        {
            _dataContext = dataContext;
            _embedFactory = embedFactory;
        }

        public async Task<Embed> Handle(IMessageChannel channel)
        {
            var existing = _dataContext.GetLeaderboardChannel(channel.Id);
            if (existing == null)
                return _embedFactory.ErrorEmbed(Name,
                    $"<#{channel.Id}> is not currently registered as a leaderboard channel.");

            // Clean up the old LiveView message so the orphaned dropdown is removed
            if (existing.MessageId != 0)
            {
                try
                {
                    var oldMsg = await channel.GetMessageAsync(existing.MessageId);
                    if (oldMsg is IUserMessage userMsg)
                        await userMsg.DeleteAsync();
                }
                catch { /* message already deleted or inaccessible */ }
            }

            await _dataContext.RemoveLeaderboardChannel(channel.Id);
            return _embedFactory.RemoveLeaderboardChannelSuccess(channel);
        }
    }
}