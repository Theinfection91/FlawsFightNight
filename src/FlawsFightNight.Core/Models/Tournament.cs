using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models.TieBreakers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public class Tournament
    {
        // Basic Info Shared by most Tournament Types
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public TournamentType Type { get; set; }
        // TODO Testing tie breaker rules, will be replaced with a more dynamic system later
        public ITieBreakerRule TieBreakerRule { get; set; } = new TraditionalTieBreaker();
        public int TeamSize { get; set; }
        public string TeamSizeFormat => $"{TeamSize}v{TeamSize}";
        public List<Team> Teams { get; set; } = [];
        public bool IsTeamsLocked { get; set; } = false;
        public bool CanTeamsBeUnlocked { get; set; } = false;
        public bool CanTeamsBeLocked { get; set; } = false;
        
        public bool IsRunning { get; set; } = false;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // Rounds and Ending Tournament Fields
        public int CurrentRound { get; set; } = 0;
        public int? TotalRounds { get; set; } = null;
        public bool IsRoundComplete { get; set; } = false;
        public bool IsRoundLockedIn { get; set; } = false;
        public bool CanEndTournament => CurrentRound >= TotalRounds && IsRoundComplete && IsRoundLockedIn;

        // Match Log to track all matches in the tournament, current and past
        public MatchLog MatchLog { get; set; } = new();


        public Tournament(string name, string? description = null)
        {
            Name = name;
            Description = description;
        }
    }
}
