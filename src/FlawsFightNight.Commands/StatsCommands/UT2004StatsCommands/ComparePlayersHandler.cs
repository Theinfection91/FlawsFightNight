using Discord;
using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Models.UT2004;
using FlawsFightNight.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands
{
    public class ComparePlayersHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly MemberService _memberService;
        private readonly OpenSkillRatingService _openSkillService;

        public ComparePlayersHandler(EmbedFactory embedFactory, MemberService memberService, OpenSkillRatingService openSkillRatingService) : base("Compare Players")
        {
            _embedFactory = embedFactory;
            _memberService = memberService;
            _openSkillService = openSkillRatingService;
        }

        public async Task<(Embed embed, bool hasBothProfiles)> Handle(IUser player1, IUser player2)
        {
            if (player1.Id == player2.Id)
                return (_embedFactory.ErrorEmbed(Name, "You can't compare a player with themselves. Pick two different players!"), false);

            var member1 = _memberService.GetMemberProfile(player1.Id);
            var member2 = _memberService.GetMemberProfile(player2.Id);

            if (member1 == null || member1.RegisteredUT2004GUIDs.Count == 0)
                return (_embedFactory.ErrorEmbed(Name, $"**{player1.Username}** does not have a registered UT2004 GUID."), false);
            if (member2 == null || member2.RegisteredUT2004GUIDs.Count == 0)
                return (_embedFactory.ErrorEmbed(Name, $"**{player2.Username}** does not have a registered UT2004 GUID."), false);

            var profile1 = _memberService.GetUT2004PlayerProfile(member1.RegisteredUT2004GUIDs.First());
            var profile2 = _memberService.GetUT2004PlayerProfile(member2.RegisteredUT2004GUIDs.First());

            if (profile1 == null)
                return (_embedFactory.ErrorEmbed(Name, $"No UT2004 stats found for **{player1.Username}**. Stats may not have been processed yet."), false);
            if (profile2 == null)
                return (_embedFactory.ErrorEmbed(Name, $"No UT2004 stats found for **{player2.Username}**. Stats may not have been processed yet."), false);

            double winProb1 = ComputeWinProb(profile1, profile2, UT2004GameMode.Unknown);
            return (_embedFactory.ComparePlayersOverviewEmbed(profile1, profile2, winProb1), true);
        }

        /// <summary>
        /// Resolves a section embed for a component interaction by Discord user IDs.
        /// Returns null if either player's profile cannot be resolved.
        /// </summary>
        public Embed? HandleSection(ulong player1Id, ulong player2Id, string section)
        {
            var member1 = _memberService.GetMemberProfile(player1Id);
            var member2 = _memberService.GetMemberProfile(player2Id);

            if (member1 == null || member1.RegisteredUT2004GUIDs.Count == 0 ||
                member2 == null || member2.RegisteredUT2004GUIDs.Count == 0)
                return null;

            var profile1 = _memberService.GetUT2004PlayerProfile(member1.RegisteredUT2004GUIDs.First());
            var profile2 = _memberService.GetUT2004PlayerProfile(member2.RegisteredUT2004GUIDs.First());

            if (profile1 == null || profile2 == null)
                return null;

            var gameMode = section switch
            {
                "ictf" => UT2004GameMode.iCTF,
                "tam"  => UT2004GameMode.TAM,
                "ibr"  => UT2004GameMode.iBR,
                _      => UT2004GameMode.Unknown
            };

            double winProb1 = ComputeWinProb(profile1, profile2, gameMode);
            return _embedFactory.ComparePlayersSectionEmbed(profile1, profile2, section, winProb1);
        }

        private double ComputeWinProb(UT2004PlayerProfile p1, UT2004PlayerProfile p2, UT2004GameMode gameMode)
        {
            var (mu1, sigma1) = GetModeMuSigma(p1, gameMode);
            var (mu2, sigma2) = GetModeMuSigma(p2, gameMode);
            return _openSkillService.GetTeamAWinProbability(
                new List<(double Mu, double Sigma)> { (mu1, sigma1) },
                new List<(double Mu, double Sigma)> { (mu2, sigma2) });
        }

        private static (double Mu, double Sigma) GetModeMuSigma(UT2004PlayerProfile profile, UT2004GameMode gameMode) =>
            gameMode switch
            {
                UT2004GameMode.iCTF => (profile.CaptureTheFlagRating.Mu, profile.CaptureTheFlagRating.Sigma),
                UT2004GameMode.TAM  => (profile.TAMRating.Mu, profile.TAMRating.Sigma),
                UT2004GameMode.iBR  => (profile.BombingRunRating.Mu, profile.BombingRunRating.Sigma),
                _                   => GetCompositeMuSigma(profile)
            };

        private static (double Mu, double Sigma) GetCompositeMuSigma(UT2004PlayerProfile profile)
        {
            int total = profile.TotalCTFMatches + profile.TotalTAMMatches + profile.TotalBRMatches;
            if (total == 0)
                return (25.0, 25.0 / 3.0);

            double mu =
                (profile.CaptureTheFlagRating.Mu * profile.TotalCTFMatches +
                 profile.TAMRating.Mu * profile.TotalTAMMatches +
                 profile.BombingRunRating.Mu * profile.TotalBRMatches) / total;

            double sigma =
                (profile.CaptureTheFlagRating.Sigma * profile.TotalCTFMatches +
                 profile.TAMRating.Sigma * profile.TotalTAMMatches +
                 profile.BombingRunRating.Sigma * profile.TotalBRMatches) / total;

            return (mu, sigma);
        }
    }
}
