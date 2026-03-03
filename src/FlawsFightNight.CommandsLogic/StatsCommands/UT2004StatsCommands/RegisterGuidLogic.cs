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
        public RegisterGuidLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, MemberManager memberManager) : base("Register GUID")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _memberManager = memberManager;
        }
        public async Task<Embed> RegisterGuidProcess(SocketInteractionContext context, string guid)
        {
            if (_memberManager.IsUT2004GUIDRegistered(guid))
            {
                return _embedManager.ErrorEmbed(Name, $"The GUID `{guid}` is already registered to another account. Please check the GUID and try again.");
            }

            MemberProfile memberProfile;
            if (!_memberManager.DoesMemberProfileExist(context.User.Id))
            {
                // If the user doesn't have a member profile yet, create one for them
                memberProfile = _memberManager.CreateNewMemberProfile(context.User.Id, (context.User as SocketGuildUser)!.DisplayName);
                _memberManager.AddProfileToDatabase(memberProfile);
            }

            memberProfile = _memberManager.GetMemberProfile(context.User.Id)!;
            if (memberProfile == null)
            {
                return _embedManager.ErrorEmbed(Name, "An error occurred while retrieving your member profile. Please try again later.");
            }
            _memberManager.RegisterUT2004GUIDToProfile(memberProfile, guid);

            var utProfile = _memberManager.GetUT2004PlayerProfile(guid);
            if (utProfile == null) 
            {
                return _embedManager.ErrorEmbed(Name, $"An error occurred while retrieving the UT2004 profile for GUID `{guid}`. Please ensure the GUID is correct and try again.");
            }

            await _memberManager.SaveAndReloadMemberProfiles();
            _gitBackupManager.EnqueueBackup();

            return _embedManager.GenericEmbed(Name, $"The GUID `{guid}` has been successfully registered to your account.", Color.Blue);
        }
    }
}
