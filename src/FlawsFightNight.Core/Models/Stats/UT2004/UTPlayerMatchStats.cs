using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.Stats.UT2004
{
    public class UTPlayerMatchStats
    {
        public string? Guid { get; set; }
        public string? LastKnownName { get; set; }
        public int Team { get; set; } // 0 = Red, 1 = Blue
        public bool IsBot { get; set; }
        public bool IsWinner { get; set; }
        public int Placement { get; set; }

        // Combat Stats
        public int Score { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Suicides { get; set; }
        public int Headshots { get; set; }

        // Kill Streaks & Multikills
        public int BestKillStreak { get; set; }
        public int BestMultiKill { get; set; }

        // Flag Objective Stats - Primary Actions
        public int FlagCaptures { get; set; }          // flag_cap_final
        public int FlagGrabs { get; set; }             // flag_taken (picking up enemy flag from base)
        public int FlagPickups { get; set; }           // flag_pickup (picking up dropped flag)
        public int FlagDrops { get; set; }             // flag_dropped (dropping the flag)
        
        // Flag Objective Stats - Defensive Actions
        public int FlagReturns { get; set; }           // Total flag returns (all types)
        public int FlagReturnsEnemy { get; set; }      // flag_ret_enemy ??
        public int FlagReturnsFriendly { get; set; }   // flag_ret_friendly ??
        public int FlagDenials { get; set; }           // flag_denial ??
        
        // Flag Objective Stats - Support Actions
        public int FlagCaptureAssists { get; set; }    // flag_cap_assist
        public int FlagCaptureFirstTouch { get; set; } // flag_cap_1st_touch
        public int TeamProtectFrags { get; set; }      // team_protect_frag ??
        public int CriticalFrags { get; set; }         // critical_frag ??

        // Weapon Tracking (Optional: Dictionary<WeaponName, Count>)
        public Dictionary<string, int> WeaponKills { get; set; } = new Dictionary<string, int>();

        public UTPlayerMatchStats() { }
    }
}
