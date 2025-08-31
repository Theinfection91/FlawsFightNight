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

        private ConfigManager _configManager;
        public GitBackupManager(ConfigManager configManager, DataManager dataManager) : base("Git Backup Manager", dataManager)
        {
            _configManager = configManager;

            // Grab info from GitHub Credential File
            _remoteUrl = _dataManager.GitHubCredentialFile.GitUrlPath;
            _token = _dataManager.GitHubCredentialFile.GitPatToken;

            // Set up repo path
            _repoPath = SetRepoFilePath();

            _databasesFolderPath = SetDatabasesFolder();

            if (_configManager.IsGitPatTokenSet())
            {
                InitializeRepository();
            }
            else
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Git PAT Token not set. Can not initialize the repo in 'BackupRepo'.");
            }
        }

        private string SetDatabasesFolder()
        {
            string appBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            return Path.Combine(appBaseDirectory, "Databases");
        }

        private string SetRepoFilePath()
        {
            // Set the file path relative to the base directory of the executable
            string appBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Construct a path in a "BackupRepo" folder within the base directory
            string repoPath = Path.Combine(appBaseDirectory, "BackupRepo");

            // Ensure the directory exists
            if (!Directory.Exists(repoPath))
            {
                Directory.CreateDirectory(repoPath);
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Directory created: {_repoPath}");
            }

            return repoPath;
        }

        private void InitializeRepository()
        {
            if (!Repository.IsValid(_repoPath))
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - No local repo found. Cloning repository...");
                try
                {
                    var options = new CloneOptions
                    {
                        FetchOptions =
                        {
                            CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
                            {
                                Username = "Ladderbot4",
                                Password = _token
                            }
                        }
                    };
                    Repository.Clone(_remoteUrl, _repoPath, options);
                    Console.WriteLine($"{DateTime.Now} - GitBackupManager - Repository cloned successfully.");

                    // Ask if user wants to use this data copied to database
                    Console.WriteLine($"{DateTime.Now} - GitBackupManager - Do you want to use the newly cloned backup data from the repository as your Database? Yes is typically the answer here. NOTE - This will overwrite data currently present in your JSON files in 'Database'. This can not be reversed. \n\nHINT: If the files in your backup repo online is more up to date than your local files in the 'Database' folder then input Y, if your JSON files in the 'Database' folder is more up to date than the files in your backup repo online, then input N");
                    bool isQuestionProcessComplete = false;
                    while (!isQuestionProcessComplete)
                    {
                        Console.WriteLine($"Enter Y or N\n");
                        string? userInput = Console.ReadLine();
                        switch (userInput.ToLower().Trim())
                        {
                            case "y":
                                // Copy newly cloned files from BackupRepo to Databases
                                Console.WriteLine($"{DateTime.Now} GitBackupManager - Copying files from 'BackupRepo' folder to 'Databases' folder.");
                                CopyFilesFromBackupRepoToDatabases();
                                isQuestionProcessComplete = true;
                                break;

                            case "n":
                                Console.WriteLine($"{DateTime.Now} GitBackupManager - Files were not copied from 'BackupRepo' to 'Databases' folder. The next event driven data change may cause unwanted data changes and loss. Use this not copying the cloned repo option at your own expense and only if you know what you are doing.");
                                isQuestionProcessComplete = true;
                                break;

                            default:
                                Console.WriteLine($"{DateTime.Now} GitBackupManager - Invalid input was given. Please reply with Y or N. You entered: {userInput}");
                                break;
                        }
                    }
                }
                catch (LibGit2SharpException ex)
                {
                    Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error cloning repository: {ex.Message}");
                }
            }
        }

        public void CopyJsonFilesToBackupRepo()
        {
            try
            {
                if (Directory.Exists(_databasesFolderPath))
                {
                    // Get files from the Databases folder
                    var jsonFiles = Directory.GetFiles(_databasesFolderPath, "*.json", SearchOption.TopDirectoryOnly);

                    foreach (var jsonFile in jsonFiles)
                    {
                        // Set destination to be BackupRepo Folder
                        string fileName = Path.GetFileName(jsonFile);
                        string destinationPath = Path.Combine(_repoPath, fileName);

                        try
                        {
                            // Copy the file even if it's in use
                            using (FileStream sourceStream = new FileStream(jsonFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (FileStream destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                            {
                                sourceStream.CopyTo(destinationStream);
                            }
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error copying file {jsonFile}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error during backup process: {ex.Message}");
            }
        }

        public void CopyFilesFromBackupRepoToDatabases()
        {
            try
            {
                if (Directory.Exists(_repoPath))
                {
                    // Get files from the BackupRepo folder
                    var jsonFiles = Directory.GetFiles(_repoPath, "*.json", SearchOption.TopDirectoryOnly);

                    foreach (var jsonFile in jsonFiles)
                    {
                        // Set destination to be Databases Folder
                        string fileName = Path.GetFileName(jsonFile);
                        string destinationPath = Path.Combine(_databasesFolderPath, fileName);

                        try
                        {
                            // Copy the file even if it's in use
                            using (FileStream sourceStream = new FileStream(jsonFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (FileStream destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                            {
                                sourceStream.CopyTo(destinationStream);
                            }
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error copying file {jsonFile}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error while copying files from BackupRepo to Databases: {ex.Message}");
            }
        }

        public void BackupFiles()
        {
            using (var repo = new Repository(_repoPath))
            {
                // Get all files in the repository directory
                var files = Directory.GetFiles(_repoPath, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    // Skip Git metadata files
                    if (file.Contains(".git")) continue;

                    // Stage the file
                    Commands.Stage(repo, file);
                }

                // Check if there are any changes to commit
                if (repo.RetrieveStatus().IsDirty)
                {
                    // Create a commit with the current timestamp
                    Signature author = new Signature("FlawsFightNight", "Mr.Flaw@bot.com", DateTimeOffset.Now);
                    Commit commit = repo.Commit($"Backup: Update data files ({DateTime.Now})", author, author);

                    // Push changes to the remote repository
                    var options = new PushOptions
                    {
                        CredentialsProvider = (_, _, _) => new UsernamePasswordCredentials
                        {
                            Username = "Ladderbot4",
                            Password = _token
                        }
                    };

                    try
                    {
                        repo.Network.Push(repo.Branches["main"], options);
                        Console.WriteLine($"{DateTime.Now} - GitBackupManager - Backup pushed successfully.");
                    }
                    catch (LibGit2SharpException ex)
                    {
                        Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error during push: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now} - GitBackupManager - No changes detected; nothing to backup.");
                }
            }
        }

        public void CopyAndBackupFilesToGit()
        {
            if (_configManager.IsGitPatTokenSet())
            {
                CopyJsonFilesToBackupRepo();
                BackupFiles();
            }
            else
            {
                Console.WriteLine($"{DateTime.Now} GitBackupManager - Git PAT Token not set. Git Backup Storage not enabled.");
            }
        }

        public void ForceBackupFiles()
        {
            using (var repo = new Repository(_repoPath))
            {
                // Get all files in the repository directory
                var files = Directory.GetFiles(_repoPath, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    // Skip Git metadata files
                    if (file.Contains(".git")) continue;

                    // Stage the file
                    Commands.Stage(repo, file);
                }

                // Create a commit with the current timestamp
                Signature author = new Signature("FlawsFightNight", "Mr.Flaw@bot.com", DateTimeOffset.Now);
                try
                {
                    Commit commit = repo.Commit(
                        $"Backup: Commit at {DateTime.Now}",
                        author,
                        author,
                        new CommitOptions { AllowEmptyCommit = true } // Force empty commits
                    );

                    // Push changes to the remote repository
                    var options = new PushOptions
                    {
                        CredentialsProvider = (_, _, _) => new UsernamePasswordCredentials
                        {
                            Username = "FlawsFightNight",
                            Password = _token
                        }
                    };
                    repo.Network.Push(repo.Branches["main"], options);
                    Console.WriteLine($"{DateTime.Now} GitBackupManager - Automated Backup pushed successfully.");
                }
                catch (EmptyCommitException)
                {
                    Console.WriteLine($"{DateTime.Now} - GitBackupManager - No changes; empty commit was skipped.");
                }
                catch (LibGit2SharpException ex)
                {
                    Console.WriteLine($"{DateTime.Now} - GitBackupManager - Error during push: {ex.Message}");
                }
            }
        }
    }
}
