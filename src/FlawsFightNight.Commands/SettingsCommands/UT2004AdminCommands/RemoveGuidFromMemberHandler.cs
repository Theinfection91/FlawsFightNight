using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class RemoveGuidFromMemberHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly MemberService _memberService;
        private readonly UT2004StatsService _ut2004StatsService;

        public RemoveGuidFromMemberHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, MemberService memberService, UT2004StatsService ut2004StatsService) : base("Remove GUID From Member")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _memberService = memberService;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<Embed?> RemoveGuidFromMemberProcess(IUser member, string guid)
        {
            if (!_memberService.DoesMemberProfileExist(member.Id))
            {
                var newProfile = _memberService.CreateMemberProfile(member.Id, member.Username);
                _memberService.AddProfileToDatabase(newProfile);

                await _memberService.SaveAndReloadMemberProfiles();
                _gitBackupService.EnqueueBackup();

                return _embedFactory.ErrorEmbed(Name, "Member profile did not exist, but has now been created and a GUID can be added. A GUID can not be removed from a profile that didn't exist until now.");
            }

            var memberProfile = _memberService.GetMemberProfile(member.Id);
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
                await _ut2004StatsService.RebuildPlayerProfiles();
            }

            await _memberService.SaveAndReloadMemberProfiles();
            _gitBackupService.EnqueueBackup();

            return _embedFactory.GenericEmbed(Name, $"Successfully removed GUID `{guid}` from member {member.Username}.", Color.DarkGreen);
        }
    }
}
