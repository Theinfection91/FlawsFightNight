using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Helpers.UT2004
{
    public class UT2004LogParser : ILogParser
    {
        // Debug logging configuration - Toggle independently
        private const bool _simpleDebugLogging = false;
        private const bool _expandedDebugLogging = false;

        // State tracking for current match
        private Dictionary<int, UTPlayerMatchStats> _activePlayersBySeqNum = new();
        private Dictionary<string, UTPlayerMatchStats> _activePlayersByGuid = new(); // Track by GUID for reconnects
        private Dictionary<int, int> _teamScores = new();
        private int? _winningTeam = null;
        private bool _gameStarted = false;
        private double _gameStartTime = 0;
        private DateTime _matchStartTime = DateTime.MinValue;
        private UT2004GameMode _currentGameMode = default;

        // Track last seen timestamp (seconds) for end-of-file flush
        private double _lastEventTimestamp = 0.0;

        // TAM-specific tracking
        private int _currentRoundNumber = 0;
        private int? _lastKillerSeqNum = null;         // Track who got the last kill before round end
        private Dictionary<int, int> _roundWinsByTeam = new(); // Track rounds won per team

        // iBR tracking - who last carried/picked the ball (bomb)
        private int? _lastBallCarrierSeqNum = null;

        // Kill matrix: killerGuid -> (victimGuid -> count)
        private Dictionary<string, Dictionary<string, int>> _killMatch = new();

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

                    double timestamp;
                    if (!double.TryParse(parts[0], out timestamp))
                        continue;

                    _lastEventTimestamp = timestamp; // update last seen timestamp
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
                            ParseConnection(parts, timestamp);
                            break;

                        case "D": // Disconnect
                            ParseDisconnect(parts, timestamp);
                            break;

                        case "PS": // Player String (additional info)
                            ParsePlayerString(parts, timestamp);
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
                            ParseEndGame(parts, timestamp);
                            break;
                    }
                }

                return BuildStatLog();
            }
        }

        private void ClearMatchState()
        {
            _activePlayersBySeqNum.Clear();
            _activePlayersByGuid.Clear();
            _teamScores.Clear();
            _winningTeam = null;
            _gameStarted = false;
            _gameStartTime = 0;
            _matchStartTime = DateTime.MinValue;
            _currentGameMode = default;
            _currentRoundNumber = 0;
            _lastKillerSeqNum = null;
            _roundWinsByTeam.Clear();
            _lastBallCarrierSeqNum = null;
            _lastEventTimestamp = 0.0;
            _killMatch.Clear();
        }

        private void ParseNewGame(string[] parts)
        {
            if (parts.Length < 6) return;

            // [Time] NG [DateTime] [Unknown] [MapID] [MapName] [Creator] [GameMode] [Params]
            if (parts.Length >= 8 && !string.IsNullOrEmpty(parts[7]))
            {
                string gameMode = parts[7];
                if (gameMode.Contains("CTF", StringComparison.OrdinalIgnoreCase))
                {
                    _currentGameMode = UT2004GameMode.iCTF;
                }
                else if (gameMode.Contains("ReTAM", StringComparison.OrdinalIgnoreCase) || gameMode.Contains("TAM", StringComparison.OrdinalIgnoreCase))
                {
                    _currentGameMode = UT2004GameMode.TAM;
                }
                else if (gameMode.Contains("BombingRun", StringComparison.OrdinalIgnoreCase) || gameMode.Contains("xBombingRun", StringComparison.OrdinalIgnoreCase))
                {
                    _currentGameMode = UT2004GameMode.iBR;
                }
                else
                {
                    _currentGameMode = UT2004GameMode.Unknown;
                    if (_expandedDebugLogging)
                    {
                        Console.WriteLine($"Unknown Game Mode: {gameMode}");
                    }
                }
            }

            // Parse timestamp (format: YYYY-M-D H:mm:ss)
            if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
            {
                try
                {
                    _matchStartTime = DateTime.Parse(parts[2]);
                    if (_expandedDebugLogging)
                        Console.WriteLine($"Match Start Time: {_matchStartTime}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to parse match timestamp '{parts[2]}': {ex.Message}");
                    _matchStartTime = DateTime.UtcNow; // Fallback to current time
                }
            }

            if (_expandedDebugLogging)
                Console.WriteLine($"New Game Started: Map={parts[5]}, Mode={parts[7]}");
        }

        private void ParseServerInfo(string[] parts)
        {
            if (parts.Length < 3) return;
            if (_expandedDebugLogging)
                Console.WriteLine($"Server: {parts[2]}");
        }

        private void ParseStartGame(string[] parts, double timestamp)
        {
            // [Time] SG
            _gameStarted = true;
            _gameStartTime = timestamp;
            if (_expandedDebugLogging)
                Console.WriteLine($"Game Started at {timestamp}");
        }

        private void ParseConnection(string[] parts, double timestamp)
        {
            if (parts.Length < 5) return;

            // [Time] C [SeqNum] [TempGUID] [Name] [CDKey?]
            if (!int.TryParse(parts[2], out int seqNum))
                return;

            bool hasKey = parts.Length >= 6 && !string.IsNullOrEmpty(parts[5]);

            // Create player with temporary GUID - will be updated when PS line is parsed
            var player = new UTPlayerMatchStats
            {
                IsBot = !hasKey
            };

            // Start active time tracking at this timestamp
            player.LastActiveTimestamp = timestamp;
            player.TotalTimeSeconds = 0;

            _activePlayersBySeqNum[seqNum] = player;

            if (_expandedDebugLogging)
                Console.WriteLine($"Player Connected: (SeqNum: {seqNum}, Bot: {!hasKey})");
        }

        private void ParsePlayerString(string[] parts, double timestamp)
        {
            if (parts.Length < 6) return;

            if (!int.TryParse(parts[2], out int seqNum))
                return;

            string playerGuid = parts[5];

            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                // Check if this player already exists by GUID (reconnect scenario)
                if (_activePlayersByGuid.TryGetValue(playerGuid, out var existingPlayer))
                {
                    // Player reconnected - remap to existing player stats
                    _activePlayersBySeqNum[seqNum] = existingPlayer;
                    existingPlayer.LastKnownName = player.LastKnownName; // Update name

                    // Start a new active period for the existing profile
                    existingPlayer.LastActiveTimestamp = timestamp;

                    if (_expandedDebugLogging)
                        Console.WriteLine($"Player Reconnected: {existingPlayer.LastKnownName} (SeqNum: {seqNum}, GUID: {playerGuid})");
                }
                else
                {
                    // First time seeing this GUID - update player and add to GUID dictionary
                    player.Guid = playerGuid;
                    player.IsBot = false; // PS line confirms human player
                    // Ensure LastActiveTimestamp is set (connection may have happened earlier)
                    if (player.LastActiveTimestamp < 0)
                        player.LastActiveTimestamp = timestamp;
                    _activePlayersByGuid[playerGuid] = player;

                    if (_expandedDebugLogging)
                        Console.WriteLine($"Player {player.LastKnownName} (SeqNum: {seqNum}) confirmed with GUID: {playerGuid}");
                }
            }
        }

        private void ParseDisconnect(string[] parts, double timestamp)
        {
            if (parts.Length < 3) return;
            if (!int.TryParse(parts[2], out int seqNum))
                return;

            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                // Parse weapon stats if present (happens at disconnect in some logs)
                if (parts.Length >= 7)
                {
                    ParsePlayerAccuracyStats(seqNum, parts);
                }

                if (_expandedDebugLogging)
                    Console.WriteLine($"Player Disconnected: {player.LastKnownName} (SeqNum: {seqNum}) at {timestamp}");

                // Add active time for this connected period
                if (player.LastActiveTimestamp >= 0)
                {
                    double delta = timestamp - player.LastActiveTimestamp;
                    if (delta > 0)
                        player.TotalTimeSeconds += (int)Math.Round(delta);
                    player.LastActiveTimestamp = -1.0;
                }

                // Remove from sequence number mapping but keep in GUID mapping
                _activePlayersBySeqNum.Remove(seqNum);

                // If the disconnected player was last ball carrier, clear it
                if (_lastBallCarrierSeqNum == seqNum)
                    _lastBallCarrierSeqNum = null;
            }
        }

        private void ParsePlayerPing(string[] parts)
        {
            if (parts.Length < 4) return;

            if (!int.TryParse(parts[2], out int seqNum))
                return;

            int ping = int.Parse(parts[3]);

            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                if (_expandedDebugLogging)
                    Console.WriteLine($"Player {player.LastKnownName} ping: {ping}ms");
            }
        }

        private void ParsePlayerAccuracy(string[] parts)
        {
            if (parts.Length < 6) return;

            if (!int.TryParse(parts[2], out int seqNum))
                return;

            ParsePlayerAccuracyStats(seqNum, parts);
        }

        private void ParsePlayerAccuracyStats(int seqNum, string[] parts)
        {
            if (parts.Length < 6) return;

            string weapon = parts[3];
            if (!int.TryParse(parts[4], out int shotsFired)) shotsFired = 0;
            if (!int.TryParse(parts[5], out int hits)) hits = 0;
            int damage = parts.Length >= 7 && int.TryParse(parts[6], out int d) ? d : 0;

            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                player.WeaponStatistics[weapon] = new WeaponStats
                {
                    WeaponName = weapon,
                    ShotsFired = shotsFired,
                    Hits = hits,
                    DamageDealt = damage
                };

                if (_expandedDebugLogging)
                {
                    double accuracy = shotsFired > 0 ? (double)hits / shotsFired * 100.0 : 0.0;
                    Console.WriteLine($"Player {player.LastKnownName} {weapon}: {hits}/{shotsFired} ({accuracy:F1}%) = {damage} damage");
                }
            }
        }

        private void ParseBotInfo(string[] parts)
        {
            if (parts.Length < 4) return;
            if (!int.TryParse(parts[2], out int seqNum))
                return;

            string botSkill = parts[3];

            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                player.IsBot = true;
                if (_expandedDebugLogging)
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
                    if (parts.Length >= 5 && int.TryParse(parts[3], out int seqTc))
                    {
                        if (_activePlayersBySeqNum.TryGetValue(seqTc, out var player))
                        {
                            player.Team = int.Parse(parts[4]);
                            if (_expandedDebugLogging)
                                Console.WriteLine($"{player.LastKnownName} changed to Team {player.Team}");
                        }
                    }
                    break;

                case "NameChange":
                    if (parts.Length >= 5 && int.TryParse(parts[3], out int seqNc))
                    {
                        if (_activePlayersBySeqNum.TryGetValue(seqNc, out var player))
                        {
                            string oldName = player.LastKnownName;
                            player.LastKnownName = parts[4];
                            if (_expandedDebugLogging)
                                Console.WriteLine($"Player (SeqNum: {seqNc}) changed name: {oldName} → {player.LastKnownName}");
                        }
                    }
                    break;

                case "NewRound":
                    if (_currentGameMode == UT2004GameMode.TAM)
                    {
                        HandleTAMNewRound(parts);
                    }
                    break;

                case "flag_taken":
                    if (parts.Length >= 5 && int.TryParse(parts[3], out int ftSeq))
                    {
                        if (_activePlayersBySeqNum.TryGetValue(ftSeq, out var player))
                            player.FlagGrabs++;
                    }
                    break;

                case "flag_pickup":
                    if (parts.Length >= 5 && int.TryParse(parts[3], out int fpSeq))
                    {
                        if (_activePlayersBySeqNum.TryGetValue(fpSeq, out var player))
                            player.FlagPickups++;
                    }
                    break;

                case "flag_dropped":
                    if (parts.Length >= 5 && int.TryParse(parts[3], out int fdSeq))
                    {
                        if (_activePlayersBySeqNum.TryGetValue(fdSeq, out var player))
                            player.FlagDrops++;
                    }
                    break;

                case "flag_captured":
                case "flag_returned":
                case "flag_returned_timeout":
                    break;

                case "bomb_pickup":
                    if (parts.Length >= 4 && int.TryParse(parts[3], out int bpSeq))
                    {
                        if (_activePlayersBySeqNum.TryGetValue(bpSeq, out var player))
                        {
                            player.BombPickups++;
                            _lastBallCarrierSeqNum = bpSeq;
                            if (_expandedDebugLogging)
                                Console.WriteLine($"{player.LastKnownName} picked up the bomb");
                        }
                    }
                    break;

                case "bomb_dropped":
                    if (parts.Length >= 4 && int.TryParse(parts[3], out int bdSeq))
                    {
                        if (_activePlayersBySeqNum.TryGetValue(bdSeq, out var player))
                        {
                            player.BombDrops++;
                            if (_lastBallCarrierSeqNum == bdSeq)
                                _lastBallCarrierSeqNum = null;
                            if (_expandedDebugLogging)
                                Console.WriteLine($"{player.LastKnownName} dropped the bomb");
                        }
                    }
                    break;

                case "bomb_taken":
                    if (parts.Length >= 4 && int.TryParse(parts[3], out int btSeq))
                    {
                        if (_activePlayersBySeqNum.TryGetValue(btSeq, out var player))
                        {
                            player.BombTaken++;
                            _lastBallCarrierSeqNum = btSeq;
                            if (_expandedDebugLogging)
                                Console.WriteLine($"{player.LastKnownName} took the bomb");
                        }
                    }
                    break;

                case "bomb_returned_timeout":
                    if (parts.Length >= 4 && int.TryParse(parts[3], out int brtSeq) && brtSeq >= 0)
                    {
                        if (_activePlayersBySeqNum.TryGetValue(brtSeq, out var player))
                        {
                            player.BombReturnedTimeouts++;
                            if (_expandedDebugLogging)
                                Console.WriteLine($"{player.LastKnownName} had a bomb return timeout credited");
                        }
                    }
                    break;

                case "ball_cap_final":
                case "ball_score_assist":
                case "ball_thrown_final":
                    if (parts.Length >= 4 && int.TryParse(parts[3], out int bSeq))
                    {
                        if (_activePlayersBySeqNum.TryGetValue(bSeq, out var p))
                        {
                            var en = eventName.ToLowerInvariant();
                            if (en.Contains("cap"))
                                p.BallCaptures++;
                            else if (en.Contains("assist"))
                                p.BallScoreAssists++;
                            else if (en.Contains("thrown"))
                                p.BallThrownFinals++;

                            if (_expandedDebugLogging)
                                Console.WriteLine($"{p.LastKnownName} event {eventName} recorded (G line).");
                        }
                    }
                    break;
            }
        }

        private void HandleTAMNewRound(string[] parts)
        {
            if (parts.Length >= 5 && int.TryParse(parts[4], out int roundNum))
            {
                _currentRoundNumber = roundNum;

                if (_lastKillerSeqNum.HasValue && _activePlayersBySeqNum.TryGetValue(_lastKillerSeqNum.Value, out var lastKiller))
                {
                    lastKiller.RoundEndingKills++;
                    if (_expandedDebugLogging)
                        Console.WriteLine($"{lastKiller.LastKnownName} ended round {_currentRoundNumber - 1}!");
                }

                foreach (var player in _activePlayersBySeqNum.Values.Where(p => !p.IsBot))
                {
                    player.RoundsPlayed++;
                }

                _lastKillerSeqNum = null;

                if (_expandedDebugLogging)
                    Console.WriteLine($"TAM Round {roundNum} starting...");
            }
        }

        private void ParseTeamScore(string[] parts)
        {
            if (parts.Length < 5) return;
            if (!int.TryParse(parts[2], out int teamId))
                return;

            if (!double.TryParse(parts[3], out double points))
                points = 0.0;

            string reason = parts[4];

            if (!_teamScores.ContainsKey(teamId))
                _teamScores[teamId] = 0;

            _teamScores[teamId] += (int)Math.Round(points);

            if (reason.Equals("ball_carried", StringComparison.OrdinalIgnoreCase) && parts.Length >= 4)
            {
                if (_lastBallCarrierSeqNum.HasValue && _activePlayersBySeqNum.TryGetValue(_lastBallCarrierSeqNum.Value, out var carrier))
                {
                    if (_expandedDebugLogging)
                        Console.WriteLine($"{carrier.LastKnownName} carried ball for {points} seconds (team-level T event).");
                }
            }

            if (_currentGameMode == UT2004GameMode.TAM && reason == "tdm_frag")
            {
                if (!_roundWinsByTeam.ContainsKey(teamId))
                    _roundWinsByTeam[teamId] = 0;
                _roundWinsByTeam[teamId]++;

                foreach (var player in _activePlayersBySeqNum.Values.Where(p => p.Team == teamId && !p.IsBot))
                {
                    player.RoundsWon++;
                }

                if (_expandedDebugLogging)
                    Console.WriteLine($"Team {teamId} won round {_currentRoundNumber}! Total rounds won: {_roundWinsByTeam[teamId]}");
            }

            if (_expandedDebugLogging)
                Console.WriteLine($"Team {teamId} scored {points} points. Total: {_teamScores[teamId]}");
        }

        private void ParseKill(string[] parts, double timestamp)
        {
            if (parts.Length < 6) return;
            if (!int.TryParse(parts[2], out int killerSeqNum)) return;
            if (!int.TryParse(parts[4], out int victimSeqNum)) return;

            string weapon = parts[5];
            string damageType = parts[3];

            if (!_activePlayersBySeqNum.TryGetValue(victimSeqNum, out var victim))
                return;

            if (killerSeqNum == -1 || killerSeqNum == victimSeqNum)
            {
                victim.Suicides++;
                if (_expandedDebugLogging)
                    Console.WriteLine($"{victim.LastKnownName} committed suicide");
                return;
            }

            if (!_activePlayersBySeqNum.TryGetValue(killerSeqNum, out var killer))
                return;

            killer.Kills++;
            victim.Deaths++;

            if (!killer.WeaponKills.ContainsKey(weapon))
                killer.WeaponKills[weapon] = 0;
            killer.WeaponKills[weapon]++;

            if (damageType.IndexOf("headshot", StringComparison.OrdinalIgnoreCase) >= 0 ||
                damageType.IndexOf("UTComp_SSRHeadshot", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                killer.Headshots++;
            }

            if (_currentGameMode == UT2004GameMode.TAM)
            {
                _lastKillerSeqNum = killerSeqNum;
            }

            // Register into kill matrix (use current GUIDs if available)
            string killerGuid = killer.Guid ?? string.Empty;
            string victimGuid = victim.Guid ?? string.Empty;
            if (!string.IsNullOrEmpty(killerGuid) && !string.IsNullOrEmpty(victimGuid))
            {
                if (!_killMatch.TryGetValue(killerGuid, out var inner))
                {
                    inner = new Dictionary<string, int>();
                    _killMatch[killerGuid] = inner;
                }

                if (!inner.TryGetValue(victimGuid, out var cnt))
                    cnt = 0;
                inner[victimGuid] = cnt + 1;
            }

            if (_expandedDebugLogging)
                Console.WriteLine($"{killer.LastKnownName} killed {victim.LastKnownName} with {weapon} ({damageType})");
        }

        private void ParseScore(string[] parts)
        {
            if (parts.Length < 5) return;
            if (!int.TryParse(parts[2], out int seqNum)) return;
            if (!double.TryParse(parts[3], out double points)) points = 0.0;
            string reason = parts[4];

            if (!_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
                return;

            player.Score += (int)Math.Round(points);

            var reasonLower = reason.ToLowerInvariant();

            // TAM Combat Tracking
            if (reasonLower.Contains("enemydamage"))
            {
                player.TotalDamageDealt += (int)Math.Round(points);
            }
            else if (reasonLower.Contains("friendlydamage"))
            {
                player.FriendlyFireDamage += (int)Math.Round(Math.Abs(points));
            }
            // Flag Capture Events
            else if (reasonLower.Contains("flag_cap"))
            {
                if (reasonLower.Contains("final"))
                    player.FlagCaptures++;
                else if (reasonLower.Contains("assist"))
                    player.FlagCaptureAssists++;
                else if (reasonLower.Contains("1st") || reasonLower.Contains("first") || reasonLower.Contains("1st_touch"))
                    player.FlagCaptureFirstTouch++;
            }
            // Flag Return Events — use exact prefix matching to avoid matching
            // "flag_returned_timeout" which is a team/server event, not a player return.
            else if (reasonLower == "flag_ret_enemy" ||
                     reasonLower == "flag_ret_friendly" ||
                     reasonLower == "flag_ret")
            {
                player.FlagReturns++;
                if (reasonLower.Contains("enemy"))
                    player.FlagReturnsEnemy++;
                else if (reasonLower.Contains("friendly"))
                    player.FlagReturnsFriendly++;
            }
            // Defensive Events
            else if (reasonLower.Contains("flag_denial"))
            {
                player.FlagDenials++;
            }
            else if (reasonLower.Contains("team_protect_frag"))
            {
                player.TeamProtectFrags++;
            }
            else if (reasonLower.Contains("critical_frag"))
            {
                player.CriticalFrags++;
            }
            // Combat Events
            else if (reasonLower.Contains("headshot"))
            {
                player.Headshots++;
            }
            else if (reasonLower.Contains("self_frag"))
            {
                // Suicide already handled in K parsing
            }
            // BombingRun scoring
            else if (reasonLower.Contains("ball_cap_final") || reasonLower.Contains("ball_cap"))
            {
                player.BallCaptures++;
                if (_lastBallCarrierSeqNum == seqNum)
                    _lastBallCarrierSeqNum = null;
            }
            else if (reasonLower.Contains("ball_score_assist") || reasonLower.Contains("ball_score"))
            {
                player.BallScoreAssists++;
            }
            else if (reasonLower.Contains("ball_thrown_final") || reasonLower.Contains("ball_thrown"))
            {
                player.BallThrownFinals++;
            }
            else if (reasonLower.Contains("tdm_frag"))
            {
                // Team frag point (round win in TAM)
            }
            else if (reasonLower.Contains("objectivescore"))
            {
                // TAM objective scoring
            }
            else
            {
                if (_expandedDebugLogging)
                    Console.WriteLine($"{player.LastKnownName} scored {points} for UNKNOWN event: {reason}");
            }

            if (_expandedDebugLogging)
                Console.WriteLine($"{player.LastKnownName} scored {points} for {reason}. Total: {player.Score}");
        }

        private void ParseSpecialEvent(string[] parts, double timestamp)
        {
            if (parts.Length < 4) return;
            if (!int.TryParse(parts[2], out int seqNum)) return;

            string eventType = parts[3];

            if (!_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
                return;

            // Canonical UTStatsDB behavior: count occurrences of spree and multikill levels.
            if (eventType.StartsWith("spree_"))
            {
                if (int.TryParse(eventType.Substring("spree_".Length), out int streakLevel))
                player.BestKillStreak = Math.Max(player.BestKillStreak, streakLevel);

                // Map absolute streak to spree index used by UTStatsDB:
                // 5-9 -> index 0, 10-14 -> index 1, ..., 30+ -> index 5
                if (streakLevel >= 5)
                {
                    int spreeIndex = Math.Min((streakLevel - 5) / 5, 5);
                    player.SpreeCounts[spreeIndex]++;
                }

                if (_expandedDebugLogging)
                    Console.WriteLine($"{player.LastKnownName} achieved {eventType} (spree index recorded).");
            }
            else if (eventType.StartsWith("multikill_"))
            {
                if (int.TryParse(eventType.Substring("multikill_".Length), out int multiLevel))
                {
                    player.BestMultiKill = Math.Max(player.BestMultiKill, multiLevel);

                    // Map multi to index: 2->0, 3->1, ..., 8+ -> 6
                    if (multiLevel >= 2)
                    {
                        int multiIndex = Math.Min(multiLevel - 2, 6);
                        player.MultiCounts[multiIndex]++;
                    }

                    if (_expandedDebugLogging)
                        Console.WriteLine($"{player.LastKnownName} achieved {eventType} (multi index recorded).");
                }
            }
            else if (eventType == "first_blood")
            {
                if (_expandedDebugLogging)
                    Console.WriteLine($"{player.LastKnownName} drew first blood!");
            }
            else if (eventType == "Overkill")
            {
                if (_expandedDebugLogging)
                    Console.WriteLine($"{player.LastKnownName} got Overkill!");
            }
        }

        private void ParseItemPickup(string[] parts)
        {
            if (parts.Length < 4) return;
            if (!int.TryParse(parts[2], out int seqNum)) return;
            string itemName = parts[3];

            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                if (_expandedDebugLogging)
                    Console.WriteLine($"{player.LastKnownName} picked up {itemName}");
            }
        }

        private void ParseChat(string[] parts, double timestamp, bool isTeamChat)
        {
            if (parts.Length < 4) return;
            if (!int.TryParse(parts[2], out int seqNum)) return;
            string message = parts[3];

            if (_activePlayersBySeqNum.TryGetValue(seqNum, out var player))
            {
                if (_expandedDebugLogging)
                {
                    string chatType = isTeamChat ? "[TEAM]" : "[ALL]";
                    Console.WriteLine($"{chatType} {player.LastKnownName}: {message}");
                }
            }
        }

        private void ParseEndGame(string[] parts, double timestamp)
        {
            if (parts.Length < 3) return;

            string reason = parts[2];

            if (_teamScores.Count > 0)
            {
                _winningTeam = _teamScores.OrderByDescending(kvp => kvp.Value).First().Key;
            }

            if (_expandedDebugLogging)
                Console.WriteLine($"Game Ended: {reason}, Winning Team: {_winningTeam}");

            // Flush active time for everyone still marked active using this timestamp
            foreach (var player in _activePlayersByGuid.Values)
            {
                if (player.LastActiveTimestamp >= 0)
                {
                    double delta = timestamp - player.LastActiveTimestamp;
                    if (delta > 0)
                        player.TotalTimeSeconds += (int)Math.Round(delta);
                    player.LastActiveTimestamp = -1.0;
                }
            }
        }

        private bool ValidateMatchEligibility()
        {
            // Before validating, if parser reached EOF without EG we still need to flush active time
            // using the last seen event timestamp.
            if (_lastEventTimestamp > 0)
            {
                foreach (var player in _activePlayersByGuid.Values)
                {
                    if (player.LastActiveTimestamp >= 0)
                    {
                        double delta = _lastEventTimestamp - player.LastActiveTimestamp;
                        if (delta > 0)
                            player.TotalTimeSeconds += (int)Math.Round(delta);
                        player.LastActiveTimestamp = -1.0;
                    }
                }
            }

            // Use GUID dictionary to get unique players (handles reconnects)
            var uniquePlayers = _activePlayersByGuid.Values.ToList();
            var humanPlayers = uniquePlayers.Where(p => !p.IsBot).ToList();
            var botPlayers = uniquePlayers.Where(p => p.IsBot).ToList();

            // Rule 1: No bots allowed
            if (botPlayers.Any())
            {
                if (_simpleDebugLogging || _expandedDebugLogging)
                    Console.WriteLine($"\nMatch INVALID: Contains {botPlayers.Count} bot(s). Bots: {string.Join(", ", botPlayers.Select(b => b.LastKnownName))}");
                return false;
            }

            // Rule 2: Must have at least 2 human players
            if (humanPlayers.Count < 2)
            {
                if (_simpleDebugLogging || _expandedDebugLogging)
                    Console.WriteLine($"\nMatch INVALID: Only {humanPlayers.Count} human player(s). Need at least 2 players for valid stats.");
                return false;
            }

            // Rule 3: Players must be on different teams (at least 2 teams)
            var teamIds = humanPlayers.Select(p => p.Team).Distinct().ToList();
            if (teamIds.Count < 2)
            {
                if (_simpleDebugLogging || _expandedDebugLogging)
                {
                    Console.WriteLine($"\nMatch INVALID: All {humanPlayers.Count} players are on Team {teamIds.FirstOrDefault()}. " +
                        $"Players: {string.Join(", ", humanPlayers.Select(p => p.LastKnownName))}");
                }
                return false;
            }

            // Rule 4: At least 3 kills must be recorded from each team (to prevent matches where players just connect and do nothing)
            foreach ( var teamId in teamIds )
            {
                if (!humanPlayers.Any(p => p.Team == teamId && p.Kills >= 3))
                {
                    if (_simpleDebugLogging || _expandedDebugLogging)
                    {
                        Console.WriteLine($"\nMatch INVALID: Team {teamId} doesn't have at least 3 kills recorded. " +
                            $"Players on this team: {string.Join(", ", humanPlayers.Where(p => p.Team == teamId).Select(p => p.LastKnownName))}");
                    }
                    return false;
                }
            }

            if (_simpleDebugLogging || _expandedDebugLogging)
                Console.WriteLine($"\nMatch VALID: {humanPlayers.Count} human players across {teamIds.Count} teams, 0 bots");

            return true;
        }

        private UT2004StatLog BuildStatLog()
        {
            if (!ValidateMatchEligibility())
            {
                return null; // Return null to indicate invalid match
            }

            if (_winningTeam.HasValue)
            {
                foreach (var player in _activePlayersByGuid.Values)
                {
                    if (player.Team == _winningTeam.Value)
                    {
                        player.IsWinner = true;
                    }
                }
            }

            // Calculate placement PER TEAM (using unique players from GUID dict)
            var playersByTeam = _activePlayersByGuid.Values
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

            var statLog = new UT2004StatLog();
            foreach (var teamGroup in playersByTeam.OrderBy(g => g.Key))
            {
                statLog.Players.Add(teamGroup.ToList());
            }

            // Expose the collected kill matrix
            statLog.KillMatch = _killMatch;

            if (_simpleDebugLogging)
            {
                Console.WriteLine($"Match Completed | Mode: {_currentGameMode} | Players: {_activePlayersByGuid.Count} | Winning Team: {_winningTeam}");
                foreach (var player in statLog.Players.SelectMany(p => p))
                {
                    Console.WriteLine($"  {player.LastKnownName}: Team={player.Team}, Winner={player.IsWinner}, " +
                        $"Placement={player.Placement} (on team), Score={player.Score}, " +
                        $"K/D={player.Kills}/{player.Deaths}, Caps={player.FlagCaptures}" +
                        (_currentGameMode == UT2004GameMode.TAM ? $", Dmg={player.TotalDamageDealt}, Rounds={player.RoundsWon}/{player.RoundsPlayed}" : ""));
                }
                Console.WriteLine("\n");
            }

            statLog.MatchDate = _matchStartTime != DateTime.MinValue ? _matchStartTime : DateTime.UtcNow;
            statLog.GameMode = _currentGameMode;

            return statLog;
        }
    }
}