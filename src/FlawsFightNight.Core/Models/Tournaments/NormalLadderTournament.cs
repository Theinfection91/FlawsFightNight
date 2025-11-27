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
    public class NormalLadderTournament : TournamentBase, INormalLadderRankSystem
    {
        public override TournamentType Type { get; protected set; } = TournamentType.NormalLadder;

        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
        public override MatchLogBase MatchLog { get; protected set; }

        [JsonConstructor]
        protected NormalLadderTournament() : base() { }

        public NormalLadderTournament(string id, string name, int teamSize) : base(id, name, teamSize)
        {
            MatchLog ??= new NormalLadderMatchLog();
            Console.WriteLine($"Normal Ladder Constructor Called");
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
            // A ladder tournament can be ended at any time
            return true;
        }

        public override void End()
        {
            IsRunning = false;
            // TODO Add Ladder specific end logic here
        }

        public override string GetFormattedType() => "Normal Ladder";

        public override bool CanDelete() => !IsRunning;

        public override bool CanAcceptNewTeams()
        {
            return true;
        }

        public void ReassignRanks()
        {
            if (Teams == null || Teams.Count == 0)
            {
                return;
            }

            Teams.Sort((a, b) => a.Rank.CompareTo(b.Rank));

            // Reassign ranks sequentially from 1 to N
            for (int i = 0; i < Teams.Count; i++)
            {
                Teams[i].Rank = i + 1;
            }
        }
    }
}
