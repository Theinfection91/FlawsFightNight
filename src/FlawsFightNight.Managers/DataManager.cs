using FlawsFightNight.Core.Models;
using FlawsFightNight.Data.DataModels;
using FlawsFightNight.Data.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class DataManager
    {
        #region Fields and Constructor
        public string Name { get; set; } = "DataManager";

        // Discord Credential File
        public DiscordCredentialFile DiscordCredentialFile { get; private set; }
        private readonly DiscordCredentialHandler _discordCredentialHandler;

        // Tournaments Database Handler
        public TournamentsDatabaseFile TournamentsDatabaseFile { get; private set; }
        private readonly TournamentsDatabaseHandler _tournamentsDatabaseHandler;

        // Constructor is given each handler type for each specific JSON file
        public DataManager(DiscordCredentialHandler discordCredentialHandler, TournamentsDatabaseHandler tournamentsDatabaseHandler)
        {
            _discordCredentialHandler = discordCredentialHandler;
            LoadDiscordCredentialFile();

            _tournamentsDatabaseHandler = tournamentsDatabaseHandler;
            LoadTournamentsDatabase();
        }
        #endregion

        #region Discord Credential File Data
        public void LoadDiscordCredentialFile()
        {
            DiscordCredentialFile = _discordCredentialHandler.Load();
        }

        public void SaveDiscordCredentialFile()
        {
            _discordCredentialHandler.Save(DiscordCredentialFile);
        }

        public void SaveAndReloadDiscordCredentialFile()
        {
            _discordCredentialHandler.Save(DiscordCredentialFile);
            LoadDiscordCredentialFile();
        }
        #endregion

        #region Tournaments Data
        public void LoadTournamentsDatabase()
        {
            TournamentsDatabaseFile = _tournamentsDatabaseHandler.Load();
        }

        public void SaveTournamentsDatabase()
        {
            _tournamentsDatabaseHandler.Save(TournamentsDatabaseFile);
        }

        public void SaveAndReloadTournamentsDatabase()
        {
            _tournamentsDatabaseHandler.Save(TournamentsDatabaseFile);
            LoadTournamentsDatabase();
        }

        public void AddTournament(Tournament tournament)
        {
            TournamentsDatabaseFile.Tournaments.Add(tournament);
            SaveAndReloadTournamentsDatabase();
        }

        public void RemoveTournament()
        {
            // TODO: Implement removal logic
        }
        #endregion
    }
}
