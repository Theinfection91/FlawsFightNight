using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.Tournaments
{
    public abstract class TournamentBase
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public TournamentType Type { get; protected set; }
        public int TeamSize { get; set; }
        public string TeamSizeFormat => $"{TeamSize}v{TeamSize}";
        public List<Team> Teams { get; set; } = [];
        public bool IsRunning { get; set; } = false;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public IMatchLog MatchLog { get; protected set; }

        public abstract bool IsReadyToStart();
        public abstract void Start();
        public abstract bool IsReadyToEnd();
        public abstract void End();
        public abstract string GetFormattedType();
    }
}
