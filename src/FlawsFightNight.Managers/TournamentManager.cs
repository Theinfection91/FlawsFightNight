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

        public bool CanAcceptNewTeams(Tournament tournament)
        {
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    return true; // Ladder tournaments can always accept new teams
                case TournamentType.RoundRobin:
                    return !tournament.IsRunning; // Round Robin tournaments cannot accept new teams once they start
                case TournamentType.SingleElimination:
                case TournamentType.DoubleElimination:
                    return !tournament.IsRunning; // SE/DE tournaments cannot accept new teams once they start
                default:
                    return false; // Unknown tournament type
            }
        }

        public bool IsTournamentIdInDatabase(string tournamentId)
        {
            foreach (Tournament tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                if (tournament.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public Tournament? GetTournamentById(string tournamentId)
        {
            return _dataManager.TournamentsDatabaseFile.Tournaments
                .FirstOrDefault(t => t.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase));
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
    }
}
