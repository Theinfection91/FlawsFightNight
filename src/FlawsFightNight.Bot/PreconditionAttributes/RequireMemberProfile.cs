using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Services;
using System;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.PreconditionAttributes
{
    public class RequireMemberProfile : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            // Resolve MemberManager from DI
            var memberManager = services.GetService(typeof(MemberService)) as MemberService;
            if (memberManager == null)
                return PreconditionResult.FromError("MemberManager is not available in the service provider.");

            var userId = context.User.Id;

            // If profile exists, succeed
            if (memberManager.DoesMemberProfileExist(userId))
                return PreconditionResult.FromSuccess();

            // Create profile for invoking user
            var gitBackupManager = services.GetService(typeof(GitBackupService)) as GitBackupService;
            if (gitBackupManager == null) 
                return PreconditionResult.FromError("GitBackupManager is not available in the service provider.");
            string displayName = (context.User as SocketGuildUser)?.DisplayName ?? context.User.Username;
            var profile = memberManager.CreateMemberProfile(userId, displayName);
            memberManager.AddProfileToDatabase(profile);

            // Persist and reload data (await async save)
            try
            {
                await memberManager.SaveAndReloadMemberProfiles();
                gitBackupManager.EnqueueBackup();
            }
            catch (Exception ex)
            {
                // If persistence fails, return error so command won't run with inconsistent state
                return PreconditionResult.FromError($"Failed to create member profile: {ex.Message}");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}