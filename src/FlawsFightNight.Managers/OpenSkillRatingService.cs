using OpenSkillSharp.Models;
using OpenSkillSharp.Rating;
using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Helpers.UT2004;
using FlawsFightNight.Core.Models.UT2004;

namespace FlawsFightNight.Managers
{
    /// <summary>
    /// Bridges OpenSkillSharp with the UT2004 player profile system.
    /// Uses the Plackett-Luce model by default for team-based rating.
    /// </summary>
    public class OpenSkillRatingService
    {
        private readonly PlackettLuce _model;
        private readonly SeamlessRatingsMapper _ratingsMapper;
        private const double Tau = 0.0083;
        private const int MinHumansPerTeam = 1;

        public int SkippedImbalancedMatches { get; set; }
        public int SkippedInsufficientPlayers { get; set; }

        public OpenSkillRatingService(SeamlessRatingsMapper ratingsMapper)
        {
            _ratingsMapper = ratingsMapper;
            _model = new PlackettLuce
            {
                Mu = 25.0,
                Sigma = 25.0 / 3.0
            };
        }

        /// <summary>
        /// Updates Mu/Sigma on every non-bot player profile based on a single match result.
        /// Only rates matches where teams were structurally balanced (same total team size).
        /// </summary>
        public void UpdateRatingsForMatch(UT2004StatLog match, Dictionary<string, UT2004PlayerProfile> profiles)
        {
            // First check: Do teams have the same total size (including bots)?
            if (match.Players.Count < 2)
                return;

            var teamSizes = match.Players.Select(team => team.Count).ToList();
            if (teamSizes.Distinct().Count() > 1)
            {
                // Teams have different total sizes - match was structurally imbalanced
                SkippedImbalancedMatches++;
                return;
            }

            // Build teams of IRating objects aligned with player GUIDs
            var teams = new List<ITeam>();
            var teamGuids = new List<List<string>>();
            var teamRanks = new List<double>();
            var humanCounts = new List<int>();

            foreach (var teamPlayers in match.Players)
            {
                var humanPlayers = teamPlayers.Where(p => !p.IsBot && !string.IsNullOrEmpty(p.Guid)).ToList();
                if (humanPlayers.Count == 0)
                    continue;

                humanCounts.Add(humanPlayers.Count);

                var players = new List<IRating>();
                var guids = new List<string>();

                foreach (var player in humanPlayers)
                {
                    var resolvedGuid = _ratingsMapper.Resolve(player.Guid!);
                    var profile = profiles[resolvedGuid];
                    profile.GetMuSigma(match.GameMode, out double mu, out double sigma);
                    players.Add(new Rating { Mu = mu, Sigma = sigma });
                    guids.Add(resolvedGuid);
                }

                teams.Add(new Team { Players = players });
                teamGuids.Add(guids);

                // Determine rank: winners get rank 1, losers get rank 2
                bool isWinningTeam = humanPlayers.Any(p => p.IsWinner);
                teamRanks.Add(isWinningTeam ? 1.0 : 2.0);
            }

            if (teams.Count < 2)
                return;

            // Second check: Do both teams have minimum human players?
            if (humanCounts.Any(c => c < MinHumansPerTeam))
            {
                SkippedInsufficientPlayers++;
                return;
            }

            // Rate the match with pure win/loss (no weights)
            var updatedTeams = _model.Rate(teams, teamRanks, null, null, Tau).ToList();

            // Apply updated ratings back to profiles
            for (int t = 0; t < updatedTeams.Count; t++)
            {
                var playersList = updatedTeams[t].Players.ToList();
                for (int p = 0; p < playersList.Count; p++)
                {
                    var guid = teamGuids[t][p];
                    var player = playersList[p];
                    profiles[guid].UpdateSkillRating(match.GameMode, player.Mu, player.Sigma);
                }
            }
        }
    }
}