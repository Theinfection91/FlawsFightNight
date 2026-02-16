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
        /// Calculates a CTF contribution weight for a player (0.0 to 1.0).
        /// Emphasizes objective play over raw kills.
        /// </summary>
        private static double CalculateCTFWeight(UTPlayerMatchStats player, List<UTPlayerMatchStats> team)
        {
            // --- Contribution Categories ---

            // 1. Objective (flag work) - 50% of weight
            //    Captures are king, grabs/pickups show intent, assists show teamwork
            double objectiveScore =
                (player.FlagCaptures * 10.0) +    // Completing the objective
                (player.FlagCaptureAssists * 6.0) + // Directly helping a cap
                (player.FlagGrabs * 2.0) +         // Initiating a run
                (player.FlagPickups * 1.5) +        // Continuing a run
                (player.FlagCaptureFirstTouch * 3.0); // Starting the play

            // 2. Defense (protecting your flag) - 25% of weight
            //    Returns and denials directly prevent enemy scoring
            double defenseScore =
                (player.FlagReturns * 3.0) +       // Any flag return
                (player.FlagDenials * 5.0) +        // Stopping a cap attempt
                (player.TeamProtectFrags * 2.0);    // Killing near flag carrier

            // 3. Combat impact - 25% of weight
            //    Kills matter but in context of the match, not raw count
            //    Critical frags (killing flag carrier, etc.) weighted higher
            double combatScore =
                (player.CriticalFrags * 4.0) +     // High-value kills
                (player.Kills * 1.0) +              // General fragging
                (player.Score * 0.1);               // Game's own scoring

            // Combine with category weights
            double rawWeight = (objectiveScore * 0.50) + (defenseScore * 0.25) + (combatScore * 0.25);

            // Normalize against team average to get relative contribution
            double teamTotal = team
                .Where(p => !p.IsBot)
                .Sum(p => CalculateRawCTFScore(p));

            if (teamTotal <= 0)
                return 1.0; // Fallback: equal weight

            double teamSize = team.Count(p => !p.IsBot);
            double fairShare = teamTotal / teamSize;

            // Ratio of player's contribution vs average teammate
            // Clamped to [0.2, 1.8] to prevent extreme swings
            double weight = rawWeight / fairShare;
            return Math.Clamp(weight, 0.2, 1.8);
        }

        /// <summary>
        /// Raw CTF score for normalization (same formula, no weighting between categories).
        /// </summary>
        private static double CalculateRawCTFScore(UTPlayerMatchStats player)
        {
            return
                (player.FlagCaptures * 10.0) +
                (player.FlagCaptureAssists * 6.0) +
                (player.FlagGrabs * 2.0) +
                (player.FlagPickups * 1.5) +
                (player.FlagCaptureFirstTouch * 3.0) +
                (player.FlagReturns * 3.0) +
                (player.FlagDenials * 5.0) +
                (player.TeamProtectFrags * 2.0) +
                (player.CriticalFrags * 4.0) +
                (player.Kills * 1.0) +
                (player.Score * 0.1);
        }

        /// <summary>
        /// Updates Mu/Sigma on every non-bot player profile based on a single match result.
        /// Supports N teams with variable player counts.
        /// Weights players by CTF contribution so objective players gain more rating.
        /// </summary>
        public void UpdateRatingsForMatch(UT2004StatLog match, Dictionary<string, UT2004PlayerProfile> profiles)
        {
            // Build teams of IRating objects aligned with player GUIDs
            var teams = new List<ITeam>();
            var teamGuids = new List<List<string>>();
            var teamRanks = new List<double>();
            var teamWeights = new List<IList<double>>();

            foreach (var teamPlayers in match.Players)
            {
                var humanPlayers = teamPlayers.Where(p => !p.IsBot && !string.IsNullOrEmpty(p.Guid)).ToList();
                if (humanPlayers.Count == 0)
                    continue;

                var players = new List<IRating>();
                var guids = new List<string>();
                var weights = new List<double>();

                foreach (var player in humanPlayers)
                {
                    var profile = profiles[player.Guid!];
                    players.Add(new Rating { Mu = profile.Mu, Sigma = profile.Sigma });
                    guids.Add(player.Guid!);
                    weights.Add(CalculateCTFWeight(player, teamPlayers));
                }

                teams.Add(new Team { Players = players });
                teamGuids.Add(guids);
                teamWeights.Add(weights);

                // Determine rank: winners get rank 1, losers get rank 2
                bool isWinningTeam = humanPlayers.Any(p => p.IsWinner);
                teamRanks.Add(isWinningTeam ? 1.0 : 2.0);
            }

            if (teams.Count < 2)
                return;

            // Run OpenSkill rating calculation with weights
            var updatedTeams = _plModel.Rate(teams, teamRanks, null, teamWeights).ToList();

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
        /// </summary>
        public static double GetDisplayRating(UT2004PlayerProfile profile)
        {
            return profile.Rating;
        }

        /// <summary>
        /// Gets a more optimistic display rating (mu - 1*sigma) for general display.
        /// </summary>
        public static double GetOptimisticRating(UT2004PlayerProfile profile)
        {
            return profile.Mu - profile.Sigma;
        }

        /// <summary>
        /// Returns just Mu for a pure skill estimate (ignores uncertainty).
        /// </summary>
        public static double GetSkillEstimate(UT2004PlayerProfile profile)
        {
            return profile.Mu;
        }

        /// <summary>
        /// Returns the predicted win probability for teamA vs teamB.
        /// </summary>
        public double PredictWin(List<UT2004PlayerProfile> teamA, List<UT2004PlayerProfile> teamB)
        {
            // Null checks
            if (teamA == null || teamB == null)
                throw new ArgumentNullException(teamA == null ? nameof(teamA) : nameof(teamB));

            if (teamA.Count == 0 || teamB.Count == 0)
                throw new ArgumentException("Teams cannot be empty");

            // Filter out any null profiles and create ratings
            var playersA = teamA
                .Where(p => p != null)
                .Select(p => (IRating)new Rating { Mu = p.Mu, Sigma = p.Sigma })
                .ToList();

            var playersB = teamB
                .Where(p => p != null)
                .Select(p => (IRating)new Rating { Mu = p.Mu, Sigma = p.Sigma })
                .ToList();

            if (playersA.Count == 0 || playersB.Count == 0)
                throw new ArgumentException("Teams must have at least one valid player profile");

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