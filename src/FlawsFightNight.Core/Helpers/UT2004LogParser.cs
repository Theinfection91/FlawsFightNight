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
            using (var reader = new StreamReader(fileStream))
            {
                var activePlayers = new Dictionary<int, UTPlayerMatchStats>();
                var teamScores = new Dictionary<int, int>(); // Track team scores
                int? winningTeam = null;

                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var parts = line.Split('\t');
                    if (parts.Length < 2) continue;

                    string type = parts[1];
                    switch (type)
                    {
                        case "C": // Connection: 0.89 C [ID] [GUID] [Name]
                            if (parts.Length >= 5)
                            {
                                int playerId = int.Parse(parts[2]);
                                activePlayers[playerId] = new UTPlayerMatchStats
                                {
                                    Guid = parts[3],
                                    LastKnownName = parts[4]
                                };
                                Console.WriteLine($"Player Connected: {activePlayers[playerId].LastKnownName} (ID: {parts[2]}, GUID: {parts[3]})");
                            }
                            break;

                        case "G": // Game Event
                            if (parts.Length >= 4)
                            {
                                if (parts[2] == "TeamChange" && parts.Length >= 5)
                                {
                                    int playerId = int.Parse(parts[3]);
                                    if (activePlayers.ContainsKey(playerId))
                                    {
                                        activePlayers[playerId].Team = int.Parse(parts[4]);
                                        Console.WriteLine($"Player {activePlayers[playerId].LastKnownName} (ID: {parts[3]}) changed to Team {parts[4]}");
                                    }
                                }
                                else if (parts[2] == "NameChange" && parts.Length >= 5)
                                {
                                    int playerId = int.Parse(parts[3]);
                                    if (activePlayers.ContainsKey(playerId))
                                    {
                                        activePlayers[playerId].LastKnownName = parts[4];
                                        Console.WriteLine($"Player ID: {parts[3]} changed name to {parts[4]}");
                                    }
                                }
                            }
                            break;

                        case "T": // Team Score: [Time] T [TeamID] [PointValue] [EventName]
                            if (parts.Length >= 5)
                            {
                                int teamId = int.Parse(parts[2]);
                                double pointValue = double.Parse(parts[3]);
                                
                                if (!teamScores.ContainsKey(teamId))
                                    teamScores[teamId] = 0;
                                
                                teamScores[teamId] += (int)Math.Round(pointValue);
                                Console.WriteLine($"Team {teamId} scored {pointValue} points. Total Team Score: {teamScores[teamId]}");
                            }
                            break;

                        case "EG": // End Game: [Time] EG [Reason] [WinnerIDs...]
                            if (parts.Length >= 3)
                            {
                                // Determine winning team from team scores
                                if (teamScores.Count > 0)
                                {
                                    winningTeam = teamScores.OrderByDescending(kvp => kvp.Value).First().Key;
                                }
                                
                                Console.WriteLine($"Game Ended: Reason - {parts[2]}, Winning Team: {winningTeam}");
                            }
                            break;

                        case "K": // Kill Line: [Time] K [KillerID] [DamType] [VictimID] [Weapon]
                            if (parts.Length >= 6)
                            {
                                int killerId = int.Parse(parts[2]);
                                int victimId = int.Parse(parts[4]);
                                string weapon = parts[5];

                                if (killerId == victimId)
                                {
                                    if (activePlayers.ContainsKey(killerId))
                                    {
                                        activePlayers[killerId].Suicides++;
                                        Console.WriteLine($"Player {activePlayers[killerId].LastKnownName} (ID: {killerId}) committed suicide.");
                                    }
                                }
                                else
                                {
                                    if (activePlayers.ContainsKey(killerId))
                                    {
                                        activePlayers[killerId].Kills++;
                                        // Track weapon usage
                                        if (!activePlayers[killerId].WeaponKills.ContainsKey(weapon))
                                            activePlayers[killerId].WeaponKills[weapon] = 0;
                                        activePlayers[killerId].WeaponKills[weapon]++;
                                        Console.WriteLine($"Player {activePlayers[killerId].LastKnownName} (ID: {killerId}) killed Player {activePlayers[victimId].LastKnownName} (ID: {victimId}) with {weapon}.");
                                    }
                                    if (activePlayers.ContainsKey(victimId))
                                    {
                                        activePlayers[victimId].Deaths++;
                                        Console.WriteLine($"Player {activePlayers[victimId].LastKnownName} (ID: {victimId}) was killed by Player {activePlayers[killerId].LastKnownName} (ID: {killerId}) with {weapon}.");
                                    }
                                }
                            }
                            break;

                        case "S": // Score Line: [Time] S [ID] [PointValue] [EventName]
                            if (parts.Length >= 5)
                            {
                                int scorerId = int.Parse(parts[2]);
                                double pointValue = double.Parse(parts[3]);
                                string eventName = parts[4];

                                if (activePlayers.ContainsKey(scorerId))
                                {
                                    // Add to total score
                                    activePlayers[scorerId].Score += (int)Math.Round(pointValue);

                                    // Track specific events
                                    if (eventName == "flag_cap_final") activePlayers[scorerId].FlagCaptures++;
                                    if (eventName.Contains("flag_ret")) activePlayers[scorerId].FlagReturns++;
                                    if (eventName == "headshot") activePlayers[scorerId].Headshots++;
                                    
                                    Console.WriteLine($"Player {activePlayers[scorerId].LastKnownName} (ID: {scorerId}) scored {eventName} worth {pointValue} points. Total Score: {activePlayers[scorerId].Score}");
                                }
                            }
                            break;

                        case "P": // Special Events: [Time] P [ID] [Event]
                            if (parts.Length >= 4)
                            {
                                int pId = int.Parse(parts[2]);
                                string specialEvent = parts[3];
                                if (activePlayers.ContainsKey(pId))
                                {
                                    Console.WriteLine($"Player {activePlayers[pId].LastKnownName} (ID: {pId}) achieved {specialEvent}.");
                                }
                            }
                            break;
                    }
                }

                // Mark winners based on winning team
                if (winningTeam.HasValue)
                {
                    foreach (var player in activePlayers.Values)
                    {
                        if (player.Team == winningTeam.Value)
                        {
                            player.IsWinner = true;
                        }
                    }
                }

                // Calculate placement by score (descending order)
                var rankedPlayers = activePlayers.Values
                    .OrderByDescending(p => p.Score)
                    .ThenByDescending(p => p.Kills)
                    .ThenBy(p => p.Deaths)
                    .ToList();

                int placement = 1;
                for (int i = 0; i < rankedPlayers.Count; i++)
                {
                    rankedPlayers[i].Placement = placement;
                    
                    // If next player has different score, increment placement
                    if (i + 1 < rankedPlayers.Count && rankedPlayers[i].Score != rankedPlayers[i + 1].Score)
                    {
                        placement = i + 2;
                    }
                }

                Console.WriteLine($"Count of active players before model: {activePlayers.Count}");
                UT2004StatLog statLog = new UT2004StatLog();
                foreach (var value in activePlayers.Values)
                {
                    statLog.Players.Add(new List<UTPlayerMatchStats> { value });
                }
                
                foreach (var player in statLog.Players.SelectMany(p => p))
                {
                    Console.WriteLine($"Player: {player.LastKnownName}, Team: {player.Team}, IsWinner: {player.IsWinner}, Placement: {player.Placement}, Score: {player.Score}, Kills: {player.Kills}, Deaths: {player.Deaths}, Flag Captures: {player.FlagCaptures}");
                }

                // Implementation for parsing UT2004StatLog
                return statLog;
            }
        }
    }
}
