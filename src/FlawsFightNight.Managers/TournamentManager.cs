using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
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

        public void AddTournament(TournamentBase tournament)
        {
            _dataManager.AddTournament(tournament);
        }

        public bool IsTournamentNameUnique(string tournamentName)
        {
            List<TournamentBase> tournaments = _dataManager.TournamentsDatabaseFile.NewTournaments;
            foreach (TournamentBase tournament in tournaments)
            {
                if (tournament.Name.Equals(tournamentName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true; // Team name is unique across all tournaments
        }

        public TournamentBase CreateNewTournament(string name, TournamentType tournamentType, int teamSize, string? description = null)
        {
            string? id = GenerateTournamentId();
            if (id == null)
            {
                return null;
            }

            switch (tournamentType)
            {
                case TournamentType.NormalLadder:
                    return new NormalLadderTournament(id, name, teamSize);

                case TournamentType.NormalRoundRobin:
                    return new NormalRoundRobinTournament(id, name, teamSize);

                case TournamentType.OpenRoundRobin:
                    return new OpenRoundRobinTournament(id, name, teamSize);

                default:
                    return null;
            }
        }

        public void DeleteTournament(string tournamentId)
        {
            _dataManager.RemoveTournamentBase(tournamentId);
        }

        public void SetCanTeamsBeLocked(ITeamLocking tournament, bool canTeamsBeLocked)
        {
            tournament.CanTeamsBeLocked = canTeamsBeLocked;
        }

        public bool IsTournamentIdInDatabase(string tournamentId, bool isCaseSensitive = false)
        {
            if (isCaseSensitive)
            {
                return _dataManager.TournamentsDatabaseFile.NewTournaments
                    .Any(t => t.Id.Equals(tournamentId));
            }
            else
            {
                return _dataManager.TournamentsDatabaseFile.NewTournaments
                    .Any(t => t.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase));
            }
        }

        public bool IsTeamsInSameTournament(TournamentBase tournament, Team teamA, Team teamB)
        {
            var teams = new List<Team> { teamA, teamB };
            foreach (Team team in teams)
            {
                if (!tournament.Teams.Contains(team))
                {
                    return false; // At least one team is not in the tournament
                }
            }
            return true; // All teams are in the tournament
        }

        public List<Tournament> GetAllTournaments()
        {
            return _dataManager.TournamentsDatabaseFile.Tournaments;
        }

        public List<Tournament> GetAllLadderTournaments()
        {
            return _dataManager.TournamentsDatabaseFile.Tournaments
                .Where(t => t.Type == TournamentType.Ladder)
                .ToList();
        }

        public List<Tournament> GetAllRoundRobinTournaments()
        {
            return _dataManager.TournamentsDatabaseFile.Tournaments
                .Where(t => t.Type == TournamentType.RoundRobin)
                .ToList();
        }

        public List<Tournament> GetAllEliminationTournaments()
        {
            // TODO

            return null;
        }

        // TODO Old version, remove later
        public Tournament? GetTournamentById(string tournamentId)
        {
            return _dataManager.TournamentsDatabaseFile.Tournaments
                .FirstOrDefault(t => t.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase));
        }

        // New version
        public TournamentBase? GetNewTournamentById(string tournamentId)
        {
            return _dataManager.TournamentsDatabaseFile.NewTournaments
                .FirstOrDefault(t => t.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase));
        }

        public TournamentBase GetTournamentFromTeamName(string teamName)
        {
            foreach (var tournament in _dataManager.TournamentsDatabaseFile.NewTournaments)
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
                foreach (var round in tournament.MatchLog.MatchesToPlayByRound.Values)
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
            foreach (Tournament tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                foreach (var match in tournament.MatchLog.OpenRoundRobinMatchesToPlay)
                {
                    if (match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                    {
                        return tournament;
                    }
                }
            }
            foreach (Tournament tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                foreach (var match in tournament.MatchLog.OpenRoundRobinPostMatches)
                {
                    if (match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                    {
                        return tournament;
                    }
                }
            }
            // Ladder Matches
            foreach (Tournament tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                foreach (var match in tournament.MatchLog.LadderMatchesToPlay)
                {
                    if (match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                    {
                        return tournament;
                    }
                }
            }
            foreach (Tournament tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                foreach (var match in tournament.MatchLog.LadderPostMatches)
                {
                    if (match.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase))
                    {
                        return tournament;
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
            TournamentBase? tournament = GetNewTournamentById(tournamentId);
            if (tournament != null)
            {
                tournament.Teams.Add(team);
            }
        }
    }
}
