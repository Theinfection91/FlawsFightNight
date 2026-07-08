using FlawsFightNight.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Models.UT2004
{
    [SafeForSerialization]
    public class UTPlayerMatchStats
    {
        public string? Guid { get; set; }
        public string? LastKnownName { get; set; }
        public int Team { get; set; } // 0 = Red, 1 = Blue
        public bool IsBot { get; set; }
        public bool IsWinner { get; set; }
        public int Placement { get; set; }
        public int TotalTimeSeconds { get; set; } = 0;
        public double LastActiveTimestamp { get; set; } = -1.0;

        // Combat Stats
        public int Score { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Suicides { get; set; }
        public int Headshots { get; set; }

        // Kill Streaks & Multikills - individual counts
        public int BestKillStreak { get; set; }
        public int BestMultiKill { get; set; }

        // Killing Spree counts (indices match kill count brackets)
        public int KillingSprees { get; set; }      // 5x
        public int Rampages { get; set; }           // 10x
        public int Dominatings { get; set; }        // 15x
        public int Unstoppables { get; set; }       // 20x
        public int Godlikes { get; set; }           // 25x
        public int WickedSicks { get; set; }        // 30x

        // Multi-kill counts (indices match kill count brackets)
        public int DoubleKills { get; set; }        // 2x
        public int MultiKills { get; set; }         // 3x
        public int MegaKills { get; set; }          // 4x
        public int UltraKills { get; set; }         // 5x
        public int MonsterKills { get; set; }       // 6x
        public int LudicrousKills { get; set; }     // 7x
        public int HolyShits { get; set; }          // 8x+

        // Legacy arrays (for backwards compatibility)
        [Obsolete("Use individual Spree/MultiKill count properties instead")]
        public int[] SpreeCounts { get; set; } = new int[6];
        [Obsolete("Use individual Spree/MultiKill count properties instead")]
        public int[] MultiCounts { get; set; } = new int[7];

        // TAM Weapon Achievements - Counters (match-level)
        public int ComboWhoreCount { get; set; } = 0;
        public int BioHazardCount { get; set; } = 0;
        public int HeadHunterCount { get; set; } = 0;
        public int FlakMonkeyCount { get; set; } = 0;
        public int RocketManCount { get; set; } = 0;

        // Weapon-specific streaks (TAM mode - 15+ consecutive kills with specific weapon)
        public int ComboWhoreStreaks { get; set; }      // Shock Combo
        public int BioHazardStreaks { get; set; }       // Bio Rifle
        public int HeadHunterStreaks { get; set; }      // Sniper Rifle / Lightning Gun
        public int FlakMonkeyStreaks { get; set; }      // Flak Cannon
        public int RocketManStreaks { get; set; }       // Rocket Launcher

        // TAM-Specific Stats
        public int TotalDamageDealt { get; set; }        // Sum of all 'S EnemyDamage' events
        public int TotalDamageTaken { get; set; }        // Damage received from enemies
        public int FriendlyFireDamage { get; set; }      // 'S FriendlyDamage' events
        public int RoundEndingKills { get; set; }        // Kills that ended a round (last K before NewRound)
        public int RoundsWon { get; set; }               // Individual rounds player's team won
        public int RoundsPlayed { get; set; }            // Total rounds participated in

        // Weapon Accuracy (from PA lines at match end)
        public Dictionary<string, WeaponStats> WeaponStatistics { get; set; } = new Dictionary<string, WeaponStats>();

        // Legacy weapon tracking (keep for backwards compatibility)
        public Dictionary<string, int> WeaponKills { get; set; } = new Dictionary<string, int>();

        // iCTF Objective Stats
        public int FlagCaptures { get; set; }          // flag_cap_final
        public int FlagGrabs { get; set; }             // flag_taken (picking up enemy flag from base)
        public int FlagPickups { get; set; }           // flag_pickup (picking up dropped flag)
        public int FlagDrops { get; set; }             // flag_dropped (dropping the flag)
        public int FlagReturns { get; set; }           // Total flag returns (all types)
        public int FlagReturnsEnemy { get; set; }      // flag_ret_enemy ?? 
        public int FlagReturnsFriendly { get; set; }   // flag_ret_friendly ?? 
        public int FlagDenials { get; set; }           // flag_denial ??
        public int FlagCaptureAssists { get; set; }    // flag_cap_assist
        public int FlagCaptureFirstTouch { get; set; } // flag_cap_1st_touch
        public int TeamProtectFrags { get; set; }      // team_protect_frag ??
        public int CriticalFrags { get; set; }         // critical_frag ??

        // iBR / BombingRun Stats
        public int BallCaptures { get; set; }          // ball_cap_final
        public int BallScoreAssists { get; set; }      // ball_score_assist
        public int BallThrownFinals { get; set; }      // ball_thrown_final

        public int BombPickups { get; set; }           // bomb_pickup (BombingRun)
        public int BombDrops { get; set; }             // bomb_dropped
        public int BombTaken { get; set; }             // bomb_taken
        public int BombReturnedTimeouts { get; set; }  // bomb_returned_timeout (if attributable)

        public UTPlayerMatchStats() { }

        /// <summary>
        /// Calculates overall weapon accuracy percentage (0-100).
        /// </summary>
        public double GetOverallAccuracy()
        {
            int totalShots = WeaponStatistics.Values.Sum(w => w.ShotsFired);
            int totalHits = WeaponStatistics.Values.Sum(w => w.Hits);

            return totalShots > 0 ? (double)totalHits / totalShots * 100.0 : 0.0;
        }

        /// <summary>
        /// Calculates damage per round for TAM efficiency metric.
        /// </summary>
        public double GetDamagePerRound()
        {
            return RoundsPlayed > 0 ? (double)TotalDamageDealt / RoundsPlayed : 0.0;
        }

        /// <summary>
        /// Calculates K/D ratio with safety check.
        /// </summary>
        public double GetKillDeathRatio()
        {
            return Deaths > 0 ? (double)Kills / Deaths : Kills;
        }

        /// <summary>
        /// Gets total number of all killing spree achievements.
        /// </summary>
        public int GetTotalKillingSprees()
        {
            return KillingSprees + Rampages + Dominatings + Unstoppables + Godlikes + WickedSicks;
        }

        /// <summary>
        /// Gets total number of all multi-kill achievements.
        /// </summary>
        public int GetTotalMultiKills()
        {
            return DoubleKills + MultiKills + MegaKills + UltraKills + MonsterKills + LudicrousKills + HolyShits;
        }

        /// <summary>
        /// Gets total number of weapon-specific streak achievements (TAM).
        /// </summary>
        public int GetTotalWeaponStreaks()
        {
            return ComboWhoreStreaks + BioHazardStreaks + HeadHunterStreaks + FlakMonkeyStreaks + RocketManStreaks;
        }
    }

    /// <summary>
    /// Detailed weapon statistics from PA (Player Accuracy) log lines.
    /// Example: PA 2 NewNet_SniperRifle 41 19 1540
    /// </summary>
    public class WeaponStats
    {
        public string WeaponName { get; set; } = string.Empty;
        public int ShotsFired { get; set; }       // 3rd parameter in PA line
        public int Hits { get; set; }             // 4th parameter in PA line
        public int DamageDealt { get; set; }      // 5th parameter in PA line

        /// <summary>
        /// Calculates accuracy percentage for this weapon (0-100).
        /// </summary>
        public double GetAccuracy()
        {
            return ShotsFired > 0 ? (double)Hits / ShotsFired * 100.0 : 0.0;
        }

        /// <summary>
        /// Average damage per hit (precision metric).
        /// </summary>
        public double GetDamagePerHit()
        {
            return Hits > 0 ? (double)DamageDealt / Hits : 0.0;
        }
    }
}