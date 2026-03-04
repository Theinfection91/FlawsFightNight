using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands
{
    public class RemoveGuidFromMemberHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly MemberService _memberManager;
        private readonly UT2004StatsService _ut2004StatsManager;

        public RemoveGuidFromMemberHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, MemberService memberManager, UT2004StatsService ut2004StatsManager) : base("Remove GUID From Member")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _memberManager = memberManager;
            _ut2004StatsManager = ut2004StatsManager;
        }

        public async Task<Embed?> RemoveGuidFromMemberProcess(IUser member, string guid)
        {
            if (!_memberManager.DoesMemberProfileExist(member.Id))
            {
                var newProfile = _memberManager.CreateMemberProfile(member.Id, member.Username);
                _memberManager.AddProfileToDatabase(newProfile);

                await _memberManager.SaveAndReloadMemberProfiles();
                _gitBackupService.EnqueueBackup();

                return _embedFactory.ErrorEmbed(Name, "Member profile did not exist, but has now been created and a GUID can be added. A GUID can not be removed from a profile that didn't exist until now.");
            }

            var memberProfile = _memberManager.GetMemberProfile(member.Id);
            if (memberProfile == null)
            {
                return _embedFactory.ErrorEmbed(Name, "Member profile not found.");
            }

            if (!memberProfile.RegisteredUT2004GUIDs.Contains(guid))
            {
                return _embedFactory.ErrorEmbed(Name, "This GUID is not registered to the given member.");
            }
            memberProfile.RemoveUT2004GUID(guid);
            if (memberProfile.RegisteredUT2004GUIDs.Count is not 0)
            {
                // Testing: Trigger rebuild when removing a secondary GUID
                await _ut2004StatsManager.RebuildPlayerProfiles();
            }

            await _memberManager.SaveAndReloadMemberProfiles();
            _gitBackupService.EnqueueBackup();

            return _embedFactory.GenericEmbed(Name, $"Successfully removed GUID `{guid}` from member {member.Username}.", Color.DarkGreen);
        }
    }
}
