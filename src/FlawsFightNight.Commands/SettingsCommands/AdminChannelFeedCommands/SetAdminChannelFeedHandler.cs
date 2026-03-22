using Discord;
using FlawsFightNight.Services;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.AdminChannelFeedCommands
{
    public class SetAdminChannelFeedHandler : CommandHandler
    {
        private readonly DataContext _dataContext;
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;

        public SetAdminChannelFeedHandler(DataContext dataContext, EmbedFactory embedFactory, GitBackupService gitBackupService) : base("Set Admin Channel Feed")
        {
            _dataContext = dataContext ?? throw new System.ArgumentNullException(nameof(dataContext));
            _embedFactory = embedFactory ?? throw new System.ArgumentNullException(nameof(embedFactory));
            _gitBackupService = gitBackupService ?? throw new System.ArgumentNullException(nameof(gitBackupService));  
        }

        public async Task<Embed> Handle(IMessageChannel channel)
        {
            await _dataContext.SetAdminChannelFeedAsync(channel.Id);
            _gitBackupService.EnqueueBackup();
            return _embedFactory.SuccessEmbed("Admin Feed Configured", $"The admin channel feed has been successfully bound to <#{channel.Id}>. Critical logs and FTP alerts will now report here.");
        }
    }
}
