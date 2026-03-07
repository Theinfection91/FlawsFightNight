using FlawsFightNight.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.UT2004
{
    [SafeForSerialization]
    public class MatchEvent
    {
        /// <summary>Seconds elapsed since the game start (SG event). Negative values are pre-game.</summary>
        public double GameTimeSeconds { get; set; }

        /// <summary>
        /// Event category: Kill, Suicide, FirstBlood, Spree, MultiKill, Overkill,
        /// FlagGrab, FlagPickup, FlagDrop, FlagCapture, FlagReturn, FlagReturnTimeout,
        /// BombPickup, BombDrop, BombTaken, BombCapture, RoundStart, RoundWin.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>Name of the player who performed the action.</summary>
        public string? ActorName { get; set; }

        /// <summary>GUID of the player who performed the action.</summary>
        public string? ActorGuid { get; set; }

        /// <summary>Name of the secondary player involved (e.g. victim on a kill).</summary>
        public string? TargetName { get; set; }

        /// <summary>GUID of the secondary player involved.</summary>
        public string? TargetGuid { get; set; }

        /// <summary>Extra context: weapon name, flag team, round number, spree/multi name, etc.</summary>
        public string? Detail { get; set; }
    }
}
