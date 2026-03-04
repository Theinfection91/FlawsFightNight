using Discord;
using FlawsFightNight.Managers;
using System.Linq;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.StatsCommands.UT2004StatsCommands
{
    public class MyPlayerProfileLogic : Logic
    {
        private readonly EmbedManager _embedManager;
        private readonly MemberManager _memberManager;

        public MyPlayerProfileLogic(EmbedManager embedManager, MemberManager memberManager) : base("My Player Profile")
        {
            _embedManager = embedManager;
            _memberManager = memberManager;
        }

        public Task<(Embed embed, bool hasProfile)> MyPlayerProfileProcess(ulong discordId)
        {
            var memberProfile = _memberManager.GetMemberProfile(discordId);
            if (memberProfile == null || memberProfile.RegisteredUT2004GUIDs.Count == 0)
            {
                return Task.FromResult<(Embed, bool)>((
                    _embedManager.ErrorEmbed(Name, "You don't have a UT2004 GUID registered. Use `/stats ut2004 register_guid` to link your profile."),
                    false));
            }

            var guid = memberProfile.RegisteredUT2004GUIDs.First();
            var utProfile = _memberManager.GetUT2004PlayerProfile(guid);
            if (utProfile == null)
            {
                return Task.FromResult<(Embed, bool)>((
                    _embedManager.ErrorEmbed(Name, $"No UT2004 stats found for your registered GUID `{guid}`. Stats may not have been processed yet."),
                    false));
            }

            return Task.FromResult<(Embed, bool)>((_embedManager.UT2004ProfileGeneralEmbed(utProfile), true));
        }
    }
}
