using Discord;
using FlawsFightNight.Services;
using FlawsFightNight.Services.Logging;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.AdminChannelFeedCommands
{
    public class SetAdminChannelFeedHandler : CommandHandler
    {
        private readonly DataContext _dataContext;
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly ILogger<SetAdminChannelFeedHandler> _logger;

        public SetAdminChannelFeedHandler(DataContext dataContext, EmbedFactory embedFactory, GitBackupService gitBackupService, ILogger<SetAdminChannelFeedHandler> logger) : base("Set Admin Channel Feed")
        {
            _dataContext = dataContext ?? throw new System.ArgumentNullException(nameof(dataContext));
            _embedFactory = embedFactory ?? throw new System.ArgumentNullException(nameof(embedFactory));
            _gitBackupService = gitBackupService ?? throw new System.ArgumentNullException(nameof(gitBackupService));
            _logger = logger;
        }

        public async Task<Embed> Handle(IMessageChannel channel)
        {
            await _dataContext.SetAdminChannelFeedAsync(channel.Id);
            _gitBackupService.EnqueueBackup();
            _logger.LogInformation(AdminFeedEvents.AdminActionTaken, "Admin feed channel set to #{ChannelId}.", channel.Id);
            return _embedFactory.SuccessEmbed("Admin Feed Configured", $"The admin channel feed has been successfully bound to <#{channel.Id}>. Critical logs and FTP alerts will now report here.");
        }
    }
}
