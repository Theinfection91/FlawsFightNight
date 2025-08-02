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
    }
}
