using Discord;
using Discord.Interactions;
using FlawsFightNight.Commands;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands
{
    public class RemoveGuidHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly MemberService _memberManager;

        public RemoveGuidHandler(EmbedFactory embedFactory,
                               GitBackupService gitBackupService,
                               MemberService memberManager) : base("Remove UT2004 GUID")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _memberManager = memberManager;
        }

        public async Task<Embed> RemoveGuidProcess(SocketInteractionContext context, string guid)
        {
            var memberProfile = _memberManager.GetMemberProfile(context.User.Id)!;
            if (memberProfile == null)
            {
                return _embedFactory.ErrorEmbed(Name, "An error occurred while retrieving your member profile. Please try again later.");
            }

            if (!memberProfile.RegisteredUT2004GUIDs.Contains(guid))
            {
                var registeredGuids = memberProfile.RegisteredUT2004GUIDs.Count == 0 ? "You currently do not have any GUIDs registered to your profile." : $"You currently have the following GUIDs registered: {string.Join(", ", memberProfile.RegisteredUT2004GUIDs)}";

                return _embedFactory.ErrorEmbed(Name, $"The GUID `{guid}` is not registered to your profile. {registeredGuids}");
            }

            if (memberProfile.RegisteredUT2004GUIDs.Count > 1)
            {
                // If a user has more than one GUID an admin must remove GUIDs for them to avoid potential issues with SeamlessRatings
                return _embedFactory.ErrorEmbed(Name, $"You have multiple GUIDs registered to your profile. To avoid potential issues with SeamlessRatings, you must contact an administrator to remove GUIDs from your profile.");
            }
            memberProfile.RemoveUT2004GUID(guid);

            await _memberManager.SaveAndReloadMemberProfiles();
            _gitBackupService.EnqueueBackup();

            return _embedFactory.GenericEmbed(Name, $"The GUID `{guid}` has been successfully removed from your profile.", Color.DarkGreen);
        }
    }
}
