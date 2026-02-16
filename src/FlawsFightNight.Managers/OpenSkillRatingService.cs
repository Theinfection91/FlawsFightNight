using FlawsFightNight.Core.Models.Stats.UT2004;
using OpenSkillSharp.Models;
using OpenSkillSharp.Rating;

namespace FlawsFightNight.Managers
{
    /// <summary>
    /// Bridges OpenSkillSharp with the UT2004 player profile system.
    /// Uses the Plackett-Luce model by default for team-based rating.
    /// </summary>
    public class OpenSkillRatingService
    {
        private readonly PlackettLuce _plModel;

        public OpenSkillRatingService()
        {
            _plModel = new PlackettLuce
            {
                Mu = 25.0,
                Sigma = 25.0 / 3.0
            };
        }

        /// <summary>
        /// Updates Mu/Sigma on every non-bot player profile based on a single match result.
        /// Supports N teams with variable player counts.
        /// </summary>
        public void UpdateRatingsForMatch(UT2004StatLog match, Dictionary<string, UT2004PlayerProfile> profiles)
        {
            // Build teams of IRating objects aligned with player GUIDs
            var teams = new List<ITeam>();
            var teamGuids = new List<List<string>>();
            var teamRanks = new List<double>();

            foreach (var teamPlayers in match.Players)
            {
                var humanPlayers = teamPlayers.Where(p => !p.IsBot && !string.IsNullOrEmpty(p.Guid)).ToList();
                if (humanPlayers.Count == 0)
                    continue;

                var players = new List<IRating>();
                var guids = new List<string>();

                foreach (var player in humanPlayers)
                {
                    var profile = profiles[player.Guid!];
                    players.Add(new Rating { Mu = profile.Mu, Sigma = profile.Sigma });
                    guids.Add(player.Guid!);
                }

                teams.Add(new Team { Players = players });
                teamGuids.Add(guids);

                // Determine rank: winners get rank 1, losers get rank 2
                // Lower rank = better placement
                bool isWinningTeam = humanPlayers.Any(p => p.IsWinner);
                teamRanks.Add(isWinningTeam ? 1.0 : 2.0);
            }

            if (teams.Count < 2)
                return; // Need at least 2 teams to rate

            // Run OpenSkill rating calculation
            var updatedTeams = _plModel.Rate(teams, teamRanks).ToList();

            // Apply updated ratings back to profiles
            for (int t = 0; t < updatedTeams.Count; t++)
            {
                var playersList = updatedTeams[t].Players.ToList();
                for (int p = 0; p < playersList.Count; p++)
                {
                    var guid = teamGuids[t][p];
                    var player = playersList[p];
                    profiles[guid].UpdateSkillRating(player.Mu, player.Sigma);
                }
            }
        }

        /// <summary>
        /// Gets the conservative display rating (mu - 3*sigma) for ordering leaderboards.
        /// Use this for strict ranking where uncertainty matters.
        /// </summary>
        public static double GetDisplayRating(UT2004PlayerProfile profile)
        {
            return profile.Rating; // Already defined as Mu - (3 * Sigma)
        }

        /// <summary>
        /// Gets a more optimistic display rating (mu - 1*sigma) for general display.
        /// Better for showing "true skill" once players have 20+ matches.
        /// </summary>
        public static double GetOptimisticRating(UT2004PlayerProfile profile)
        {
            return profile.Mu - profile.Sigma;
        }

        /// <summary>
        /// Returns just Mu for a pure skill estimate (ignores uncertainty).
        /// Use this for experienced players (50+ matches).
        /// </summary>
        public static double GetSkillEstimate(UT2004PlayerProfile profile)
        {
            return profile.Mu;
        }

        /// <summary>
        /// Returns the predicted win probability for teamA vs teamB.
        /// Useful for match previews or embed displays.
        /// </summary>
        public double PredictWin(List<UT2004PlayerProfile> teamA, List<UT2004PlayerProfile> teamB)
        {
            // 
            var playersA = teamA.Select(p => (IRating)new Rating { Mu = p.Mu, Sigma = p.Sigma }).ToList();
            var playersB = teamB.Select(p => (IRating)new Rating { Mu = p.Mu, Sigma = p.Sigma }).ToList();

            var teams = new List<ITeam>
            {
                new Team { Players = playersA },
                new Team { Players = playersB }
            };

            var winProbabilities = _plModel.PredictWin(teams).ToList();
            return winProbabilities[0];
        }
    }
}