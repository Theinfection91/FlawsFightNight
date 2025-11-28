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

        public Tournament CreateNewTournament(string name, TournamentType tournamentType, int teamSize, string? description = null)
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

        public List<Tournament> GetAllLadderTournaments()
        {
            List<Tournament> ladderTournaments = new();
            foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                if (tournament is NormalLadderTournament)
                {
                    ladderTournaments.Add(tournament);
                }
            }
            return ladderTournaments;
        }

        public List<Tournament> GetAllRoundRobinTournaments()
        {
            List<Tournament> roundRobinTournaments = new();
            foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                if (tournament is NormalRoundRobinTournament || tournament is OpenRoundRobinTournament)
                {
                    roundRobinTournaments.Add(tournament);
                }
            }
            return roundRobinTournaments;
        }

        public List<Tournament> GetAllEliminationTournaments()
        {
            // TODO

            return null;
        }

        public Tournament? GetTournamentById(string tournamentId)
        {
            return _dataManager.TournamentsDatabaseFile.Tournaments
                .FirstOrDefault(t => t.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase));
        }

        public Tournament GetTournamentFromTeamName(string teamName)
        {
            foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
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
            foreach (var tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                if (tournament.MatchLog.GetAllActiveMatches().Any(m => m.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase)))
                {
                    return tournament;
                }
                if (tournament.MatchLog.GetAllPostMatches().Any(m => m.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase)))
                {
                    return tournament;
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
    }
}
