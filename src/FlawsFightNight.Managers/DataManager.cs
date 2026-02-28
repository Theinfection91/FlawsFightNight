using Discord.WebSocket;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Data.Models;
using FlawsFightNight.Data.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Models.UT2004;

namespace FlawsFightNight.Managers
{
    public class DataManager
    {
        #region Fields and Constructor
        public string Name { get; set; } = "DataManager";

        // Discord Client
        public readonly DiscordSocketClient DiscordClient;
        #region Credentials
        // Discord Credential File
        public DiscordCredentialFile DiscordCredentialFile { get; private set; }
        private readonly DiscordCredentialHandler _discordCredentialHandler;

        // GitHub Credential File
        public GitHubCredentialFile GitHubCredentialFile { get; private set; }
        private readonly GitHubCredentialHandler _gitHubCredentialHandler;

        public FTPCredentialFile FTPCredentialFile { get; private set; }
        private readonly FTPCredentialHandler _ftpCredentialHandler;
        #endregion
        #region Databases
        // Permissions Config
        public PermissionsConfigFile PermissionsConfigFile { get; private set; }
        private readonly PermissionsConfigHandler _permissionsConfigHandler;

        // Tournament Data System
        public List<TournamentDataFile> TournamentDataFiles { get; private set; } = new();
        private readonly TournamentDataHandler _tournamentDataHandler;

        // Processed Log Names File
        public ProcessedLogNamesFile ProcessedLogNamesFile { get; private set; }
        private readonly ProcessedLogNamesHandler _processedLogNamesHandler;

        // Stat Log Match Results File
        // Stat Log files are lazy loaded
        private readonly StatLogMatchResultHandler _statLogMatchResultsHandler;

        // User Profile Files
        public List<UserProfileFile> UserProfileFiles { get; private set; } = new();
        private readonly UserProfileHandler _userProfileHandler;

        // UT2004 Player Profile File
        public List<UT2004PlayerProfileFile> UT2004PlayerProfileFiles { get; private set; }
        private readonly UT2004PlayerProfileHandler _ut2004PlayerProfileHandler;
        #endregion

        public DataManager(DiscordSocketClient client, DiscordCredentialHandler discordCredentialHandler, GitHubCredentialHandler gitHubCredentialHandler, FTPCredentialHandler ftpCredentialHandler, PermissionsConfigHandler permissionsConfigHandler, TournamentDataHandler tournamentDataHandler, ProcessedLogNamesHandler processedLogNamesHandler, StatLogMatchResultHandler statLogMatchResultHandler, UserProfileHandler userProfileHandler, UT2004PlayerProfileHandler ut2004PlayerProfileHandler)
        {
            DiscordClient = client;

            _discordCredentialHandler = discordCredentialHandler;
            //LoadDiscordCredentialFile();

            _gitHubCredentialHandler = gitHubCredentialHandler;
            //LoadGitHubCredentialFile();

            _ftpCredentialHandler = ftpCredentialHandler;
            //LoadFTPCredentialFiles();

            _permissionsConfigHandler = permissionsConfigHandler;
            //LoadPermissionsConfigFile();

            _tournamentDataHandler = tournamentDataHandler;
            //LoadTournamentDataFiles();

            _processedLogNamesHandler = processedLogNamesHandler;
            //LoadProcessedLogNamesFile();

            _statLogMatchResultsHandler = statLogMatchResultHandler;
            // Stat Log files are lazy loaded when data is needed

            _userProfileHandler = userProfileHandler;
            //LoadAllUserProfileFiles();

            _ut2004PlayerProfileHandler = ut2004PlayerProfileHandler;
            //LoadAllUT2004PlayerProfileFiles();
        }
        #endregion

