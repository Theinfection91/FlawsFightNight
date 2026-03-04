using Discord;
using Discord.WebSocket;
using FlawsFightNight.Commands;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands
{
    public class RegisterGuidToMemberLogic : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly MemberService _memberManager;
        private readonly UT2004StatsService _ut2004StatsManager;

        public RegisterGuidToMemberLogic(EmbedFactory embedFactory, GitBackupService gitBackupService, MemberService memberManager, UT2004StatsService ut2004StatsManager) : base("Register GUID To Member")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _memberManager = memberManager;
            _ut2004StatsManager = ut2004StatsManager;
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
                return _embedFactory.ErrorEmbed(Name, "Member profile not found.");
            }

            if (memberProfile.RegisteredUT2004GUIDs.Contains(guid))
            {
                return _embedFactory.ErrorEmbed(Name, "This GUID is already registered to the given member.");
            }
            memberProfile.RegisterUT2004GUID(guid);
            if (memberProfile.RegisteredUT2004GUIDs.Count > 1)
            {
                // Testing SeamlessRatings: for now calling it this way but will see how well it works and will optimize later. Need to have checks in place to avoid unnecessary calls of rebuilding profiles and to avoid performance issues.
                await _ut2004StatsManager.RebuildPlayerProfiles();
            }

            await _memberManager.SaveAndReloadMemberProfiles();
            _gitBackupService.EnqueueBackup();

            return _embedFactory.GenericEmbed(Name, $"Successfully registered GUID `{guid}` to member {(member as SocketGuildUser)!.DisplayName}.", Color.DarkGreen);
        }
    }
}
