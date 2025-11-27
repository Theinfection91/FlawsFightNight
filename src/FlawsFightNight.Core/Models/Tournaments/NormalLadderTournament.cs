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

        public override bool CanAcceptNewTeams()
        {
            return true;
        }

        public override void AdjustRanks()
        {
            ReassignRanks();
        }

        public void ReassignRanks()
        {
            if (Teams == null || Teams.Count == 0)
            {
                Console.WriteLine("ReassignRanks: No teams available. Nothing to do.");
                return;
            }

            // Snapshot before sorting
            try
            {
                var beforeSnapshot = string.Join(", ", Teams.Select((t, idx) => $"[{idx}] {t.Name} (rank={t.Rank})"));
                Console.WriteLine($"ReassignRanks: Before sort -> {beforeSnapshot}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReassignRanks: Failed to create before-snapshot: {ex.Message}");
            }

            Teams.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            Console.WriteLine("ReassignRanks: Teams sorted by rank.");

            // Snapshot after sorting
            try
            {
                var afterSnapshot = string.Join(", ", Teams.Select((t, idx) => $"[{idx}] {t.Name} (rank={t.Rank})"));
                Console.WriteLine($"ReassignRanks: After sort -> {afterSnapshot}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReassignRanks: Failed to create after-snapshot: {ex.Message}");
            }

            // Reassign ranks sequentially from 1 to N
            for (int i = 0; i < Teams.Count; i++)
            {
                var oldRank = Teams[i].Rank;
                Teams[i].Rank = i + 1;
                Console.WriteLine($"ReassignRanks: Assigned new rank {Teams[i].Rank} to team {Teams[i].Name} (old rank={oldRank})");
            }

            Console.WriteLine("ReassignRanks: Completed rank reassignment.");
        }
    }
}
