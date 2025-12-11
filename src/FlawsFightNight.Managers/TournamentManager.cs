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

        // New Data System
        public void SaveTournament(Tournament tournament)
        {
            foreach (var tournamentData in _dataManager.TournamentDataFiles)
            {
                if (tournamentData.Tournament.Id.Equals(tournament.Id, StringComparison.OrdinalIgnoreCase))
                {
                    _dataManager.SaveTournamentDataFile(tournamentData);
                    return;
                }
            }
            // No existing tournament data file found, create a new one
            _dataManager.AddNewTournament(tournament);
        }

        public void LoadTournamentDataFiles()
        {
            _dataManager.LoadTournamentDataFiles();
        }

        public void SaveAndReloadTournamentDataFiles(Tournament tournament)
        {
            SaveTournament(tournament);
            LoadTournamentDataFiles();
        }

        public bool IsTournamentNameUnique(string tournamentName)
        {
            //List<Tournament> tournaments = _dataManager.TournamentsDatabaseFile.Tournaments;
            //foreach (Tournament tournament in tournaments)
            //{
            //    if (tournament.Name.Equals(tournamentName, StringComparison.OrdinalIgnoreCase))
            //    {
            //        return false;
            //    }
            //}
            //return true; // Team name is unique across all tournaments

            foreach (var dataFile in _dataManager.TournamentDataFiles)
            {
                if (dataFile.Tournament.Name.Equals(tournamentName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
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
                case TournamentType.DSRLadder:
                    return new DSRLadderTournament(id, name, teamSize);

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
            _dataManager.RemoveTournament(tournamentId);
        }

        public void SetCanTeamsBeLocked(ITeamLocking tournament, bool canTeamsBeLocked)
        {
            tournament.CanTeamsBeLocked = canTeamsBeLocked;
        }

        public bool IsTournamentIdInDatabase(string tournamentId, bool isCaseSensitive = false)
        {
            if (isCaseSensitive)
            {
                //return _dataManager.TournamentsDatabaseFile.Tournaments
                //    .Any(t => t.Id.Equals(tournamentId));
                foreach (var dataFile in _dataManager.TournamentDataFiles)
                {
                    if (dataFile.Tournament.Id.Equals(tournamentId))
                    {
                        return true;
                    }
                }
            }
            else
            {
                foreach (var dataFile in _dataManager.TournamentDataFiles)
                {
                    if (dataFile.Tournament.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public List<Tournament> GetAllTournaments()
        {
            //return _dataManager.TournamentsDatabaseFile.Tournaments;
            return _dataManager.TournamentDataFiles.Select(df => df.Tournament).ToList();
        }

        public List<Tournament> GetAllLadderTournaments()
        {
            List<Tournament> ladderTournaments = new();
            foreach (var tournament in _dataManager.TournamentDataFiles.Select(df => df.Tournament))
            {
                if (tournament is NormalLadderTournament)
                {
                    ladderTournaments.Add(tournament);
                }
                else if (tournament is DSRLadderTournament)
                {
                    ladderTournaments.Add(tournament);
                }
            }
            return ladderTournaments;
        }

        public List<Tournament> GetAllRoundRobinTournaments()
        {
            List<Tournament> roundRobinTournaments = new();
            foreach (var tournament in _dataManager.TournamentDataFiles.Select(df => df.Tournament))
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
            //return _dataManager.TournamentsDatabaseFile.Tournaments
            //    .FirstOrDefault(t => t.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase));
            foreach (var dataFile in _dataManager.TournamentDataFiles)
            {
                if (dataFile.Tournament.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase))
                {
                    return dataFile.Tournament;
                }
            }
            return null;
        }

        public Tournament GetTournamentFromTeamName(string teamName)
        {
            foreach (var tournament in _dataManager.GetTournaments())
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
            foreach (var tournament in _dataManager.GetTournaments())
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
