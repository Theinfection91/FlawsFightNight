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

        // GitHub Credential File
        public GitHubCredentialFile GitHubCredentialFile { get; private set; }
        private readonly GitHubCredentialHandler _gitHubCredentialHandler;

        // Permissions Config
        public PermissionsConfigFile PermissionsConfigFile { get; private set; }
        private readonly PermissionsConfigHandler _permissionsConfigHandler;

        // Tournaments Database
        public TournamentsDatabaseFile TournamentsDatabaseFile { get; private set; }
        private readonly TournamentsDatabaseHandler _tournamentsDatabaseHandler;

        // Constructor is given each handler type for each specific JSON file
        public DataManager(DiscordCredentialHandler discordCredentialHandler, GitHubCredentialHandler gitHubCredentialHandler, PermissionsConfigHandler permissionsConfigHandler, TournamentsDatabaseHandler tournamentsDatabaseHandler)
        {
            _discordCredentialHandler = discordCredentialHandler;
            LoadDiscordCredentialFile();

            _gitHubCredentialHandler = gitHubCredentialHandler;
            LoadGitHubCredentialFile();

            _permissionsConfigHandler = permissionsConfigHandler;
            LoadPermissionsConfigFile();

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

        #region GitHub Credential File Data
        public void LoadGitHubCredentialFile()
        {
            GitHubCredentialFile = _gitHubCredentialHandler.Load();
        }

        public void SaveGitHubCredentialFile()
        {
            _gitHubCredentialHandler.Save(GitHubCredentialFile);
        }

        public void SaveAndReloadGitHubCredentialFile()
        {
            _gitHubCredentialHandler.Save(GitHubCredentialFile);
            LoadGitHubCredentialFile();
        }
        #endregion

        #region Permissions Config Data
        public void LoadPermissionsConfigFile()
        {
            PermissionsConfigFile = _permissionsConfigHandler.Load();
        }

        public void SavePermissionsConfigFile()
        {
            _permissionsConfigHandler.Save(PermissionsConfigFile);
        }

        public void SaveAndReloadPermissionsConfigFile()
        {
            _permissionsConfigHandler.Save(PermissionsConfigFile);
            LoadPermissionsConfigFile();
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

        public void RemoveTournament(string tournamentId)
        {
            var tournament = TournamentsDatabaseFile.Tournaments.FirstOrDefault(t => t.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase));
            if (tournament != null)
            {
                TournamentsDatabaseFile.Tournaments.Remove(tournament);
                SaveAndReloadTournamentsDatabase();
            }
        }
        #endregion
    }
}
