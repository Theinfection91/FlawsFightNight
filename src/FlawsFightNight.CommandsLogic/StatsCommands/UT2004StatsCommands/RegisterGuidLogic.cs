using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.StatsCommands.UT2004StatsCommands
{
    public class RegisterGuidLogic : Logic
    {
        private readonly EmbedManager _embedManager;
        private readonly GitBackupManager _gitBackupManager;
        private readonly MemberManager _memberManager;
        private readonly UT2004StatsManager _ut2004StatsManager;
        public RegisterGuidLogic(EmbedManager embedManager,
                                 GitBackupManager gitBackupManager,
                                 MemberManager memberManager,
                                 UT2004StatsManager ut2004StatsManager) : base("Register GUID")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _memberManager = memberManager;
            _ut2004StatsManager = ut2004StatsManager;
        }
        public async Task<Embed> RegisterGuidProcess(SocketInteractionContext context, string guid)
        {
            if (_memberManager.IsUT2004GUIDRegistered(guid))
            {
                return _embedManager.ErrorEmbed(Name, $"The GUID `{guid}` is already registered to another account. Please check the GUID and try again.");
            }

            var memberProfile = _memberManager.GetMemberProfile(context.User.Id)!;
            if (memberProfile == null)
            {
                return _embedManager.ErrorEmbed(Name, "An error occurred while retrieving your member profile. Please try again later.");
            }
            memberProfile.RegisterUT2004GUID(guid);
            if (memberProfile.RegisteredUT2004GUIDs.Count > 1)
            {
                // Testing SeamlessRatings: for now calling it this way but will see how well it works and will optimize later. Need to have checks in place to avoid unnecessary calls of rebuilding profiles and to avoid performance issues.
                await _ut2004StatsManager.RebuildPlayerProfiles();
            }

            //var utProfile = _memberManager.GetUT2004PlayerProfile(guid);
            //if (utProfile == null) 
            //{
            //    return _embedManager.ErrorEmbed(Name, $"An error occurred while retrieving the UT2004 profile for GUID `{guid}`. Please ensure the GUID is correct and try again.");
            //}

            await _memberManager.SaveAndReloadMemberProfiles();
            _gitBackupManager.EnqueueBackup();

            return _embedManager.GenericEmbed(Name, $"The GUID `{guid}` has been successfully registered to your account.", Color.Blue);
        }
    }
}
