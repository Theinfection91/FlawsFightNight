using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models.Stats;
using FlawsFightNight.Services;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands
{
    public class SetLeaderboardChannelHandler : CommandHandler
    {
        private readonly DataContext _dataContext;
        private readonly EmbedFactory _embedFactory;

        public SetLeaderboardChannelHandler(DataContext dataContext, EmbedFactory embedFactory)
            : base("Set Leaderboard Channel")
        {
            _dataContext = dataContext;
            _embedFactory = embedFactory;
        }

        public async Task<Embed> Handle(IMessageChannel channel, LeaderboardChannelTypes channelType)
        {
            var existing = _dataContext.GetLeaderboardChannel(channel.Id);
            if (existing != null)
                return _embedFactory.ErrorEmbed(Name,
                    $"<#{channel.Id}> is already registered as a **{existing.Type}** leaderboard channel.\n\nUse `/settings leaderboard_channel remove` first if you want to reassign it.");

            var channelData = new LeaderboardChannelData
            {
                ChannelId = channel.Id,
                Type = channelType
            };

            await _dataContext.AddLeaderboardChannel(channelData);
            return _embedFactory.SetLeaderboardChannelSuccess(channel, channelType);
        }
    }
}
