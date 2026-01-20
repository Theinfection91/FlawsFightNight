using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public void RunInteractiveSetup()
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

            while (true)
            {
                Console.WriteLine("Enter Y or N:");
                string? userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    Console.WriteLine($"{DateTime.Now} - GitBackupManager - Invalid input. Please enter Y or N.");
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
                        break;
                }
            }
        }

        private void CopyFileWithSharing(string source, string destination)
        {
            try
            {
                using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write);
                sourceStream.CopyTo(destinationStream);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error copying file {source}: {ex.Message}");
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
                    string fileName = Path.GetFileName(jsonFile);
                    string destFilePath = Path.Combine(_repoPath, fileName);
                    CopyFileWithSharing(jsonFile, destFilePath);
                }

                // Copy subdirectory structure and files
                var databaseSubdirectories = Directory.GetDirectories(_databasesFolderPath, "*", SearchOption.AllDirectories);
                foreach (var subdirectory in databaseSubdirectories)
                {
                    string relativePath = Path.GetRelativePath(_databasesFolderPath, subdirectory);
                    string destinationPath = Path.Combine(_repoPath, relativePath);
                    Directory.CreateDirectory(destinationPath);

                    var jsonFiles = Directory.GetFiles(subdirectory, "*.json", SearchOption.TopDirectoryOnly);
                    foreach (var jsonFile in jsonFiles)
                    {
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
                    // Skip .git folder
                    if (repoFile.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar))
                        continue;

                    string relativePath = Path.GetRelativePath(_repoPath, repoFile);
                    string correspondingDbFile = Path.Combine(_databasesFolderPath, relativePath);

                    if (!File.Exists(correspondingDbFile))
                    {
                        File.Delete(repoFile);
                        Console.WriteLine($"{DateTime.Now} - GitBackupManager - Deleted stale file: {relativePath}");
                    }
                }

                // Delete empty directories in BackupRepo (except .git)
                var repoDirectories = Directory.GetDirectories(_repoPath, "*", SearchOption.AllDirectories)
                    .OrderByDescending(d => d.Length); // Process deepest first

                foreach (var repoDir in repoDirectories)
                {
                    // Skip .git folder
                    if (repoDir.Contains(Path.DirectorySeparatorChar + ".git"))
                        continue;

                    string relativePath = Path.GetRelativePath(_repoPath, repoDir);
                    string correspondingDbDir = Path.Combine(_databasesFolderPath, relativePath);

                    // Delete if directory doesn't exist in Databases OR is empty
                    if (!Directory.Exists(correspondingDbDir) || 
                        (!Directory.GetFiles(repoDir).Any() && !Directory.GetDirectories(repoDir).Any()))
                    {
                        Directory.Delete(repoDir, recursive: false);
                        Console.WriteLine($"{DateTime.Now} - GitBackupManager - Deleted stale folder: {relativePath}");
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

        public void CopyAndBackupFilesToGit()
        {
            try
            {
                if (!_configManager.IsGitPatTokenSet())
                {
                    Console.WriteLine($"{DateTime.Now} - GitBackupManager - Git PAT Token not set. Git Backup Storage not enabled.");
                    return;
                }

                CopyJsonFilesToBackupRepo();
                BackupFiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error during CopyAndBackupFilesToGit: {ex.Message}");
            }
        }
    }
}
