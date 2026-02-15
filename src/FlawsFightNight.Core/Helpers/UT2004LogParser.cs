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
        // State tracking for current match
        private Dictionary<int, UTPlayerMatchStats> _activePlayersBySeqNum = new();
        private Dictionary<int, int> _teamScores = new();
        private int? _winningTeam = null;
        private bool _gameStarted = false;
        private double _gameStartTime = 0;

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

                        case "SG": // Start Game
                            ParseStartGame(parts, timestamp);
                            break;

                        case "C": // Connection
                            ParseConnection(parts);
                            break;

                        case "D": // Disconnect
                            ParseDisconnect(parts, timestamp);
                            break;

                        case "PS": // Player String (additional info)
                            ParsePlayerString(parts);
                            break;

                        case "PP": // Player Ping
                            ParsePlayerPing(parts);
                            break;

                        case "PA": // Player Accuracy
                            ParsePlayerAccuracy(parts);
                            break;

                        case "BI": // Bot Info
                            ParseBotInfo(parts);
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

                        case "I": // Item Pickup
                            ParseItemPickup(parts);
                            break;

                        case "V": // Chat
                            ParseChat(parts, timestamp, false);
                            break;

                        case "TV": // Team Chat
                        case "VT":
                            ParseChat(parts, timestamp, true);
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
            _gameStarted = false;
            _gameStartTime = 0;
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

        private void ParseStartGame(string[] parts, double timestamp)
        {
            // [Time] SG
            _gameStarted = true;
            _gameStartTime = timestamp;
            Console.WriteLine($"Game Started at {timestamp}");
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
                IsBot = !hasKey
            };

            _activePlayersBySeqNum[seqNum] = player;
            
            Console.WriteLine($"Player Connected: {name} (SeqNum: {seqNum}, GUID: {guid}, Bot: {!hasKey})");
        }

        private void ParseDisconnect(string[] parts, double timestamp)
        {
            if (parts.Length < 3) return;

            // [Time] D [SeqNum]
            int seqNum = int.Parse(parts[2]);
            
            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                Console.WriteLine($"Player Disconnected: {player.LastKnownName} (SeqNum: {seqNum}) at {timestamp}");
            }
        }

        private void ParsePlayerString(string[] parts)
        {
            if (parts.Length < 6) return;
            
            // [Time] PS [SeqNum] [IP:Port] [NetSpeed] [OtherData]
            int seqNum = int.Parse(parts[2]);
            
            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                player.IsBot = false;
                Console.WriteLine($"Player {player.LastKnownName} (SeqNum: {seqNum}) confirmed as human player");
            }
        }

        private void ParsePlayerPing(string[] parts)
        {
            if (parts.Length < 4) return;

            // [Time] PP [SeqNum] [Ping]
            int seqNum = int.Parse(parts[2]);
            int ping = int.Parse(parts[3]);

            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                Console.WriteLine($"Player {player.LastKnownName} ping: {ping}ms");
            }
        }

        private void ParsePlayerAccuracy(string[] parts)
        {
            if (parts.Length < 5) return;

            // [Time] PA [SeqNum] [Weapon] [Accuracy%]
            int seqNum = int.Parse(parts[2]);
            string weapon = parts[3];
            double accuracy = double.Parse(parts[4]);

            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                Console.WriteLine($"Player {player.LastKnownName} {weapon} accuracy: {accuracy}%");
            }
        }

        private void ParseBotInfo(string[] parts)
        {
            if (parts.Length < 4) return;

            // [Time] BI [SeqNum] [BotSkill]
            int seqNum = int.Parse(parts[2]);
            string botSkill = parts[3];

            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                player.IsBot = true;
                Console.WriteLine($"Bot {player.LastKnownName} skill: {botSkill}");
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

            // Suicide or environment death
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

            // Always add score first
            player.Score += (int)Math.Round(points);

            // Track specific stat categories
            switch (reason)
            {
                // Flag Capture Events
                case "flag_cap_final":
                    player.FlagCaptures++;
                    break;
                case "flag_cap_assist":
                    player.FlagCaptureAssists++;
                    break;
                case "flag_cap_1st_touch":
                    player.FlagCaptureFirstTouch++;
                    break;

                // Flag Return Events
                case "flag_ret_enemy":
                    player.FlagReturnsEnemy++;
                    player.FlagReturns++;
                    break;
                case "flag_ret_friendly":
                    player.FlagReturnsFriendly++;
                    player.FlagReturns++;
                    break;

                // Defensive Events
                case "flag_denial":
                    player.FlagDenials++;
                    break;
                case "team_protect_frag":
                    player.TeamProtectFrags++;
                    break;
                case "critical_frag":
                    player.CriticalFrags++;
                    break;

                // Combat Events
                case "headshot":
                    player.Headshots++;
                    break;
                case "frag":
                    // Normal frag, already counted in Kills
                    break;
                case "self_frag":
                    // Negative score from suicide, already counted in Suicides
                    break;

                // Unknown/Other scoring events - just log them
                default:
                    Console.WriteLine($"{player.LastKnownName} scored {points} for UNKNOWN event: {reason}");
                    break;
            }

            Console.WriteLine($"{player.LastKnownName} scored {points} for {reason}. Total: {player.Score}");
        }

        private void ParseSpecialEvent(string[] parts, double timestamp)
        {
            if (parts.Length < 4) return;

            int seqNum = int.Parse(parts[2]);
            string eventType = parts[3];

            if (!_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
                return;

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

        private void ParseItemPickup(string[] parts)
        {
            if (parts.Length < 4) return;

            // [Time] I [SeqNum] [ItemName]
            int seqNum = int.Parse(parts[2]);
            string itemName = parts[3];

            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                Console.WriteLine($"{player.LastKnownName} picked up {itemName}");
            }
        }

        private void ParseChat(string[] parts, double timestamp, bool isTeamChat)
        {
            if (parts.Length < 4) return;

            // [Time] V/TV [SeqNum] [Message]
            int seqNum = int.Parse(parts[2]);
            string message = parts[3];

            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                string chatType = isTeamChat ? "[TEAM]" : "[ALL]";
                Console.WriteLine($"{chatType} {player.LastKnownName}: {message}");
            }
        }

        private void ParseEndGame(string[] parts)
        {
            if (parts.Length < 3) return;

            string reason = parts[2];

            if (_teamScores.Count > 0)
            {
                _winningTeam = _teamScores.OrderByDescending(kvp => kvp.Value).First().Key;
            }

            Console.WriteLine($"Game Ended: {reason}, Winning Team: {_winningTeam}");
        }

        /// <summary>
        /// Validates if the match is eligible for stat tracking.
        /// Returns false if match should be discarded.
        /// </summary>
        private bool ValidateMatchEligibility()
        {
            var allPlayers = _activePlayersBySeqNum.Values.ToList();
            var humanPlayers = allPlayers.Where(p => !p.IsBot).ToList();
            var botPlayers = allPlayers.Where(p => p.IsBot).ToList();

            // Rule 1: No bots allowed
            if (botPlayers.Any())
            {
                Console.WriteLine($"❌ Match INVALID: Contains {botPlayers.Count} bot(s). Bots: {string.Join(", ", botPlayers.Select(b => b.LastKnownName))}");
                return false;
            }

            // Rule 2: Must have at least 2 human players
            if (humanPlayers.Count < 2)
            {
                Console.WriteLine($"❌ Match INVALID: Only {humanPlayers.Count} human player(s). Need at least 2 players for valid stats.");
                return false;
            }

            Console.WriteLine($"✅ Match VALID: {humanPlayers.Count} human players, 0 bots");
            return true;
        }

        private UT2004StatLog BuildStatLog()
        {
            // Validate match eligibility FIRST
            if (!ValidateMatchEligibility())
            {
                Console.WriteLine("⚠️  Match discarded - stats will not be saved or processed.");
                return null; // Return null to indicate invalid match
            }

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

            // Calculate placement PER TEAM
            var playersByTeam = _activePlayersBySeqNum.Values
                .GroupBy(p => p.Team)
                .ToList();

            foreach (var teamGroup in playersByTeam)
            {
                var rankedPlayers = teamGroup
                    .OrderByDescending(p => p.Score)
                    .ThenByDescending(p => p.Kills)
                    .ThenBy(p => p.Deaths)
                    .ToList();

                int placement = 1;
                for (int i = 0; i < rankedPlayers.Count; i++)
                {
                    rankedPlayers[i].Placement = placement;

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