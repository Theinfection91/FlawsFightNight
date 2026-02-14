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
                var activePlayers = new Dictionary<int, UTPlayerStats>();

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
                                activePlayers[playerId] = new UTPlayerStats
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

                        case "EG": // End Game: [Time] EG [Reason] [WinnerIDs...]
                            if (parts.Length >= 3)
                            {
                                Console.WriteLine($"Game Ended: Reason - {parts[2]}, Winners - {string.Join(", ", parts.Skip(3))}");
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
                                string eventName = parts[4]; // Fixed: was parts[5], should be parts[4]

                                if (activePlayers.ContainsKey(scorerId))
                                {
                                    if (eventName == "flag_cap_final") activePlayers[scorerId].FlagCaptures++;
                                    if (eventName.Contains("flag_ret")) activePlayers[scorerId].FlagReturns++;
                                    if (eventName == "headshot") activePlayers[scorerId].Headshots++;
                                    Console.WriteLine($"Player {activePlayers[scorerId].LastKnownName} (ID: {scorerId}) scored {eventName} worth {parts[3]} points.");
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
                // Implementation for parsing UT2004StatLog
                return new UT2004StatLog();
            }
        }
    }
}
