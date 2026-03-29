using Discord;
using FlawsFightNight.Services;
using FlawsFightNight.Services.Logging;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.AdminChannelFeedCommands
{
    public class RemoveAdminChannelFeedHandler : CommandHandler
    {
        private readonly DataContext _dataContext;
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly ILogger<RemoveAdminChannelFeedHandler> _logger;

        public RemoveAdminChannelFeedHandler(DataContext dataContext, EmbedFactory embedFactory, GitBackupService gitBackupService, ILogger<RemoveAdminChannelFeedHandler> logger) : base("Remove Admin Channel Feed")
        {
            _dataContext = dataContext ?? throw new System.ArgumentNullException(nameof(dataContext));
            _embedFactory = embedFactory ?? throw new System.ArgumentNullException(nameof(embedFactory));
            _gitBackupService = gitBackupService ?? throw new System.ArgumentNullException(nameof(gitBackupService));
            _logger = logger;
        }

        public async Task<Embed> Handle()
        {
            await _dataContext.SetAdminChannelFeedAsync(0); // Zero represents no channel set
            _gitBackupService.EnqueueBackup();
            _logger.LogInformation(AdminFeedEvents.AdminActionTaken, "Admin feed channel removed.");
            return _embedFactory.SuccessEmbed("Admin Feed Removed", "The admin channel feed has been removed. You will no longer receive alerts in Discord.");
        }
    }
}
