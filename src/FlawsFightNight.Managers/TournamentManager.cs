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
            tournament.CanAdvanceToNextRound = false;
        }

        private void EndRoundRobinTournament(Tournament tournament)
        {
            // Logic to handle the end of a round robin tournament
            tournament.IsRunning = false;
        }

        public bool IsTournamentReadyToEnd(Tournament tournament)
        {
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    return false; // Ladder tournaments do not end based on rounds
                case TournamentType.RoundRobin:
                    return tournament.IsRunning && tournament.CanEndTournament;
                case TournamentType.SingleElimination:
                case TournamentType.DoubleElimination:
                    // SE/DE end logic would go here
                    return false;
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
