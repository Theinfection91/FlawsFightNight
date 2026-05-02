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
        private readonly OpenSkillRatingService _openSkillService;

        public SuggestTeamsHandler(EmbedFactory embedFactory, MemberService memberService, OpenSkillRatingService openSkillRatingService) : base("Suggest Teams")
        {
            _embedFactory = embedFactory;
            _memberService = memberService;
            _openSkillService = openSkillRatingService;
        }

        public async Task<Embed> Handle(List<IUser> players, UT2004GameMode gameMode)
        {
            if (players.Count < 4 || players.Count > 10)
                return _embedFactory.ErrorEmbed(Name, "Please provide between 4 and 10 players (2v2 to 5v5).");

            if (players.Count % 2 != 0)
                return _embedFactory.ErrorEmbed(Name, "An even number of players is required to suggest balanced teams.");

            if (players.Select(p => p.Id).Distinct().Count() != players.Count)
                return _embedFactory.ErrorEmbed(Name, "Duplicate players detected. Please provide unique players.");

            var playerData = players.Select(user =>
            {
                var member = _memberService.GetMemberProfile(user.Id);
                if (member == null || member.RegisteredUT2004GUIDs.Count == 0)
                    return (Name: GetDisplayName(user), Mu: 25.0, Sigma: 25.0 / 3.0, HasProfile: false);

                var profile = _memberService.GetUT2004PlayerProfile(member.RegisteredUT2004GUIDs.First());
                if (profile == null)
                    return (Name: GetDisplayName(user), Mu: 25.0, Sigma: 25.0 / 3.0, HasProfile: false);

                var (mu, sigma) = profile.GetMuSigmaComposite(gameMode);
                return (Name: profile.CurrentName, Mu: mu, Sigma: sigma, HasProfile: true);
            }).ToList();

            int teamSize = players.Count / 2;
            var indices = Enumerable.Range(0, players.Count).ToList();

            double bestDiff = double.MaxValue;
            List<int>? bestTeamAIndices = null;
            double bestTeamAWinProb = 0.5;

            foreach (var combo in GetCombinations(indices, teamSize))
            {
                var comboList = combo.ToList();
                var remainingIndices = indices.Where(i => !comboList.Contains(i)).ToList();

                var teamAPlayers = comboList.Select(i => (playerData[i].Mu, playerData[i].Sigma)).ToList();
                var teamBPlayers = remainingIndices.Select(i => (playerData[i].Mu, playerData[i].Sigma)).ToList();

                double winProbA = _openSkillService.GetTeamAWinProbability(teamAPlayers, teamBPlayers);
                double diff = Math.Abs(winProbA - 0.5);

                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestTeamAIndices = comboList;
                    bestTeamAWinProb = winProbA;
                }
            }

            var teamA = bestTeamAIndices!.Select(i => (playerData[i].Name, DisplayRating: playerData[i].Mu - 3 * playerData[i].Sigma, playerData[i].HasProfile)).ToList();
            var teamB = indices.Where(i => !bestTeamAIndices!.Contains(i)).Select(i => (playerData[i].Name, DisplayRating: playerData[i].Mu - 3 * playerData[i].Sigma, playerData[i].HasProfile)).ToList();

            return _embedFactory.SuggestTeamsEmbed(teamA, teamB, bestTeamAWinProb, teamSize, gameMode);
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
