using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models.Stats.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Helpers
{
    public class UT2004LogParser : ILogParser
    {
        // State tracking for current match (like Ruby's instance variables)
        private Dictionary<int, UTPlayerMatchStats> _activePlayersBySeqNum = new();
        private Dictionary<int, int> _teamScores = new();
        private int? _winningTeam = null;

        public async Task<T?> Parse<T>(Stream fileStream)
        {
            if (typeof(T) == typeof(UT2004StatLog))
            {
                return (T?)(object)await ParseUT2004StatLog(fileStream);
            }

            return default;
        }

        private async Task<UT2004StatLog> ParseUT2004StatLog(Stream fileStream)
        {
            ClearMatchState();
            
            using (var reader = new StreamReader(fileStream))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var parts = line.Split('\t');
                    if (parts.Length < 2) continue;

                    double timestamp = double.Parse(parts[0]);
                    string eventType = parts[1];

                    switch (eventType)
                    {
                        case "NG": // New Game
                            ParseNewGame(parts);
                            break;

                        case "SI": // Server Info
                            ParseServerInfo(parts);
                            break;

                        case "C": // Connection
                            ParseConnection(parts);
                            break;

                        case "PS": // Player String (additional info)
                            ParsePlayerString(parts);
                            break;

                        case "G": // Game Event
                            ParseGameEvent(parts);
                            break;

                        case "T": // Team Score
                            ParseTeamScore(parts);
                            break;

                        case "K": // Kill
                            ParseKill(parts, timestamp);
                            break;

                        case "S": // Score
                            ParseScore(parts);
                            break;

                        case "P": // Special Event (sprees, multikills)
                            ParseSpecialEvent(parts, timestamp);
                            break;

                        case "EG": // End Game
                            ParseEndGame(parts);
                            break;
                    }
                }

                return BuildStatLog();
            }
        }

        private void ClearMatchState()
        {
            _activePlayersBySeqNum.Clear();
            _teamScores.Clear();
            _winningTeam = null;
        }

        private void ParseNewGame(string[] parts)
        {
            if (parts.Length < 6) return;
            // [Time] NG [DateTime] [Unknown] [MapID] [MapName] [Creator] [GameMode] [Params]
            Console.WriteLine($"New Game Started: Map={parts[5]}, Mode={parts[7]}");
        }

        private void ParseServerInfo(string[] parts)
        {
            if (parts.Length < 3) return;
            Console.WriteLine($"Server: {parts[2]}");
        }

        private void ParseConnection(string[] parts)
        {
            if (parts.Length < 5) return;
            
            // [Time] C [SeqNum] [GUID] [Name] [CDKey?]
            int seqNum = int.Parse(parts[2]);
            string guid = parts[3];
            string name = parts[4];
            bool hasKey = parts.Length >= 6 && !string.IsNullOrEmpty(parts[5]);

            var player = new UTPlayerMatchStats
            {
                Guid = guid,
                LastKnownName = name,
                IsBot = !hasKey  // No CD-key = bot
            };

            _activePlayersBySeqNum[seqNum] = player;
            
            Console.WriteLine($"Player Connected: {name} (SeqNum: {seqNum}, GUID: {guid}, Bot: {!hasKey})");
        }

        private void ParsePlayerString(string[] parts)
        {
            if (parts.Length < 6) return;
            
            // [Time] PS [SeqNum] [IP:Port] [NetSpeed] [OtherData]
            int seqNum = int.Parse(parts[2]);
            
            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                // Mark as confirmed human player (not bot)
                player.IsBot = false;
                Console.WriteLine($"Player {player.LastKnownName} (SeqNum: {seqNum}) confirmed as human player");
            }
        }

        private void ParseGameEvent(string[] parts)
        {
            if (parts.Length < 4) return;

            string eventName = parts[2];
            
            switch (eventName)
            {
                case "TeamChange":
                    if (parts.Length >= 5)
                    {
                        int seqNum = int.Parse(parts[3]);
                        if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
                        {
                            player.Team = int.Parse(parts[4]);
                            Console.WriteLine($"{player.LastKnownName} changed to Team {player.Team}");
                        }
                    }
                    break;

                case "NameChange":
                    if (parts.Length >= 5)
                    {
                        int seqNum = int.Parse(parts[3]);
                        if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
                        {
                            string oldName = player.LastKnownName;
                            player.LastKnownName = parts[4];
                            Console.WriteLine($"Player (SeqNum: {seqNum}) changed name: {oldName} → {player.LastKnownName}");
                        }
                    }
                    break;

                case "flag_taken":
                    if (parts.Length >= 5)
                    {
                        int seqNum = int.Parse(parts[3]);
                        if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
                        {
                            player.FlagGrabs++;
                        }
                    }
                    break;

                case "flag_pickup":
                    if (parts.Length >= 5)
                    {
                        int seqNum = int.Parse(parts[3]);
                        if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
                        {
                            player.FlagPickups++;
                        }
                    }
                    break;

                case "flag_dropped":
                    if (parts.Length >= 5)
                    {
                        int seqNum = int.Parse(parts[3]);
                        if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
                        {
                            player.FlagDrops++;
                        }
                    }
                    break;

                case "flag_captured":
                case "flag_returned":
                case "flag_returned_timeout":
                    // These are logged but final counts come from "S" lines
                    break;
            }
        }

        private void ParseTeamScore(string[] parts)
        {
            if (parts.Length < 5) return;
            
            // [Time] T [TeamID] [Points] [Reason]
            int teamId = int.Parse(parts[2]);
            double points = double.Parse(parts[3]);

            if (!_teamScores.ContainsKey(teamId))
                _teamScores[teamId] = 0;

            _teamScores[teamId] += (int)Math.Round(points);
            Console.WriteLine($"Team {teamId} scored {points} points. Total: {_teamScores[teamId]}");
        }

        private void ParseKill(string[] parts, double timestamp)
        {
            if (parts.Length < 6) return;

            // [Time] K [KillerSeqNum] [DamageType] [VictimSeqNum] [Weapon]
            int killerSeqNum = int.Parse(parts[2]);
            int victimSeqNum = int.Parse(parts[4]);
            string weapon = parts[5];

            if (!_activePlayersBySeqNum.TryGetValue(victimSeqNum, out var victim))
                return;

            // Suicide or environment death (killer == -1 or killer == victim)
            if (killerSeqNum == -1 || killerSeqNum == victimSeqNum)
            {
                victim.Suicides++;
                Console.WriteLine($"{victim.LastKnownName} committed suicide");
                return;
            }

            if (!_activePlayersBySeqNum.TryGetValue(killerSeqNum, out var killer))
                return;

            // Normal kill
            killer.Kills++;
            victim.Deaths++;

            // Track weapon usage
            if (!killer.WeaponKills.ContainsKey(weapon))
                killer.WeaponKills[weapon] = 0;
            killer.WeaponKills[weapon]++;

            Console.WriteLine($"{killer.LastKnownName} killed {victim.LastKnownName} with {weapon}");
        }

        private void ParseScore(string[] parts)
        {
            if (parts.Length < 5) return;

            // [Time] S [SeqNum] [Points] [Reason]
            int seqNum = int.Parse(parts[2]);
            double points = double.Parse(parts[3]);
            string reason = parts[4];

            if (!_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
                return;

            // Increment score (like Ruby's increment!)
            player.Score += (int)Math.Round(points);

            // Track specific events
            switch (reason)
            {
                case "flag_cap_final":
                    player.FlagCaptures++;
                    break;
                case "flag_cap_assist":
                    player.FlagCaptureAssists++;
                    break;
                case "flag_cap_1st_touch":
                    player.FlagCaptureFirstTouch++;
                    break;
                case "flag_ret_enemy":
                    player.FlagReturnsEnemy++;
                    player.FlagReturns++;
                    break;
                case "flag_ret_friendly":
                    player.FlagReturnsFriendly++;
                    player.FlagReturns++;
                    break;
                case "flag_denial":
                    player.FlagDenials++;
                    break;
                case "team_protect_frag":
                    player.TeamProtectFrags++;
                    break;
                case "critical_frag":
                    player.CriticalFrags++;
                    break;
                case "headshot":
                    player.Headshots++;
                    break;
            }

            Console.WriteLine($"{player.LastKnownName} scored {points} for {reason}. Total: {player.Score}");
        }

        private void ParseSpecialEvent(string[] parts, double timestamp)
        {
            if (parts.Length < 4) return;

            // [Time] P [SeqNum] [EventType]
            int seqNum = int.Parse(parts[2]);
            string eventType = parts[3];

            if (!_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
                return;

            // Track kill streaks and multikills
            if (eventType.StartsWith("spree_"))
            {
                int streakLevel = int.Parse(eventType.Replace("spree_", ""));
                player.BestKillStreak = Math.Max(player.BestKillStreak, streakLevel);
                Console.WriteLine($"{player.LastKnownName} achieved {eventType}!");
            }
            else if (eventType.StartsWith("multikill_"))
            {
                int multiLevel = int.Parse(eventType.Replace("multikill_", ""));
                player.BestMultiKill = Math.Max(player.BestMultiKill, multiLevel);
                Console.WriteLine($"{player.LastKnownName} achieved {eventType}!");
            }
            else if (eventType == "first_blood")
            {
                Console.WriteLine($"{player.LastKnownName} drew first blood!");
            }
        }

        private void ParseEndGame(string[] parts)
        {
            if (parts.Length < 3) return;

            string reason = parts[2];

            // Determine winning team
            if (_teamScores.Count > 0)
            {
                _winningTeam = _teamScores.OrderByDescending(kvp => kvp.Value).First().Key;
            }

            Console.WriteLine($"Game Ended: {reason}, Winning Team: {_winningTeam}");
        }

        private UT2004StatLog BuildStatLog()
        {
            // Mark winners
            if (_winningTeam.HasValue)
            {
                foreach (var player in _activePlayersBySeqNum.Values)
                {
                    if (player.Team == _winningTeam.Value)
                    {
                        player.IsWinner = true;
                    }
                }
            }

            // Calculate placement PER TEAM (not overall)
            var playersByTeam = _activePlayersBySeqNum.Values
                .GroupBy(p => p.Team)
                .ToList();

            foreach (var teamGroup in playersByTeam)
            {
                // Rank players within their team
                var rankedPlayers = teamGroup
                    .OrderByDescending(p => p.Score)
                    .ThenByDescending(p => p.Kills)
                    .ThenBy(p => p.Deaths)
                    .ToList();

                int placement = 1;
                for (int i = 0; i < rankedPlayers.Count; i++)
                {
                    rankedPlayers[i].Placement = placement;

                    // If next player has different score, increment placement
                    if (i + 1 < rankedPlayers.Count && 
                        rankedPlayers[i].Score != rankedPlayers[i + 1].Score)
                    {
                        placement = i + 2;
                    }
                }
            }

            // Build the log
            var statLog = new UT2004StatLog();
            foreach (var player in _activePlayersBySeqNum.Values)
            {
                statLog.Players.Add(new List<UTPlayerMatchStats> { player });
                
                Console.WriteLine($"Final Stats - {player.LastKnownName}: " +
                    $"Team={player.Team}, Winner={player.IsWinner}, " +
                    $"Placement={player.Placement} (on team), Score={player.Score}, " +
                    $"K/D={player.Kills}/{player.Deaths}, Caps={player.FlagCaptures}");
            }

            return statLog;
        }
    }
}
