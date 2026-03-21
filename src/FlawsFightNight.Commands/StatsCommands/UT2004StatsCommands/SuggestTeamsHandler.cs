using Discord;
using Discord.WebSocket;
using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Models.UT2004;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands
{
    public class SuggestTeamsHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly MemberService _memberService;

        public SuggestTeamsHandler(EmbedFactory embedFactory, MemberService memberService) : base("Suggest Teams")
        {
            _embedFactory = embedFactory;
            _memberService = memberService;
        }

        public Task<Embed> Handle(List<IUser> players, UT2004GameMode gameMode)
        {
            if (players.Count < 4 || players.Count > 10)
                return Task.FromResult(_embedFactory.ErrorEmbed(Name, "Please provide between 4 and 10 players (2v2 to 5v5)."));

            if (players.Count % 2 != 0)
                return Task.FromResult(_embedFactory.ErrorEmbed(Name, "An even number of players is required to suggest balanced teams."));

            if (players.Select(p => p.Id).Distinct().Count() != players.Count)
                return Task.FromResult(_embedFactory.ErrorEmbed(Name, "Duplicate players detected. Please provide unique players."));

            var playerData = players.Select(user =>
            {
                var member = _memberService.GetMemberProfile(user.Id);
                if (member == null || member.RegisteredUT2004GUIDs.Count == 0)
                    return (Name: GetDisplayName(user), Score: 0.0, HasProfile: false);

                var profile = _memberService.GetUT2004PlayerProfile(member.RegisteredUT2004GUIDs.First());
                if (profile == null)
                    return (Name: GetDisplayName(user), Score: 0.0, HasProfile: false);

                return (Name: profile.CurrentName, Score: GetModeRating(profile, gameMode), HasProfile: true);
            }).ToList();

            int teamSize = players.Count / 2;
            var indices = Enumerable.Range(0, players.Count).ToList();

            double bestDiff = double.MaxValue;
            List<int>? bestTeamAIndices = null;

            foreach (var combo in GetCombinations(indices, teamSize))
            {
                var comboList = combo.ToList();
                double teamAScore = comboList.Sum(i => playerData[i].Score);
                double teamBScore = indices.Where(i => !comboList.Contains(i)).Sum(i => playerData[i].Score);
                double diff = Math.Abs(teamAScore - teamBScore);

                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestTeamAIndices = comboList;
                }
            }

            var teamA = bestTeamAIndices!.Select(i => playerData[i]).ToList();
            var teamB = indices.Where(i => !bestTeamAIndices!.Contains(i)).Select(i => playerData[i]).ToList();

            return Task.FromResult(_embedFactory.SuggestTeamsEmbed(teamA, teamB, bestDiff, teamSize, gameMode));
        }

        private static double GetModeRating(UT2004PlayerProfile profile, UT2004GameMode gameMode) =>
            gameMode switch
            {
                UT2004GameMode.iCTF => profile.CaptureTheFlagRating.Rating,
                UT2004GameMode.TAM  => profile.TAMRating.Rating,
                UT2004GameMode.iBR  => profile.BombingRunRating.Rating,
                _                   => ComputeCompositeScore(profile)
            };

        private static double ComputeCompositeScore(UT2004PlayerProfile profile)
        {
            int total = profile.TotalCTFMatches + profile.TotalTAMMatches + profile.TotalBRMatches;
            if (total == 0)
                return 0.0;

            double weighted =
                (profile.CaptureTheFlagRating.Rating * profile.TotalCTFMatches) +
                (profile.TAMRating.Rating * profile.TotalTAMMatches) +
                (profile.BombingRunRating.Rating * profile.TotalBRMatches);

            return weighted / total;
        }

        private static string GetDisplayName(IUser user) =>
            user is SocketGuildUser g && !string.IsNullOrEmpty(g.DisplayName) ? g.DisplayName : user.Username;

        private static IEnumerable<IEnumerable<int>> GetCombinations(List<int> list, int size)
        {
            if (size == 0) { yield return Enumerable.Empty<int>(); yield break; }
            for (int i = 0; i < list.Count; i++)
            {
                var rest = list.Skip(i + 1).ToList();
                foreach (var tail in GetCombinations(rest, size - 1))
                    yield return tail.Prepend(list[i]);
            }
        }
    }
}
