using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models.MatchLogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.Tournaments
{
    public class NormalLadderTournament : Tournament, INormalLadderRankSystem
    {
        public override TournamentType Type { get; protected set; } = TournamentType.NormalLadder;

        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
        public override MatchLog MatchLog { get; protected set; }

        [JsonConstructor]
        protected NormalLadderTournament() : base() { }

        public NormalLadderTournament(string id, string name, int teamSize) : base(id, name, teamSize)
        {
            MatchLog ??= new NormalLadderMatchLog();
        }

        public override bool CanStart()
        {
            // A ladder tournament requires at least 3 teams to function properly
            return Teams.Count >= 3;
        }

        public override void Start()
        {
            IsRunning = true;

            // Reset team stats to zero
            foreach (var team in Teams)
            {
                team.ResetTeamToZero();
            }
        }

        public override bool CanEnd()
        {
            // If the tournament is running, it can be ended at any time
            return IsRunning;
        }

        public override void End()
        {
            IsRunning = false;
            // TODO Add Ladder specific end logic here
        }

        public override string GetFormattedType() => "Normal Ladder";

        public override bool CanDelete() => !IsRunning;

        public override bool CanAcceptNewTeams() => true;

        public override void AdjustRanks()
        {
            ReassignRanks();
            (MatchLog as IChallengeLog)?.RunChallengeRankCorrection(GetAllChallengeTeams());
        }

        public void ReassignRanks()
        {
            // Sort teams by their current rank
            Teams.Sort((a, b) => a.Rank.CompareTo(b.Rank));

            // Reassign ranks sequentially starting from 1
            for (int i = 0; i < Teams.Count; i++)
            {
                Teams[i].Rank = i + 1;
            }
        }

        public List<Team> GetAllChallengeTeams()
        {
            var challengeTeams = new List<Team>();
            foreach (var match in MatchLog.GetAllActiveMatches())
            {
                if (match.Challenge is not null)
                {
                    var challenger = Teams.FirstOrDefault(t => t.Name.Equals(match.Challenge.Challenger, StringComparison.OrdinalIgnoreCase));
                    var challenged = Teams.FirstOrDefault(t => t.Name.Equals(match.Challenge.Challenged, StringComparison.OrdinalIgnoreCase));
                    if (challenger != null && !challengeTeams.Any(t => t.Name.Equals(challenger.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        challengeTeams.Add(challenger);
                    }
                    if (challenged != null && !challengeTeams.Any(t => t.Name.Equals(challenged.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        challengeTeams.Add(challenged);
                    }
                }
            }
            return challengeTeams;
        }

        public bool IsChallengedTeamWithinRanks(Team challenger, Team challenged)
        {
            return (challenger.Rank - challenged.Rank) is >= 1 and <= 2;
        }
    }
}

