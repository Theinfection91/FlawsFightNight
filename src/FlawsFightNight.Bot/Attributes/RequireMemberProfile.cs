using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Services;
using System;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Attributes
{
    public class RequireMemberProfile : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            // Resolve memberService from DI
            var memberService = services.GetService(typeof(MemberService)) as MemberService;
            if (memberService == null)
                return PreconditionResult.FromError("memberService is not available in the service provider.");

            var userId = context.User.Id;

            // If profile exists, succeed
            if (memberService.DoesMemberProfileExist(userId))
                return PreconditionResult.FromSuccess();

            // Create profile for invoking user
            var gitBackupService = services.GetService(typeof(GitBackupService)) as GitBackupService;
            if (gitBackupService == null) 
                return PreconditionResult.FromError("gitBackupService is not available in the service provider.");
            string displayName = (context.User as SocketGuildUser)?.DisplayName ?? context.User.Username;
            var profile = memberService.CreateMemberProfile(userId, displayName);
            memberService.AddProfileToDatabase(profile);

            // Persist and reload data (await async save)
            try
            {
                await memberService.SaveAndReloadMemberProfiles();
                gitBackupService.EnqueueBackup();
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