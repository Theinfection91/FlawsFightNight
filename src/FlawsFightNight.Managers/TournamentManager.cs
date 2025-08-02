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
            switch (tournamentType)
            {
                case TournamentType.RoundRobin:
                    return new RoundRobinTournament(name, description)
                    {
                        TeamSize = teamSize,
                        Description = description
                    };
                default:
                    return null;
            }
        }
    }
}
