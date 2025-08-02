using FlawsFightNight.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models
{
    public abstract class Tournament
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public TournamentType Type { get; set; }
        public int TeamSize { get; set; }
        public List<Team> Teams { get; set; } = [];
        public MatchHistoryList MatchHistory { get; set; } = new();
        public bool IsRunning { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        protected Tournament(string name, string? description = null)
        {
            Name = name;
            Description = description;
        }
    }
}