        #region Intialization
        public async Task InitializeAsync()
        {
            // Helper method to check for and invoke InitializePendingPathAsync on handlers that have it
            // This is to fix having async methods in the handlers constructors
            static async Task InvokeInitIfExistsAsync(object? handler)
            {
                if (handler == null) return;
                var initMethod = handler.GetType().GetMethod("InitializePendingPathAsync", BindingFlags.Public | BindingFlags.Instance);
                if (initMethod == null) return;

                var result = initMethod.Invoke(handler, null);
                if (result is Task task) await task;
            }
            // Invoke initialization on all handlers that have pending paths to set up
            await InvokeInitIfExistsAsync(_discordCredentialHandler);
            await InvokeInitIfExistsAsync(_gitHubCredentialHandler);
            await InvokeInitIfExistsAsync(_ftpCredentialHandler);
            await InvokeInitIfExistsAsync(_permissionsConfigHandler);
            await InvokeInitIfExistsAsync(_tournamentDataHandler);
            await InvokeInitIfExistsAsync(_processedLogNamesHandler);
            await InvokeInitIfExistsAsync(_statLogMatchResultsHandler);
            await InvokeInitIfExistsAsync(_userProfileHandler);
            await InvokeInitIfExistsAsync(_ut2004PlayerProfileHandler);

            // After all pending paths are initialized, load the data from those paths
            await LoadDiscordCredentialFile();
            await LoadFTPCredentialFiles();
            await LoadGitHubCredentialFile();
            await LoadProcessedLogNamesFile();
            await LoadTournamentDataFiles();
            await LoadAllUserProfileFiles();
            await LoadAllUT2004PlayerProfileFiles();

            // Changing all these to async soon, but for now they can stay synchronous
            LoadPermissionsConfigFile();
        }
        #endregion

        #region Discord Credential File
        public async Task LoadDiscordCredentialFile()
        {
            DiscordCredentialFile = await _discordCredentialHandler.Load();
        }

        public async Task SaveDiscordCredentialFile()
        {
            await _discordCredentialHandler.Save(DiscordCredentialFile);
        }

        public async Task SaveAndReloadDiscordCredentialFile()
        {
            await _discordCredentialHandler.Save(DiscordCredentialFile);
            await LoadDiscordCredentialFile();
        }
        #endregion

        #region GitHub Credential File
        public async Task LoadGitHubCredentialFile()
        {
            GitHubCredentialFile = await _gitHubCredentialHandler.Load();
        }

        public async Task SaveGitHubCredentialFile()
        {
            await _gitHubCredentialHandler.Save(GitHubCredentialFile);
        }

        public async Task SaveAndReloadGitHubCredentialFile()
        {
            await _gitHubCredentialHandler.Save(GitHubCredentialFile);
            await LoadGitHubCredentialFile();
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
        public async Task LoadTournamentDataFiles()
        {
            TournamentDataFiles = await _tournamentDataHandler.LoadAll("tournament.json", "Tournaments");
        }

        public async Task SaveTournamentDataFile(TournamentDataFile tournamentDataFile)
        {
            await _tournamentDataHandler.SetFilePathCustom("tournament.json", $"Databases/Tournaments/{tournamentDataFile.Tournament.Id}");
            await _tournamentDataHandler.Save(tournamentDataFile);
        }

        public async Task SaveAndReloadTournamentDataFiles(Tournament tournament)
        {
            foreach (var tournamentData in TournamentDataFiles)
            {
                if (tournamentData.Tournament.Id.Equals(tournament.Id, StringComparison.OrdinalIgnoreCase))
                {
                    await SaveTournamentDataFile(tournamentData);
                    await LoadTournamentDataFiles();
                    return;
                }
            }
            // No existing tournament data file found, create a new one
            await AddNewTournament(tournament);
            await LoadTournamentDataFiles();
        }

        public async Task AddNewTournament(Tournament tournament)
        {
            var newTournamentDataFile = new TournamentDataFile
            {
                Tournament = tournament
            };
            TournamentDataFiles.Add(newTournamentDataFile);
            await SaveTournamentDataFile(newTournamentDataFile);
        }

        public async Task RemoveTournament(string tournamentId)
        {
            // Remove from in-memory list
            TournamentDataFiles.RemoveAll(t => t.Tournament.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase));

            // Remove the actual folder and contents
            await _tournamentDataHandler.DeleteFolderAndContents(tournamentId);
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
            return await _statLogMatchResultsHandler.LoadAll("*.json", "StatLogs");
        }

        public async Task<StatLogMatchResultsFile> LoadStatLogMatchResultFile(string fileName)
        {
            await _statLogMatchResultsHandler.SetFilePath(PathOption.iCTFStatLogs, fileName);
            return await _statLogMatchResultsHandler.Load();
        }

        public async Task SaveStatLogMatchResultFile(UT2004StatLog statLog)
        {
            switch (statLog.GameMode)
            {
                case UT2004GameMode.iCTF:
                    await _statLogMatchResultsHandler.SetFilePath(PathOption.iCTFStatLogs, statLog.FileName);
                    break;
                case UT2004GameMode.TAM:
                    await _statLogMatchResultsHandler.SetFilePath(PathOption.TAMStatLogs, statLog.FileName);
                    break;
                case UT2004GameMode.iBR:
                    await _statLogMatchResultsHandler.SetFilePath(PathOption.iBRStatLogs, statLog.FileName);
                    break;
                default:
                    await _statLogMatchResultsHandler.SetFilePath(PathOption.iCTFStatLogs, statLog.FileName);
                    break;
            }
            var statLogMatchResultsFile = new StatLogMatchResultsFile()
            {
                StatLog = statLog
            };
            await _statLogMatchResultsHandler.Save(statLogMatchResultsFile);
        }

