using Discord;
using Discord.WebSocket;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class RegisterGuidToMemberHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly MemberService _memberService;
        private readonly UT2004StatsService _ut2004StatsService;

        public RegisterGuidToMemberHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, MemberService memberService, UT2004StatsService ut2004StatsService) : base("Register GUID To Member")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _memberService = memberService;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<Embed?> RegisterGuidToMemberProcess(IUser member, string guid)
        {
            if (!_ut2004StatsService.IsValidGuid(guid))
            {
                return _embedFactory.ErrorEmbed(Name, $"The GUID `{guid}` is not in a valid format. Please provide a valid GUID. Example: c392f9fe569e068a6523d5d78c6b57e7");
            }

            if (_memberService.IsUT2004GUIDRegistered(guid, out var existingMemberProfile))
            {
                return _embedFactory.ErrorEmbed(Name, $"The GUID `{guid}` is already registered to {(existingMemberProfile != null ? (existingMemberProfile.DisplayName ?? "Unknown") : "Unknown")}(#{(existingMemberProfile != null ? existingMemberProfile.DiscordId.ToString() : "Unknown")}). Please check the GUID and try again.");
            }

            if (!_memberService.DoesMemberProfileExist(member.Id))
            {
                var newProfile = _memberService.CreateMemberProfile(member.Id, member.Username);
                _memberService.AddProfileToDatabase(newProfile);
            }
            var memberProfile = _memberService.GetMemberProfile(member.Id);
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
                await _ut2004StatsService.RebuildPlayerProfiles();
            }

            await _memberService.SaveAndReloadMemberProfiles();
            _gitBackupService.EnqueueBackup();

            return _embedFactory.GenericEmbed(Name, $"Successfully registered GUID `{guid}` to member {(member as SocketGuildUser)!.DisplayName}.", Color.DarkGreen);
        }
    }
}
