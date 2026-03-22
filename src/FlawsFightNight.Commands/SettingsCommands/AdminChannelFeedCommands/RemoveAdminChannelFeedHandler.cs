using Discord;
using FlawsFightNight.Services;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.AdminChannelFeedCommands
{
    public class RemoveAdminChannelFeedHandler : CommandHandler
    {
        private readonly DataContext _dataContext;
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;

        public RemoveAdminChannelFeedHandler(DataContext dataContext, EmbedFactory embedFactory, GitBackupService gitBackupService) : base("Remove Admin Channel Feed")
        {
            _dataContext = dataContext ?? throw new System.ArgumentNullException(nameof(dataContext));
            _embedFactory = embedFactory ?? throw new System.ArgumentNullException(nameof(embedFactory));
            _gitBackupService = gitBackupService ?? throw new System.ArgumentNullException(nameof(gitBackupService));
        }

        public async Task<Embed> Handle()
        {
            await _dataContext.SetAdminChannelFeedAsync(0); // Zero represents no channel set
            _gitBackupService.EnqueueBackup();
            return _embedFactory.SuccessEmbed("Admin Feed Removed", "The admin channel feed has been removed. You will no longer receive alerts in Discord.");
        }
    }
}
