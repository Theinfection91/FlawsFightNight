using Discord.WebSocket;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Models.UT2004;
using FlawsFightNight.IO.Handlers;
using FlawsFightNight.IO.Models;
using FlawsFightNight.Core.Models.Stats;

namespace FlawsFightNight.Services
{
    public class DataContext
    {
        #region Fields and Constructor
        public string Name { get; set; } = "DataContext";

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

        // Stat Log Index File (lightweight Id + MatchDate + ServerName lookup)
        public StatLogIndexFile StatLogIndexFile { get; private set; }
        private readonly StatLogIndexHandler _statLogIndexHandler;

        // Admin Ignored Logs File
        //public AdminIgnoredLogsFile AdminIgnoredLogsFile { get; private set; }
        //private readonly AdminIgnoredLogsHandler _adminIgnoredLogsHandler;

        // User Profile Files
        public List<MemberProfileFile> MemberProfileFiles { get; private set; } = new();
        private readonly MemberProfileHandler _memberProfileHandler;

        // UT2004 Player Profile File
        public List<UT2004PlayerProfileFile> UT2004PlayerProfileFiles { get; private set; }
        private readonly UT2004PlayerProfileHandler _ut2004PlayerProfileHandler;

        // Live View Channel Files
        private readonly SemaphoreSlim _liveViewLock = new(1, 1);
        public LiveViewChannelsFile LiveViewChannelsFile { get; private set; }
        private readonly LiveViewChannelsHandler _liveViewChannelsHandler;

        #endregion

        public DataContext(DiscordSocketClient client, DiscordCredentialHandler discordCredentialHandler, GitHubCredentialHandler gitHubCredentialHandler, FTPCredentialHandler ftpCredentialHandler, PermissionsConfigHandler permissionsConfigHandler, TournamentDataHandler tournamentDataHandler, ProcessedLogNamesHandler processedLogNamesHandler, StatLogMatchResultHandler statLogMatchResultHandler, StatLogIndexHandler statLogIndexHandler, MemberProfileHandler userProfileHandler, UT2004PlayerProfileHandler ut2004PlayerProfileHandler, LiveViewChannelsHandler liveViewChannelsHandler)
        {
            DiscordClient = client;

            _discordCredentialHandler = discordCredentialHandler;
            _gitHubCredentialHandler = gitHubCredentialHandler;
            _ftpCredentialHandler = ftpCredentialHandler;
            _permissionsConfigHandler = permissionsConfigHandler;
            _tournamentDataHandler = tournamentDataHandler;
            _processedLogNamesHandler = processedLogNamesHandler;
            _statLogMatchResultsHandler = statLogMatchResultHandler;
            _statLogIndexHandler = statLogIndexHandler;
            _memberProfileHandler = userProfileHandler;
            _ut2004PlayerProfileHandler = ut2004PlayerProfileHandler;
            _liveViewChannelsHandler = liveViewChannelsHandler;
        }
        #endregion

        #region Intialization
        public async Task InitializeAsync()
        {
            // Invoke initialization on all handlers that have pending paths to set up
            await _discordCredentialHandler.InitializePendingPathAsync();
            await _gitHubCredentialHandler.InitializePendingPathAsync();
            await _ftpCredentialHandler.InitializePendingPathAsync();
            await _permissionsConfigHandler.InitializePendingPathAsync();
            await _tournamentDataHandler.InitializePendingPathAsync();
            await _processedLogNamesHandler.InitializePendingPathAsync();
            await _statLogMatchResultsHandler.InitializePendingPathAsync();
            await _memberProfileHandler.InitializePendingPathAsync();
            await _ut2004PlayerProfileHandler.InitializePendingPathAsync();
            await _liveViewChannelsHandler.InitializePendingPathAsync();

            // After all pending paths are initialized, load the data from those paths
            await LoadDiscordCredentialFile();
            await LoadFTPCredentialFiles();
            await LoadGitHubCredentialFile();
            await LoadPermissionsConfigFile();
            await LoadProcessedLogNamesFile();
            await LoadTournamentDataFiles();
            await LoadAllMemberProfileFiles();
            await LoadAllUT2004PlayerProfileFiles();
            await LoadStatLogIndexFile();
            await LoadLiveViewChannelsFile();
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
        public async Task LoadPermissionsConfigFile()
        {
            PermissionsConfigFile = await _permissionsConfigHandler.Load();
        }

        public async Task SavePermissionsConfigFile()
        {
            await _permissionsConfigHandler.Save(PermissionsConfigFile);
        }

        public async Task SaveAndReloadPermissionsConfigFile()
        {
            await _permissionsConfigHandler.Save(PermissionsConfigFile);
            await LoadPermissionsConfigFile();
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
            TournamentDataFiles.RemoveAll(t => t.Tournament.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase));
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
            PathOption pathOption = statLog.GameMode switch
            {
                UT2004GameMode.iCTF => PathOption.iCTFStatLogs,
                UT2004GameMode.TAM => PathOption.TAMStatLogs,
                UT2004GameMode.iBR => PathOption.iBRStatLogs,
                _ => PathOption.iCTFStatLogs
            };

            await _statLogMatchResultsHandler.SetFilePath(pathOption, $"{statLog.Id}.json");
            await _statLogMatchResultsHandler.Save(new StatLogMatchResultsFile { StatLog = statLog });
        }

