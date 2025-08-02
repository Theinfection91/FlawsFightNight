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
            //_dataManager.LoadTouramentsDatabase();
        }

        public void SaveTournamentsDatabase()
        {
            //_dataManager.SaveTournamentsDatabase(_dataManager.TournamentsDatabase);
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

        public Tournament CreateSpecificTournament(string name, TournamentType tournamentType, int teamSize, string? description = null)
        {
            string? id = GenerateTournamentId();
            switch (tournamentType)
            {
                case TournamentType.RoundRobin:
                    return new RoundRobinTournament(name, description)
                    {
                        Id = id,
                        TeamSize = teamSize,
                        Description = description
                    };
                default:
                    return null;
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
