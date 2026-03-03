using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.SettingsCommands
{
    public class RemoveGuidFromMemberLogic : Logic
    {
        private readonly EmbedManager _embedManager;
        private readonly GitBackupManager _gitBackupManager;
        private readonly MemberManager _memberManager;

        public RemoveGuidFromMemberLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, MemberManager memberManager) : base("Remove GUID From Member")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _memberManager = memberManager;
        }

        public async Task<Embed?> RemoveGuidFromMemberProcess(IUser member,  string guid)
        {
            if (!_memberManager.DoesMemberProfileExist(member.Id))
            {
                var newProfile = _memberManager.CreateMemberProfile(member.Id, member.Username);
                _memberManager.AddProfileToDatabase(newProfile);

                await _memberManager.SaveAndReloadMemberProfiles();
                _gitBackupManager.EnqueueBackup();

                return _embedManager.ErrorEmbed(Name, "Member profile did not exist, but has now been created and a GUID can be added. A GUID can not be removed from a profile that didn't exist until now.");
            }

            var memberProfile = _memberManager.GetMemberProfile(member.Id);
            if (memberProfile == null)
            {
                return _embedManager.ErrorEmbed(Name, "Member profile not found.");
            }

            if (!memberProfile.RegisteredUT2004GUIDs.Contains(guid))
            {
                return _embedManager.ErrorEmbed(Name, "This GUID is not registered to the given member.");
            }
            memberProfile.RemoveUT2004GUID(guid);

            await _memberManager.SaveAndReloadMemberProfiles();
            _gitBackupManager.EnqueueBackup();

            return _embedManager.GenericEmbed(Name, $"Successfully removed GUID `{guid}` from member {member.Username}.", Color.DarkGreen);
        }
    }
}