        public async Task<UT2004StatLog?> LoadStatLogByID(string statLogID)
        {
            PathOption pathOption;

            if (statLogID.StartsWith("iCTF", StringComparison.OrdinalIgnoreCase))
                pathOption = PathOption.iCTFStatLogs;
            else if (statLogID.StartsWith("TAM", StringComparison.OrdinalIgnoreCase))
                pathOption = PathOption.TAMStatLogs;
            else if (statLogID.StartsWith("iBR", StringComparison.OrdinalIgnoreCase))
                pathOption = PathOption.iBRStatLogs;
            else
                return null;

            await _statLogMatchResultsHandler.SetFilePath(pathOption, $"{statLogID}.json");
            var file = await _statLogMatchResultsHandler.Load();
            return file?.StatLog;
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

        public async Task<List<UT2004StatLog>> GetStatLogsByGameMode(UT2004GameMode gameMode)
        {
            List<UT2004StatLog> statLogs = new();
            switch (gameMode)
            {
                case UT2004GameMode.iCTF:
                    var iCTFFiles = await _statLogMatchResultsHandler.LoadAll("*.json", "StatLogs/iCTF");
                    statLogs.AddRange(iCTFFiles.Select(f => f.StatLog));
                    break;
                case UT2004GameMode.TAM:
                    var TAMFiles = await _statLogMatchResultsHandler.LoadAll("*.json", "StatLogs/TAM");
                    statLogs.AddRange(TAMFiles.Select(f => f.StatLog));
                    break;
                case UT2004GameMode.iBR:
                    var iBRFiles = await _statLogMatchResultsHandler.LoadAll("*.json", "StatLogs/iBR");
                    statLogs.AddRange(iBRFiles.Select(f => f.StatLog));
                    break;
                default:
                    break;
            }
            return statLogs;
        }

        public async Task<List<UT2004StatLog>> GetAllStatLogs()
        {
            List<UT2004StatLog> statLogs = new();
            var iCTFFiles = await _statLogMatchResultsHandler.LoadAll("*.json", "StatLogs/iCTF");
            var TAMFiles = await _statLogMatchResultsHandler.LoadAll("*.json", "StatLogs/TAM");
            var iBRFiles = await _statLogMatchResultsHandler.LoadAll("*.json", "StatLogs/iBR");
            statLogs.AddRange(iCTFFiles.Select(f => f.StatLog));
            statLogs.AddRange(TAMFiles.Select(f => f.StatLog));
            statLogs.AddRange(iBRFiles.Select(f => f.StatLog));
            return statLogs;
        }
        #endregion

        #region Member Profile Files
        public async Task LoadAllMemberProfileFiles()
        {
            MemberProfileFiles = await _memberProfileHandler.LoadAll("*.json", "MemberProfiles");
        }

        public async Task<MemberProfileFile> LoadMemberProfileFile(ulong discordId)
        {
            await _memberProfileHandler.SetFilePath(PathOption.MemberProfiles, $"{discordId}.json");
            return await _memberProfileHandler.Load();
        }

        public MemberProfileFile CreateNewMemberProfileFile(MemberProfile memberProfile)
        {
            return new MemberProfileFile()
            {
                MemberProfile = memberProfile
            };
        }

        public void AddNewMemberProfileFile(MemberProfileFile memberProfile)
        {
            MemberProfileFiles.Add(memberProfile);
        }

        public async Task SaveMemberProfileFile(MemberProfile userProfile)
        {
            try
            {
                var userProfileFile = new MemberProfileFile()
                {
                    MemberProfile = userProfile
                };
                await _memberProfileHandler.SetFilePath(PathOption.MemberProfiles, $"{userProfile.DiscordId}.json");
                await _memberProfileHandler.Save(userProfileFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving member profile for Discord ID {userProfile.DiscordId}: {ex.Message}");
            }
        }

        public async Task SaveAllMemberProfileFiles()
        {
            foreach (var profileFile in MemberProfileFiles)
            {
                await _memberProfileHandler.SetFilePath(PathOption.MemberProfiles, $"{profileFile.MemberProfile.DiscordId}.json");
                await _memberProfileHandler.Save(profileFile);
            }
        }

        public MemberProfile? GetMemberProfile(ulong discordId)
        {
            foreach (var profileFile in MemberProfileFiles)
            {
                if (profileFile.MemberProfile.DiscordId == discordId)
                {
                    return profileFile.MemberProfile;
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

            // Keep the in-memory list in sync so same-session lookups (e.g. profileNames
            // in GetStatLogByID) can resolve profiles that were just created or updated.
            var existing = UT2004PlayerProfileFiles
                .FirstOrDefault(f => f.PlayerProfile?.Guid != null &&
                                     f.PlayerProfile.Guid.Equals(playerProfile.Guid, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                existing.PlayerProfile = playerProfile;
            else
                UT2004PlayerProfileFiles.Add(playerProfileFile);
        }

        public UT2004PlayerProfile? GetUT2004PlayerProfile(string playerGuid)
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
            UT2004PlayerProfileFiles.Clear();
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

        #region Stat Log Index
        public async Task LoadStatLogIndexFile()
        {
            await _statLogIndexHandler.SetFilePath(PathOption.Databases, "stat_log_index.json");
            StatLogIndexFile = await _statLogIndexHandler.Load();
        }

        public async Task SaveStatLogIndexFile()
        {
            await _statLogIndexHandler.SetFilePath(PathOption.Databases, "stat_log_index.json");
            await _statLogIndexHandler.Save(StatLogIndexFile);
        }

        public async Task SaveAndReloadStatLogIndexFile()
        {
            await _statLogIndexHandler.SetFilePath(PathOption.Databases, "stat_log_index.json");
            await _statLogIndexHandler.Save(StatLogIndexFile);
            await LoadStatLogIndexFile();
        }

        public async Task AddStatLogIndexEntry(StatLogIndexEntry entry)
        {
            if (StatLogIndexFile == null)
                await LoadStatLogIndexFile();

            StatLogIndexFile!.Entries.Add(entry);
            await SaveStatLogIndexFile();
        }

        public StatLogIndexEntry? GetStatLogIndexEntry(String statLogId)
        {
            return StatLogIndexFile?.Entries.FirstOrDefault(e => e.Id.Equals(statLogId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task RebuildStatLogIndex(List<StatLogIndexEntry> entries)
        {
            StatLogIndexFile = new StatLogIndexFile { Entries = entries };
            await SaveStatLogIndexFile();
        }
        #endregion

        #region LiveView Channels
        public async Task LoadLiveViewChannelsFile()
        {
            await _liveViewChannelsHandler.SetFilePath(PathOption.Databases, "liveview_channels.json");
            LiveViewChannelsFile = await _liveViewChannelsHandler.Load();
        }

        public async Task SaveLiveViewChannelsFile()
        {
            await _liveViewChannelsHandler.SetFilePath(PathOption.Databases, "liveview_channels.json");
            await _liveViewChannelsHandler.Save(LiveViewChannelsFile);
        }

        public async Task AddLeaderboardChannel(LeaderboardChannelData channelData)
        {
            await _liveViewLock.WaitAsync();
            try
            {
                if (LiveViewChannelsFile == null)
                    await LoadLiveViewChannelsFile();

                LiveViewChannelsFile!.LeaderboardChannels.Add(channelData);
                await SaveLiveViewChannelsFile();
            }
            finally { _liveViewLock.Release(); }
        }

        public async Task RemoveLeaderboardChannel(ulong channelId)
        {
            await _liveViewLock.WaitAsync();
            try
            {
                if (LiveViewChannelsFile == null)
                    await LoadLiveViewChannelsFile();

                LiveViewChannelsFile!.LeaderboardChannels.RemoveAll(c => c.ChannelId == channelId);
                await SaveLiveViewChannelsFile();
            }
            finally { _liveViewLock.Release(); }
        }

        public LeaderboardChannelData? GetLeaderboardChannel(ulong channelId)
        {
            return LiveViewChannelsFile?.LeaderboardChannels.FirstOrDefault(c => c.ChannelId == channelId);
        }

        public List<LeaderboardChannelData> GetAllLeaderboardChannels()
        {
            return LiveViewChannelsFile?.LeaderboardChannels ?? new List<LeaderboardChannelData>();
        }   

        public async Task SaveAndReloadLeaderboardChannelsFile()
        {
            await SaveLiveViewChannelsFile();
            await LoadLiveViewChannelsFile();
        }

        public async Task SetAdminChannelFeedAsync(ulong channelId)
        {
            if (LiveViewChannelsFile == null)
                await LoadLiveViewChannelsFile();

            LiveViewChannelsFile!.AdminChannelFeedId = channelId;
            await SaveLiveViewChannelsFile();
        }

        public ulong GetAdminChannelFeedId()
        {
            return LiveViewChannelsFile?.AdminChannelFeedId ?? 0;
        }
        #endregion
    }
}
