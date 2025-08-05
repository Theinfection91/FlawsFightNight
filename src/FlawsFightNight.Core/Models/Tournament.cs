using FlawsFightNight.Core.Enums;
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
        public int TeamSize { get; set; }
        public string TeamSizeFormat => $"{TeamSize}v{TeamSize}";
        public List<Team> Teams { get; set; } = [];
        public bool IsTeamsLocked { get; set; } = false;
        public bool CanTeamsBeUnlocked { get; set; } = false;
        public bool CanTeamsBeLocked { get; set; } = false;
        public MatchLog MatchLog { get; set; } = new();
        public bool IsRunning { get; set; } = false;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // Ladder Specific Fields

        // Round Robin Specific Fields
        public int CurrentRound { get; set; } = 0;
        public int? TotalRounds { get; set; } = null;
        public bool IsRoundComplete { get; set; } = false;
        public bool CanAdvanceToNextRound { get; set; } = false;


        public Tournament(string name, string? description = null)
        {
            Name = name;
            Description = description;
        }
    }
}
