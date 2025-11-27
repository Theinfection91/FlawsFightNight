using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models.MatchLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.Tournaments
{
    public abstract class TournamentBase
    {
        [JsonConstructor]
        protected TournamentBase() { }
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public abstract TournamentType Type { get; protected set; }
        public int TeamSize { get; set; }
        public string TeamSizeFormat => $"{TeamSize}v{TeamSize}";
        public List<Team> Teams { get; set; } = [];
        public bool IsRunning { get; set; } = false;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public abstract MatchLogBase MatchLog { get; protected set; }

        public TournamentBase(string id, string name, int teamSize)
        {
            Id = id;
            Name = name;
            TeamSize = teamSize;
        }

        public abstract bool CanStart();
        public abstract void Start();
        public abstract bool CanEnd();
        public abstract void End();
        public abstract string GetFormattedType();
        public abstract bool CanDelete();
        public abstract bool CanAcceptNewTeams();
    }
}
