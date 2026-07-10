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

        public async Task<Embed> Handle(List<IUser> players, UT2004GameMode gameMode, int teamSizeChoice)
        {
            // Validate minimum players
            if (players.Count < teamSizeChoice * 2)
                return _embedFactory.ErrorEmbed(Name, $"Need at least {teamSizeChoice * 2} players for {teamSizeChoice}v{teamSizeChoice}. You provided {players.Count}.");

            var playerData = players.Select((user, index) =>
            {
                var member = _memberService.GetMemberProfile(user.Id);
                if (member == null || member.RegisteredUT2004GUIDs.Count == 0)
                    return (Index: index, Name: GetDisplayName(user), Mu: 25.0, Sigma: 25.0 / 3.0, HasProfile: false);

                var profile = _memberService.GetUT2004PlayerProfile(member.RegisteredUT2004GUIDs.First());
                if (profile == null)
                    return (Index: index, Name: GetDisplayName(user), Mu: 25.0, Sigma: 25.0 / 3.0, HasProfile: false);

                var (mu, sigma) = profile.GetMuSigmaComposite(gameMode);
                return (Index: index, Name: profile.CurrentName, Mu: mu, Sigma: sigma, HasProfile: true);
            }).ToList();

            int numTeams = players.Count / teamSizeChoice;
            int playersUsed = numTeams * teamSizeChoice;
            var unusedPlayers = playerData.Where(p => p.Index >= playersUsed).ToList();

            // Sort players by rating (descending)
            var sortedPlayers = playerData.Where(p => p.Index < playersUsed)
                .OrderByDescending(p => p.Mu)
                .ToList();

            // Multi-start greedy: try random orderings, keep best result
            var bestTeams = RunGreedyAssignment(sortedPlayers, numTeams, teamSizeChoice);
            double bestBalance = GetTeamWinProbabilityBalance(bestTeams);

            for (int attempt = 0; attempt < 4; attempt++)
            {
                var shuffledPlayers = sortedPlayers.OrderBy(_ => Random.Shared.Next()).ToList();
                var candidateTeams = RunGreedyAssignment(shuffledPlayers, numTeams, teamSizeChoice);
                double candidateBalance = GetTeamWinProbabilityBalance(candidateTeams);

                if (candidateBalance < bestBalance)
                {
                    bestBalance = candidateBalance;
                    bestTeams = candidateTeams;
                }
            }

            var teams = bestTeams;

            // Aggressive refinement: 50 iterations, try all swaps
            RefineTeamsWithSwaps(teams, maxIterations: 50);

            // Format teams for embed
            var formattedTeams = teams.Select(team => team
                .Select(p => (p.Name, DisplayRating: p.Mu - 3 * p.Sigma, p.HasProfile, p.Sigma))
                .ToList())
                .ToList();

            return _embedFactory.SuggestTeamsEmbed(formattedTeams, gameMode, teamSizeChoice, unusedPlayers.Count);
        }

        private List<List<(int Index, string Name, double Mu, double Sigma, bool HasProfile)>> RunGreedyAssignment(
            List<(int Index, string Name, double Mu, double Sigma, bool HasProfile)> players,
            int numTeams,
            int teamSizeChoice)
        {
            var teams = Enumerable.Range(0, numTeams)
                .Select(_ => new List<(int Index, string Name, double Mu, double Sigma, bool HasProfile)>())
                .ToList();

            foreach (var player in players)
            {
                int weakestTeamIdx = teams
                    .Select((t, idx) => (Index: idx, TotalStrength: t.Sum(p => p.Mu - 2 * p.Sigma)))
                    .Where(x => teams[x.Index].Count < teamSizeChoice)
                    .OrderBy(x => x.TotalStrength)
                    .First()
                    .Index;

                teams[weakestTeamIdx].Add(player);
            }

            return teams;
        }

        private void RefineTeamsWithSwaps(
            List<List<(int Index, string Name, double Mu, double Sigma, bool HasProfile)>> teams,
            int maxIterations = 20)
        {
            bool improved = true;
            int iterations = 0;

            while (improved && iterations < maxIterations)
            {
                improved = false;
                iterations++;

                double currentBalance = GetTeamWinProbabilityBalance(teams);

                for (int i = 0; i < teams.Count && !improved; i++)
                {
                    for (int j = i + 1; j < teams.Count && !improved; j++)
                    {
                        for (int pi = 0; pi < teams[i].Count && !improved; pi++)
                        {
                            for (int pj = 0; pj < teams[j].Count && !improved; pj++)
                            {
                                // Swap
                                var temp = teams[i][pi];
                                teams[i][pi] = teams[j][pj];
                                teams[j][pj] = temp;

                                double newBalance = GetTeamWinProbabilityBalance(teams);

                                if (newBalance < currentBalance)
                                {
                                    improved = true;
                                    currentBalance = newBalance;
                                }
                                else
                                {
                                    // Swap back
                                    temp = teams[i][pi];
                                    teams[i][pi] = teams[j][pj];
                                    teams[j][pj] = temp;
                                }
                            }
                        }
                    }
                }
            }
        }

        private double GetTeamWinProbabilityBalance(List<List<(int Index, string Name, double Mu, double Sigma, bool HasProfile)>> teams)
        {
            double maxImbalance = 0;

            for (int i = 0; i < teams.Count; i++)
            {
                for (int j = i + 1; j < teams.Count; j++)
                {
                    var teamAPlayers = teams[i].Select(p => (p.Mu, p.Sigma)).ToList();
                    var teamBPlayers = teams[j].Select(p => (p.Mu, p.Sigma)).ToList();

                    if (teamAPlayers.Count > 0 && teamBPlayers.Count > 0)
                    {
                        double winProb = _openSkillService.GetTeamAWinProbability(teamAPlayers, teamBPlayers);
                        double imbalance = Math.Abs(winProb - 0.5);
                        maxImbalance = Math.Max(maxImbalance, imbalance);
                    }
                }
            }

            return maxImbalance;
        }

        private static string GetDisplayName(IUser user) =>
            user is SocketGuildUser g && !string.IsNullOrEmpty(g.DisplayName) ? g.DisplayName : user.Username;
    }
}
