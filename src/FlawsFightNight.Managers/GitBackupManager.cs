using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class GitBackupManager : BaseDataDriven
    {
        private readonly string _repoPath;
        private readonly string _remoteUrl;
        private readonly string _token;
        private readonly string _databasesFolderPath;
        private readonly string _gitUsername = "FlawsFightNight";

        private ConfigManager _configManager;
        private bool _isInitialized = false;
        private readonly CancellationTokenSource _shutdownToken = new();

        public GitBackupManager(ConfigManager configManager, DataManager dataManager) : base("Git Backup Manager", dataManager)
        {
            _configManager = configManager;

            // Grab info from GitHub Credential File
            _remoteUrl = _dataManager.GitHubCredentialFile.GitUrlPath;
            _token = _dataManager.GitHubCredentialFile.GitPatToken;

            // Set up repo path
            _repoPath = SetRepoFilePath();
            _databasesFolderPath = SetDatabasesFolder();

            // Only validate repo exists, don't do interactive prompts here
            if (_configManager.IsGitPatTokenSet())
            {
                ValidateRepository();
            }
            else
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Git PAT Token not set. Can not initialize the repo in 'BackupRepo'.");
            }
        }

        public async Task RunInteractiveSetup()
        {
            if (!_configManager.IsGitPatTokenSet() || _isInitialized)
                return;

            if (!Repository.IsValid(_repoPath))
            {
                CloneAndPromptForRestore();
            }

            _isInitialized = true;
        }

        private string SetDatabasesFolder()
        {
            string appBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appBaseDirectory, "Databases");
        }

        private string SetRepoFilePath()
        {
            string appBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string repoPath = Path.Combine(appBaseDirectory, "BackupRepo");

            if (!Directory.Exists(repoPath))
            {
                Directory.CreateDirectory(repoPath);
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Directory created: {repoPath}");
            }

            return repoPath;
        }

        private void ValidateRepository()
        {
            try
            {
                if (Repository.IsValid(_repoPath))
                {
                    Console.WriteLine($"{DateTime.Now} - GitBackupManager - Valid repository found at '{_repoPath}'.");
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error validating repository: {ex.Message}");
            }
        }

        private void CloneAndPromptForRestore()
        {
            try
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - No local repo found. Cloning repository...");

                var options = new CloneOptions
                {
                    FetchOptions =
                    {
                        CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
                        {
                            Username = _gitUsername,
                            Password = _token
                        },
                        OnProgress = output =>
                        {
                            Console.Write($"\r{DateTime.Now} - GitBackupManager - {output}                    ");
                            return true;
                        },
                        OnTransferProgress = progress =>
                        {
                            Console.Write($"\r{DateTime.Now} - GitBackupManager - Receiving: {progress.ReceivedObjects}/{progress.TotalObjects} ({progress.ReceivedBytes / 1024} KB)   ");
                            return true;
                        }
                    },
                    OnCheckoutProgress = (path, completedSteps, totalSteps) =>
                    {
                        if (totalSteps > 0)
                        {
                            Console.Write($"\r{DateTime.Now} - GitBackupManager - Checkout: {completedSteps}/{totalSteps}   ");
                        }
                    }
                };

                Repository.Clone(_remoteUrl, _repoPath, options);
                Console.WriteLine(); // New line after progress
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Repository cloned successfully.");

                PromptForDataRestore();
            }
            catch (LibGit2SharpException ex)
            {
                Console.WriteLine($"\n{DateTime.Now} - GitBackupManager - Error cloning repository: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n{DateTime.Now} - GitBackupManager - Error during repository initialization: {ex.Message}");
            }
        }

        private void PromptForDataRestore()
        {
            Console.WriteLine($"{DateTime.Now} - GitBackupManager - Do you want to use the newly cloned backup data from the repository as your Database?");
            Console.WriteLine("NOTE - This will overwrite data currently present in your JSON files in 'Databases'. This cannot be reversed.");
            Console.WriteLine("\nHINT: Enter Y if your backup repo is more up-to-date, N if your local 'Databases' folder is more current.");

            // Add timeout to prevent indefinite blocking
            var readTask = Task.Run(() => Console.ReadLine());
            
            while (true)
            {
                Console.WriteLine("Enter Y or N (timeout in 60 seconds):");
                
                if (readTask.Wait(TimeSpan.FromSeconds(60)))
                {
                    string? userInput = readTask.Result;

                    if (string.IsNullOrWhiteSpace(userInput))
                    {
                        Console.WriteLine($"{DateTime.Now} - GitBackupManager - Invalid input. Please enter Y or N.");
                        readTask = Task.Run(() => Console.ReadLine());
                        continue;
                    }

                    switch (userInput.ToLower().Trim())
                    {
                        case "y":
                            Console.WriteLine($"{DateTime.Now} - GitBackupManager - Copying files from 'BackupRepo' to 'Databases'...");
                            CopyFilesFromBackupRepoToDatabases();
                            _dataManager.LoadTournamentDataFiles();
                            _dataManager.LoadPermissionsConfigFile();
                            Console.WriteLine($"{DateTime.Now} - GitBackupManager - Data restored successfully.");
                            return;

                        case "n":
                            Console.WriteLine($"{DateTime.Now} - GitBackupManager - Skipped data restore. Local files will be used.");
                            return;

                        default:
                            Console.WriteLine($"{DateTime.Now} - GitBackupManager - Invalid input: '{userInput}'. Please enter Y or N.");
                            readTask = Task.Run(() => Console.ReadLine());
                            break;
                    }
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now} - GitBackupManager - Input timeout. Defaulting to 'N' - keeping local files.");
                    return;
                }
            }
        }

        private void CopyFileWithSharing(string source, string destination)
        {
            try
            {
                // Add timeout for file operations
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write);
                sourceStream.CopyTo(destinationStream);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error copying file {source}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Unexpected error copying {source}: {ex.Message}");
            }
        }

        public void CopyJsonFilesToBackupRepo()
        {
            try
            {
                if (!Directory.Exists(_databasesFolderPath))
                    return;

                // Clean up BackupRepo to mirror Databases exactly (preserves .git folder)
                CleanupBackupRepo();

                // Copy root-level JSON files
                var rootJsonFiles = Directory.GetFiles(_databasesFolderPath, "*.json", SearchOption.TopDirectoryOnly);
                foreach (var jsonFile in rootJsonFiles)
                {
                    if (_shutdownToken.Token.IsCancellationRequested)
                        return;

                    string fileName = Path.GetFileName(jsonFile);
                    string destFilePath = Path.Combine(_repoPath, fileName);
                    CopyFileWithSharing(jsonFile, destFilePath);
                }

                // Copy subdirectory structure and files
                var databaseSubdirectories = Directory.GetDirectories(_databasesFolderPath, "*", SearchOption.AllDirectories);
                foreach (var subdirectory in databaseSubdirectories)
                {
                    if (_shutdownToken.Token.IsCancellationRequested)
                        return;

                    string relativePath = Path.GetRelativePath(_databasesFolderPath, subdirectory);
                    string destinationPath = Path.Combine(_repoPath, relativePath);
                    Directory.CreateDirectory(destinationPath);

                    var jsonFiles = Directory.GetFiles(subdirectory, "*.json", SearchOption.TopDirectoryOnly);
                    foreach (var jsonFile in jsonFiles)
                    {
                        if (_shutdownToken.Token.IsCancellationRequested)
                            return;

                        string fileName = Path.GetFileName(jsonFile);
                        string destFilePath = Path.Combine(destinationPath, fileName);
                        CopyFileWithSharing(jsonFile, destFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error during backup process: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes all files and folders from BackupRepo that don't exist in Databases.
        /// Preserves the .git folder.
        /// </summary>
        private void CleanupBackupRepo()
        {
            try
            {
                if (!Directory.Exists(_repoPath))
                    return;

                // Delete files in BackupRepo that don't exist in Databases
                var repoFiles = Directory.GetFiles(_repoPath, "*.json", SearchOption.AllDirectories);
                foreach (var repoFile in repoFiles)
                {
                    if (_shutdownToken.Token.IsCancellationRequested)
                        return;

                    // Skip .git folder
                    if (repoFile.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar))
                        continue;

                    string relativePath = Path.GetRelativePath(_repoPath, repoFile);
                    string correspondingDbFile = Path.Combine(_databasesFolderPath, relativePath);

                    if (!File.Exists(correspondingDbFile))
                    {
                        try
                        {
                            // Add retry logic for locked files
                            int retries = 3;
                            while (retries > 0)
                            {
                                try
                                {
                                    File.Delete(repoFile);
                                    Console.WriteLine($"{DateTime.Now} - GitBackupManager - Deleted stale file: {relativePath}");
                                    break;
                                }
                                catch (IOException) when (retries > 1)
                                {
                                    retries--;
                                    Thread.Sleep(100);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{DateTime.Now} - GitBackupManager - Failed to delete {relativePath}: {ex.Message}");
                        }
                    }
                }

                // Delete empty directories in BackupRepo (except .git)
                var repoDirectories = Directory.GetDirectories(_repoPath, "*", SearchOption.AllDirectories)
                    .OrderByDescending(d => d.Length); // Process deepest first

                foreach (var repoDir in repoDirectories)
                {
                    if (_shutdownToken.Token.IsCancellationRequested)
                        return;

                    // Skip .git folder
                    if (repoDir.Contains(Path.DirectorySeparatorChar + ".git"))
                        continue;

                    string relativePath = Path.GetRelativePath(_repoPath, repoDir);
                    string correspondingDbDir = Path.Combine(_databasesFolderPath, relativePath);

                    // Delete if directory doesn't exist in Databases OR is empty
                    if (!Directory.Exists(correspondingDbDir) ||
                        (!Directory.GetFiles(repoDir).Any() && !Directory.GetDirectories(repoDir).Any()))
                    {
                        try
                        {
                            Directory.Delete(repoDir, recursive: false);
                            Console.WriteLine($"{DateTime.Now} - GitBackupManager - Deleted stale folder: {relativePath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{DateTime.Now} - GitBackupManager - Failed to delete directory {relativePath}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error during cleanup: {ex.Message}");
            }
        }

        public void CopyFilesFromBackupRepoToDatabases()
        {
            try
            {
                if (!Directory.Exists(_repoPath))
                    return;

                var jsonFiles = Directory.GetFiles(_repoPath, "*.json", SearchOption.AllDirectories);

                foreach (var jsonFile in jsonFiles)
                {
                    // Skip .git folder files
                    if (jsonFile.Contains(Path.Combine(_repoPath, ".git")))
                        continue;

                    string relativePath = Path.GetRelativePath(_repoPath, jsonFile);
                    string destinationPath = Path.Combine(_databasesFolderPath, relativePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    CopyFileWithSharing(jsonFile, destinationPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error copying files from BackupRepo to Databases: {ex.Message}");
            }
        }

        public void BackupFiles()
        {
            try
            {
                if (_shutdownToken.Token.IsCancellationRequested)
                    return;

                using var repo = new Repository(_repoPath);

                // Stage ALL changes including deletions
                Commands.Stage(repo, "*");

                if (!repo.RetrieveStatus().IsDirty)
                {
                    Console.WriteLine($"{DateTime.Now} - GitBackupManager - No changes detected; nothing to backup.");
                    return;
                }

                var author = new Signature(_gitUsername, "backup@flawsfightnight.bot", DateTimeOffset.Now);
                repo.Commit($"Backup: Update data files ({DateTime.Now:yyyy-MM-dd HH:mm:ss})", author, author);

                var options = new PushOptions
                {
                    CredentialsProvider = (_, _, _) => new UsernamePasswordCredentials
                    {
                        Username = _gitUsername,
                        Password = _token
                    }
                };

                repo.Network.Push(repo.Branches["main"], options);
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Backup pushed successfully.");
            }
            catch (LibGit2SharpException ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error during push: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error during backup: {ex.Message}");
            }
        }

        public async Task CopyAndBackupFilesToGitAsync()
        {
            try
            {
                if (!_configManager.IsGitPatTokenSet())
                {
                    Console.WriteLine($"{DateTime.Now} - GitBackupManager - Git PAT Token not set. Git Backup Storage not enabled.");
                    return;
                }

                // Run with proper async/await and cancellation support
                await Task.Run(() =>
                {
                    try
                    {
                        if (_shutdownToken.Token.IsCancellationRequested)
                            return;

                        CopyJsonFilesToBackupRepo();
                        
                        if (_shutdownToken.Token.IsCancellationRequested)
                            return;
                            
                        BackupFiles();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error during background backup: {ex.Message}");
                    }
                }, _shutdownToken.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Backup operation cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error during CopyAndBackupFilesToGit: {ex.Message}");
            }
        }

        // Keep synchronous version for backwards compatibility
        public void CopyAndBackupFilesToGit()
        {
            _ = CopyAndBackupFilesToGitAsync();
        }

        public void Shutdown()
        {
            Console.WriteLine($"{DateTime.Now} - GitBackupManager - Shutting down...");
            _shutdownToken.Cancel();
        }
    }
}
