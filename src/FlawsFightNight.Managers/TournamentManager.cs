using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class TournamentManager : BaseDataDriven
    {
        public TournamentManager(DataManager dataManager) : base("TournamentManager", dataManager)
        {

        }

        public void LoadTournamentsDatabase()
        {
            _dataManager.LoadTournamentsDatabase();
        }

        public void SaveTournamentsDatabase()
        {
            _dataManager.SaveTournamentsDatabase();
        }

        public void SaveAndReloadTournamentsDatabase()
        {
            SaveTournamentsDatabase();
            LoadTournamentsDatabase();
        }

        public void AddTournament(Tournament tournament)
        {
            _dataManager.AddTournament(tournament);
        }

        public bool IsTournamentNameUnique(string tournamentName)
        {
            List<Tournament> tournaments = _dataManager.TournamentsDatabaseFile.Tournaments;
            foreach (Tournament tournament in tournaments)
            {
                if (tournament.Name.Equals(tournamentName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true; // Team name is unique across all tournaments
        }

        public Tournament CreateTournament(string name, TournamentType tournamentType, int teamSize, string? description = null)
        {
            string? id = GenerateTournamentId();
            return new Tournament(name, description)
            {
                Id = id,
                Type = tournamentType,
                TeamSize = teamSize
            };
        }

        public void DeleteTournament(string tournamentId)
        {
            _dataManager.RemoveTournament(tournamentId);
        }

        public bool CanAcceptNewTeams(Tournament tournament)
        {
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    return true; // Ladder tournaments can always accept new teams
                case TournamentType.RoundRobin:
                    return tournament.IsTeamsLocked;
                case TournamentType.SingleElimination:
                case TournamentType.DoubleElimination:
                    return !tournament.IsRunning; // SE/DE tournaments cannot accept new teams once they start
                default:
                    return false; // Unknown tournament type
            }
        }

        public bool CanTeamsBeLockedResolver(Tournament tournament)
        {
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    return false; // Ladder tournaments do not lock teams

                case TournamentType.RoundRobin:
                    return CanRoundRobinTeamsBeLocked(tournament);

                case TournamentType.SingleElimination:
                case TournamentType.DoubleElimination:
                    return !tournament.IsRunning && tournament.CanTeamsBeLocked; // SE/DE can lock teams if not running

                default:
                    return false; // Unknown tournament type
            }
        }

        public bool CanRoundRobinTeamsBeLocked(Tournament tournament)
        {
            if (tournament.IsRunning) return false; // Cannot lock teams if tournament is running

            if (tournament.Teams.Count < 3) return false; // Need at least 3 teams to lock teams in Round Robin

            return true;
        }

        public void SetCanTeamsBeLocked(Tournament tournament, bool canTeamsBeLocked)
        {
            tournament.CanTeamsBeLocked = canTeamsBeLocked;
        }

        public void LockTeamsInTournament(Tournament tournament)
        {
            tournament.IsTeamsLocked = true;
            tournament.CanTeamsBeLocked = false;

            // Allow teams to be unlocked after locking, until tournament starts
            tournament.CanTeamsBeUnlocked = true;
        }

        public void UnlockTeamsInTournament(Tournament tournament)
        {
            tournament.IsTeamsLocked = false;
            tournament.CanTeamsBeUnlocked = false;

            // Allow teams to be locked again
            tournament.CanTeamsBeLocked = true;
        }

        public void NextRoundResolver(Tournament tournament)
        {
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    // Ladder tournaments do not have rounds
                    break;
                case TournamentType.RoundRobin:
                    AdvanceRoundRobinToNextRound(tournament);
                    break;
                case TournamentType.SingleElimination:
                case TournamentType.DoubleElimination:
                    // SE/DE round advancement logic would go here
                    break;
                default:
                    // Unknown tournament type
                    break;
            }
        }

        private void AdvanceRoundRobinToNextRound(Tournament tournament)
        {
            tournament.CurrentRound++;
            tournament.IsRoundComplete = false;
            tournament.IsRoundLockedIn = false;
        }

        private void EndRoundRobinTournament(Tournament tournament)
        {
            // Logic to handle the end of a round robin tournament
            tournament.IsRunning = false;
        }

        public bool IsTournamentIdInDatabase(string tournamentId, bool isCaseSensitive = false)
        {
            if (isCaseSensitive)
            {
                return _dataManager.TournamentsDatabaseFile.Tournaments
                    .Any(t => t.Id.Equals(tournamentId));
            }
            else
            {
                return _dataManager.TournamentsDatabaseFile.Tournaments
                    .Any(t => t.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase));
            }
        }

        public List<Tournament> GetAllTournaments()
        {
            return _dataManager.TournamentsDatabaseFile.Tournaments;
        }

        public Tournament? GetTournamentById(string tournamentId)
        {
            return _dataManager.TournamentsDatabaseFile.Tournaments
                .FirstOrDefault(t => t.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase));
        }

        public Tournament GetTournamentFromTeamName(string teamName)
        {
            foreach (Tournament tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                if (tournament.Teams.Any(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase)))
                {
                    return tournament;
                }
            }
            return null;
        }

        public Tournament GetTournamentFromMatchId(string matchId)
        {
            foreach (Tournament tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                foreach (var round in tournament.MatchLog.PostMatchesByRound.Values)
                {
                    foreach (var match in round)
                    {
                        if (match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                        {
                            return tournament;
                        }
                    }
                }
            }
            return null;
        }

        public string? GenerateTournamentId()
        {
            bool isUnique = false;
            string uniqueId;

            while (!isUnique)
            {
                Random random = new();
                int randomInt = random.Next(100, 1000);
                uniqueId = $"T{randomInt}";

                // Check if the generated ID is unique
                if (!IsTournamentIdInDatabase(uniqueId))
                {
                    isUnique = true;
                    return uniqueId;
                }
            }
            return null;
        }

        public void AddTeamToTournament(Team team, string tournamentId)
        {
            Tournament? tournament = GetTournamentById(tournamentId);
            if (tournament != null)
            {
                tournament.Teams.Add(team);
            }
        }

        //public void UpdateTournamentStandings(Tournament tournament)
        //{

        //    Console.WriteLine($"Update Tournament - RR Test Count: {tournament.RoundRobinStandings.Entries.Count}");

        //    Console.WriteLine($"[DEBUG] Building standings for tournament: {tournament.Name}, Teams: {tournament.Teams.Count}");

        //    var standings = new RoundRobinStandings();

        //    // Build entries
        //    foreach (var team in tournament.Teams)
        //    {
        //        var entry = new StandingsEntry(team);
        //        standings.Entries.Add(entry);
        //        Console.WriteLine($"[DEBUG] Added team entry: {entry.TeamName}, Wins: {entry.Wins}, Losses: {entry.Losses}, Score: {entry.TotalScore}");
        //    }

        //    // Initial sort (by wins/score/etc.)
        //    standings.SortStandings();

        //    Console.WriteLine("[DEBUG] After initial sort:");
        //    foreach (var entry in standings.Entries)
        //    {
        //        Console.WriteLine($"   {entry.TeamName}: {entry.Wins}-{entry.Losses}, {entry.TotalScore} pts");
        //    }

        //    // Group by full record (Wins + Losses)
        //    var groupedByRecord = standings.Entries
        //        //.Where(e => !(e.Wins == 0 && e.Losses == 0))
        //        .GroupBy(e => new { e.Wins, e.Losses })
        //        .OrderByDescending(g => g.Key.Wins)   // more wins first
        //        .ThenBy(g => g.Key.Losses);           // fewer losses first

        //    var resolvedList = new List<StandingsEntry>();

        //    foreach (var group in groupedByRecord)
        //    {
        //        var tiedTeams = group.Select(e => e.TeamName).ToList();

        //        if (tiedTeams.Count > 1)
        //        {
        //            Console.WriteLine($"[DEBUG] Tie detected in {group.Key.Wins}-{group.Key.Losses} group: {string.Join(", ", tiedTeams)}");

        //            // Keep resolving until all tied teams are ranked
        //            var remaining = new List<string>(tiedTeams);
        //            while (remaining.Count > 0)
        //            {
        //                (string, string) tieBreakerResult = tournament.TieBreakerRule.ResolveTie(remaining, tournament.MatchLog);
        //                string winner = tieBreakerResult.Item2;
        //                var winnerEntry = group.First(e => e.TeamName == winner);

        //                resolvedList.Add(winnerEntry);
        //                remaining.Remove(winner);

        //                Console.WriteLine($"[DEBUG] -> Placed {winner} at next rank, {remaining.Count} left in tie group");
        //            }
        //        }
        //        else
        //        {
        //            resolvedList.AddRange(group);
        //        }
        //    }

        //    // TODO Move rank assignment logic to Report Match Logic to fix bugs
        //    // Assign ranks after resolution
        //    for (int i = 0; i < resolvedList.Count; i++)
        //        resolvedList[i].Rank = i + 1;

        //    standings.Entries = resolvedList;

        //    // Update ranks in original entries as well
        //    foreach (var entry in standings.Entries)
        //    {
        //        //if (entry.Wins == 0 && entry.Losses == 0)
        //        //{
        //        //    continue;
        //        //}
        //        var original = tournament.Teams.First(t => t.Name == entry.TeamName);
        //        original.Rank = entry.Rank;
        //        tournament.Teams = tournament.Teams.OrderBy(t => t.Rank).ToList();
        //    }

        //    //_dataManager.SaveAndReloadTournamentsDatabase();

        //    Console.WriteLine("[DEBUG] Final Standings:");
        //    foreach (var entry in standings.Entries)
        //    {
        //        Console.WriteLine($"   Rank {entry.Rank}: {entry.TeamName} ({entry.Wins}-{entry.Losses}, {entry.TotalScore} pts)");
        //    }

        //    tournament.RoundRobinStandings = standings;
        //}
    }
}
