using Discord;
using Discord.WebSocket;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.SettingsCommands
{
    public class RegisterGuidToMemberLogic : Logic
    {
        private readonly EmbedManager _embedManager;
        private readonly GitBackupManager _gitBackupManager;
        private readonly MemberManager _memberManager;

        public RegisterGuidToMemberLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, MemberManager memberManager) : base("Register GUID To Member")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _memberManager = memberManager;
        }

        public async Task<Embed?> RegisterGuidToMemberProcess(IUser member, string guid)
        {
            if (!_memberManager.DoesMemberProfileExist(member.Id))
            {
                var newProfile = _memberManager.CreateMemberProfile(member.Id, member.Username);
                _memberManager.AddProfileToDatabase(newProfile);
            }
            var memberProfile = _memberManager.GetMemberProfile(member.Id);
            if (memberProfile == null)
            {
                return _embedManager.ErrorEmbed(Name, "Member profile not found.");
            }

            if (memberProfile.RegisteredUT2004GUIDs.Contains(guid))
            {
                return _embedManager.ErrorEmbed(Name, "This GUID is already registered to the given member.");
            }
            memberProfile.RegisterUT2004GUID(guid);

            await _memberManager.SaveAndReloadMemberProfiles();
            _gitBackupManager.EnqueueBackup();

            return _embedManager.GenericEmbed(Name, $"Successfully registered GUID `{guid}` to member {(member as SocketGuildUser)!.DisplayName}.", Color.DarkGreen);
        }
    }
}