        public async Task<int> GetStatLogCount(UT2004GameMode gameMode)
        {
            switch (gameMode)
            {
                case UT2004GameMode.iCTF:
                    return (await _statLogMatchResultsHandler.LoadAll("*.json", "StatLogs/iCTF")).Count;
                case UT2004GameMode.TAM:
                    return (await _statLogMatchResultsHandler.LoadAll("*.json", "StatLogs/TAM")).Count;
                case UT2004GameMode.iBR:
                    return (await _statLogMatchResultsHandler.LoadAll("*.json", "StatLogs/iBR")).Count;
                default:
                    return 0;
            }
        }
        #endregion

        #region User Profile Files
        public async Task LoadAllUserProfileFiles()
        {
            UserProfileFiles = await _userProfileHandler.LoadAll("*.json", "UserProfiles");
        }

        public async Task<UserProfileFile> LoadUserProfileFile(ulong discordId)
        {
            await _userProfileHandler.SetFilePath(PathOption.UserProfiles, $"{discordId}.json");
            return await _userProfileHandler.Load();
        }

        public async Task SaveUserProfileFile(UserProfile userProfile)
        {
            var userProfileFile = new UserProfileFile()
            {
                UserProfile = userProfile
            };
            await _userProfileHandler.SetFilePath(PathOption.UserProfiles, $"{userProfile.DiscordId}.json");
            await _userProfileHandler.Save(userProfileFile);
        }

        public async Task<UserProfile?> GetUserProfile(ulong discordId)
        {
            foreach (var profileFile in UserProfileFiles)
            {
                if (profileFile.UserProfile.DiscordId == discordId)
                {
                    return profileFile.UserProfile;
                }
            }
            return null;
        }


        #endregion

        #region UT2004 Player Profile Files
        public async Task LoadAllUT2004PlayerProfileFiles()
        {
            UT2004PlayerProfileFiles = await _ut2004PlayerProfileHandler.LoadAll("*.json", "UT2004PlayerProfiles");
        }

        public async Task<UT2004PlayerProfileFile> LoadUT2004PlayerProfileFile(string playerGuid)
        {
            await _ut2004PlayerProfileHandler.SetFilePath(PathOption.UT2004PlayerProfiles, $"{playerGuid}.json");
            return await _ut2004PlayerProfileHandler.Load();
        }
        public async Task SaveUT2004PlayerProfileFile(UT2004PlayerProfile playerProfile)
        {
            var playerProfileFile = new UT2004PlayerProfileFile()
            {
                PlayerProfile = playerProfile
            };
            await _ut2004PlayerProfileHandler.SetFilePath(PathOption.UT2004PlayerProfiles, $"{playerProfile.Guid}.json");
            await _ut2004PlayerProfileHandler.Save(playerProfileFile);
        }

        public async Task<UT2004PlayerProfile> GetUT2004PlayerProfile(string playerGuid)
        {

            foreach (var profileFile in UT2004PlayerProfileFiles)
            {
                if (profileFile.PlayerProfile.Guid.Equals(playerGuid, StringComparison.OrdinalIgnoreCase))
                {
                    return profileFile.PlayerProfile;
                }
            }
            return null;
        }

        public async Task DeleteUT2004ProfilesDatabase()
        {
            // Clear in-memory list
            UT2004PlayerProfileFiles.Clear();
            // Delete all profile files from disk
            await _ut2004PlayerProfileHandler.DeleteJsonFilesInFolder(PathOption.UT2004PlayerProfiles);
        }
        #endregion

        #region FTP Credential File
        public async Task LoadFTPCredentialFiles()
        {
            FTPCredentialFile = await _ftpCredentialHandler.Load();
        }

        public async Task SaveFTPCredentialFile(FTPCredentialFile ftpCredentialFile)
        {
            await _ftpCredentialHandler.Save(ftpCredentialFile);
        }

        public async Task SaveAndReloadFTPCredentialFile()
        {
            await _ftpCredentialHandler.Save(FTPCredentialFile);
            await LoadFTPCredentialFiles();
        }
        #endregion
    }
}
