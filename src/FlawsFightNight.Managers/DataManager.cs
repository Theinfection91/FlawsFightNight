using Discord.WebSocket;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Data.Models;
using FlawsFightNight.Data.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Core.Models.Stats.UT2004;

namespace FlawsFightNight.Managers
{
    public class DataManager
    {
        #region Fields and Constructor
        public string Name { get; set; } = "DataManager";

        // Discord Client
        public readonly DiscordSocketClient DiscordClient;

        // Discord Credential File
        public DiscordCredentialFile DiscordCredentialFile { get; private set; }
        private readonly DiscordCredentialHandler _discordCredentialHandler;

        // GitHub Credential File
        public GitHubCredentialFile GitHubCredentialFile { get; private set; }
        private readonly GitHubCredentialHandler _gitHubCredentialHandler;

        // Permissions Config
        public PermissionsConfigFile PermissionsConfigFile { get; private set; }
        private readonly PermissionsConfigHandler _permissionsConfigHandler;

        // Tournament Data System
        public List<TournamentDataFile> TournamentDataFiles { get; private set; }
        private readonly TournamentDataHandler _tournamentDataHandler;

        // Processed Log Names File
        public ProcessedLogNamesFile ProcessedLogNamesFile { get; private set; }
        private readonly ProcessedLogNamesHandler _processedLogNamesHandler;

        // Stat Log Match Results File
        // Stat Log files are lazy loaded
        private readonly StatLogMatchResultHandler _statLogMatchResultsHandler;

        // Constructor is given each handler type for each specific JSON file
        public DataManager(DiscordSocketClient client, DiscordCredentialHandler discordCredentialHandler, GitHubCredentialHandler gitHubCredentialHandler, PermissionsConfigHandler permissionsConfigHandler, TournamentDataHandler tournamentDataHandler, ProcessedLogNamesHandler processedLogNamesHandler, StatLogMatchResultHandler statLogMatchResultHandler)
        {
            DiscordClient = client;

            _discordCredentialHandler = discordCredentialHandler;
            LoadDiscordCredentialFile();

            _gitHubCredentialHandler = gitHubCredentialHandler;
            LoadGitHubCredentialFile();

            _permissionsConfigHandler = permissionsConfigHandler;
            LoadPermissionsConfigFile();

            _tournamentDataHandler = tournamentDataHandler;
            LoadTournamentDataFiles();

            _processedLogNamesHandler = processedLogNamesHandler;
            LoadProcessedLogNamesFile();

            _statLogMatchResultsHandler = statLogMatchResultHandler;
            // Stat Log files will be lazy loaded when data is needed.
        }
        #endregion

        #region Discord Credential File
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

        #region GitHub Credential File
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

        #region Tournament Data System
        public void LoadTournamentDataFiles()
        {
            TournamentDataFiles = _tournamentDataHandler.LoadAll();
        }

        public void SaveTournamentDataFile(TournamentDataFile tournamentDataFile)
        {
            _tournamentDataHandler.SetFilePath(tournamentDataFile.Tournament.Id);
            _tournamentDataHandler.Save(tournamentDataFile);
        }

        public void SaveAndReloadTournamentDataFiles(Tournament tournament)
        {
            foreach (var tournamentData in TournamentDataFiles)
            {
                if (tournamentData.Tournament.Id.Equals(tournament.Id, StringComparison.OrdinalIgnoreCase))
                {
                    SaveTournamentDataFile(tournamentData);
                    LoadTournamentDataFiles();
                    return;
                }
            }
            // No existing tournament data file found, create a new one
            AddNewTournament(tournament);
            LoadTournamentDataFiles();
        }

        public void AddNewTournament(Tournament tournament)
        {
            var newTournamentDataFile = new TournamentDataFile
            {
                Tournament = tournament
            };
            TournamentDataFiles.Add(newTournamentDataFile);
            SaveTournamentDataFile(newTournamentDataFile);
        }

        public void RemoveTournament(string tournamentId)
        {
            // Remove from in-memory list
            TournamentDataFiles.RemoveAll(t => t.Tournament.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase));

            // Remove the actual folder and contents
            _tournamentDataHandler.DeleteFolderAndContents(tournamentId);
        }

        public List<Tournament> GetTournaments()
        {
            return TournamentDataFiles.Select(t => t.Tournament).ToList();
        }
        #endregion

        #region Processed Log File
        public async Task LoadProcessedLogNamesFile()
        {
            await _processedLogNamesHandler.SetFilePath(PathOption.Databases, "processed_log_names.json");
            ProcessedLogNamesFile = await _processedLogNamesHandler.Load();
        }

        public async Task SaveProcessedLogNamesFile()
        {
            //await _processedLogNamesHandler.Save(ProcessedLogNamesFile);
            await _processedLogNamesHandler.SetFilePath(PathOption.Databases, "processed_log_names.json");
            await _processedLogNamesHandler.Save(ProcessedLogNamesFile);
        }

        public async Task SaveAndReloadProcessedLogNamesFile()
        {
            await _processedLogNamesHandler.SetFilePath(PathOption.Databases, "processed_log_names.json");
            await _processedLogNamesHandler.Save(ProcessedLogNamesFile);
            await LoadProcessedLogNamesFile();
        }

        public ProcessedLogNamesFile GetProcessedLogNames()
        {
            return ProcessedLogNamesFile;
        }
        #endregion

        #region Valid Match Results File
        public async Task<List<StatLogMatchResultsFile>> LoadAllStatLogMatchResultFiles()
        {
            return await _statLogMatchResultsHandler.LoadAll();
        }

        public async Task<StatLogMatchResultsFile> LoadStatLogMatchResultFile(string fileName)
        {
            await _statLogMatchResultsHandler.SetFilePath(PathOption.StatLogs, fileName);
            return await _statLogMatchResultsHandler.Load();
        }

        public async Task SaveStatLogMatchResultFile(UT2004StatLog statLog)
        {
            var statLogMatchResultsFile = new StatLogMatchResultsFile()
            {
                StatLog = statLog
            };
            await _statLogMatchResultsHandler.SetFilePath(PathOption.StatLogs, statLog.FileName);
            await _statLogMatchResultsHandler.Save(statLogMatchResultsFile);
        }
        #endregion
    }
}
