using Discord;
using Discord.Interactions;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.StatsCommands.UT2004StatsCommands
{
    public class RemoveGuidLogic : Logic
    {
        private readonly EmbedManager _embedManager;
        private readonly GitBackupManager _gitBackupManager;
        private readonly MemberManager _memberManager;

        public RemoveGuidLogic(EmbedManager embedManager,
                               GitBackupManager gitBackupManager,
                               MemberManager memberManager) : base("Remove UT2004 GUID")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _memberManager = memberManager;
        }

        public async Task<Embed> RemoveGuidProcess(SocketInteractionContext context, string guid)
        {
            var memberProfile = _memberManager.GetMemberProfile(context.User.Id)!;
            if (memberProfile == null)
            {
                return _embedManager.ErrorEmbed(Name, "An error occurred while retrieving your member profile. Please try again later.");
            }

            if (!memberProfile.RegisteredUT2004GUIDs.Contains(guid))
            {
                var registeredGuids = memberProfile.RegisteredUT2004GUIDs.Count == 0 ? "You currently do not have any GUIDs registered to your profile." : $"You currently have the following GUIDs registered: {string.Join(", ", memberProfile.RegisteredUT2004GUIDs)}";

                return _embedManager.ErrorEmbed(Name, $"The GUID `{guid}` is not registered to your profile. {registeredGuids}");
            }

            if (memberProfile.RegisteredUT2004GUIDs.Count > 1)
            {
                // If a user has more than one GUID an admin must remove GUIDs for them to avoid potential issues with SeamlessRatings
                return _embedManager.ErrorEmbed(Name, $"You have multiple GUIDs registered to your profile. To avoid potential issues with SeamlessRatings, you must contact an administrator to remove GUIDs from your profile.");
            }
            memberProfile.RemoveUT2004GUID(guid);

            await _memberManager.SaveAndReloadMemberProfiles();
            _gitBackupManager.EnqueueBackup();

            return _embedManager.GenericEmbed(Name, $"The GUID `{guid}` has been successfully removed from your profile.", Color.DarkGreen);
        }
    }
}
