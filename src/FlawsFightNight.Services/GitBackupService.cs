using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FlawsFightNight.Services
{
    public class GitBackupService : BaseDataDriven
    {
        private readonly string _repoPath;
        private readonly string _remoteUrl;
        private readonly string _token;
        private readonly string _databasesFolderPath;
        private readonly string _gitUsername = "FlawsFightNight";

        private AdminConfigurationService _adminConfigService;
        private bool _isInitialized = false;
        private readonly CancellationTokenSource _shutdownToken = new();

        // Background queue for non-blocking backups
        private readonly Channel<bool> _backupChannel = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        private readonly Task _backupWorker;

        public GitBackupService(AdminConfigurationService adminConfigService, DataContext dataContext) : base("GitBackupService", dataContext)
        {
            _adminConfigService = adminConfigService;

            // Grab info from GitHub Credential File
            _remoteUrl = _dataContext.GitHubCredentialFile.GitUrlPath;
            _token = _dataContext.GitHubCredentialFile.GitPatToken;

            // Set up repo path
            _repoPath = SetRepoFilePath();
            _databasesFolderPath = SetDatabasesFolder();

            // Only validate repo exists, don't do interactive prompts here
            if (_adminConfigService.IsGitPatTokenSet())
            {
                ValidateRepository();
            }
            else
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Git PAT Token not set. Can not initialize the repo in 'BackupRepo'.");
            }

            // Start background worker for queued backups (long-running)
            _backupWorker = Task.Run(() => BackupWorkerAsync(_shutdownToken.Token));
        }

        public async Task RunInteractiveSetup()
        {
            if (!_adminConfigService.IsGitPatTokenSet() || _isInitialized)
                return;

            if (!Repository.IsValid(_repoPath))
            {
                await CloneAndPromptForRestore().ConfigureAwait(false);
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
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Directory created: {repoPath}");
            }

            return repoPath;
        }

        private void ValidateRepository()
        {
            try
            {
                if (Repository.IsValid(_repoPath))
                {
                    Console.WriteLine($"{DateTime.Now} - GitBackupService - Valid repository found at '{_repoPath}'.");
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Error validating repository: {ex.Message}");
            }
        }

        private async Task CloneAndPromptForRestore()
        {
            try
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupService - No local repo found. Cloning repository...");

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
                            Console.Write($"\r{DateTime.Now} - GitBackupService - {output}                    ");
                            return true;
                        },
                        OnTransferProgress = progress =>
                        {
                            Console.Write($"\r{DateTime.Now} - GitBackupService - Receiving: {progress.ReceivedObjects}/{progress.TotalObjects} ({progress.ReceivedBytes / 1024} KB)   ");
                            return true;
                        }
                    },
                    OnCheckoutProgress = (path, completedSteps, totalSteps) =>
                    {
                        if (totalSteps > 0)
                        {
                            Console.Write($"\r{DateTime.Now} - GitBackupService - Checkout: {completedSteps}/{totalSteps}   ");
                        }
                    }
                };

                // libgit2sharp is synchronous; run clone on a background thread so callers aren't blocked.
                await Task.Run(() => Repository.Clone(_remoteUrl, _repoPath, options), _shutdownToken.Token).ConfigureAwait(false);

                Console.WriteLine(); // New line after progress
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Repository cloned successfully.");

                await PromptForDataRestore().ConfigureAwait(false);
            }
            catch (LibGit2SharpException ex)
            {
                Console.WriteLine($"\n{DateTime.Now} - GitBackupService - Error cloning repository: {ex.Message}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"\n{DateTime.Now} - GitBackupService - Clone operation cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n{DateTime.Now} - GitBackupService - Error during repository initialization: {ex.Message}");
            }
        }

        private async Task PromptForDataRestore()
        {
            Console.WriteLine($"{DateTime.Now} - GitBackupService - Do you want to use the newly cloned backup data from the repository as your Database?");
            Console.WriteLine("NOTE - This will overwrite data currently present in your JSON files in 'Databases'. This cannot be reversed.");
            Console.WriteLine("\nHINT: Enter Y if your backup repo is more up-to-date, N if your local 'Databases' folder is more current.");

            while (true)
            {
                Console.WriteLine("Enter Y or N (timeout in 60 seconds):");

                var readTask = Task.Run(() => Console.ReadLine());
                var delayTask = Task.Delay(TimeSpan.FromSeconds(60), _shutdownToken.Token);

                var completed = await Task.WhenAny(readTask, delayTask).ConfigureAwait(false);

                if (completed == readTask)
                {
                    string? userInput = await readTask.ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(userInput))
                    {
                        Console.WriteLine($"{DateTime.Now} - GitBackupService - Invalid input. Please enter Y or N.");
                        continue;
                    }

                    switch (userInput.ToLower().Trim())
                    {
                        case "y":
                            Console.WriteLine($"{DateTime.Now} - GitBackupService - Copying files from 'BackupRepo' to 'Databases'...");
                            await CopyFilesFromBackupRepoToDatabases().ConfigureAwait(false);
                            await _dataContext.LoadTournamentDataFiles().ConfigureAwait(false);
                            await _dataContext.LoadPermissionsConfigFile().ConfigureAwait(false);
                            Console.WriteLine($"{DateTime.Now} - GitBackupService - Data restored successfully.");
                            return;

                        case "n":
                            Console.WriteLine($"{DateTime.Now} - GitBackupService - Skipped data restore. Local files will be used.");
                            return;

                        default:
                            Console.WriteLine($"{DateTime.Now} - GitBackupService - Invalid input: '{userInput}'. Please enter Y or N.");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now} - GitBackupService - Input timeout. Defaulting to 'N' - keeping local files.");
                    return;
                }
            }
        }

        private async Task CopyFileWithSharing(string source, string destination, CancellationToken ct = default)
        {
            try
            {
                var destDir = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(destDir))
                    Directory.CreateDirectory(destDir);

                // Link provided cancellation token with a short timeout to avoid hung file ops
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                // Use async FileStreams
                using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 81920, useAsync: true);
                using var destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);

                await sourceStream.CopyToAsync(destinationStream, 81920, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // honor cancellation silently
            }
            catch (IOException ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Error copying file {source}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Unexpected error copying {source}: {ex.Message}");
            }
        }

        public async Task CopyJsonFilesToBackupRepo()
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
                    await CopyFileWithSharing(jsonFile, destFilePath, _shutdownToken.Token).ConfigureAwait(false);
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
                        await CopyFileWithSharing(jsonFile, destFilePath, _shutdownToken.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Error during backup process: {ex.Message}");
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
                            Console.WriteLine($"{DateTime.Now} - GitBackupService - Failed to delete {relativePath}: {ex.Message}");
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
                            //Console.WriteLine($"{DateTime.Now} - GitBackupService - Deleted stale folder: {relativePath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{DateTime.Now} - GitBackupService - Failed to delete directory {relativePath}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Error during cleanup: {ex.Message}");
            }
        }

        public async Task CopyFilesFromBackupRepoToDatabases()
        {
            try
            {
                if (!Directory.Exists(_repoPath))
                    return;

                var jsonFiles = Directory.GetFiles(_repoPath, "*.json", SearchOption.AllDirectories);

                foreach (var jsonFile in jsonFiles)
                {
                    if (_shutdownToken.Token.IsCancellationRequested)
                        return;

                    // Skip .git folder files
                    if (jsonFile.Contains(Path.Combine(_repoPath, ".git")))
                        continue;

                    string relativePath = Path.GetRelativePath(_repoPath, jsonFile);
                    string destinationPath = Path.Combine(_databasesFolderPath, relativePath);

                    var destDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destDir))
                        Directory.CreateDirectory(destDir);

                    await CopyFileWithSharing(jsonFile, destinationPath, _shutdownToken.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Error copying files from BackupRepo to Databases: {ex.Message}");
            }
        }

        public async Task BackupFiles()
        {
            try
            {
                if (_shutdownToken.Token.IsCancellationRequested)
                    return;

                // LibGit2Sharp is synchronous — run its operations on a background thread to avoid blocking callers.
                await Task.Run(() =>
                {
                    if (_shutdownToken.Token.IsCancellationRequested)
                        return;

                    using var repo = new Repository(_repoPath);

                    // Stage ALL changes including deletions
                    Commands.Stage(repo, "*");

                    if (!repo.RetrieveStatus().IsDirty)
                    {
                        Console.WriteLine($"{DateTime.Now} - GitBackupService - No changes detected; nothing to backup.");
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
                    Console.WriteLine($"{DateTime.Now} - GitBackupService - Backup pushed successfully.");
                }, _shutdownToken.Token).ConfigureAwait(false);
            }
            catch (LibGit2SharpException ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Error during push: {ex.Message}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Backup operation cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Error during backup: {ex.Message}");
            }
        }

        /// <summary>
        /// Queue a backup request to the background worker and return immediately.
        /// Use this from command handlers to avoid blocking the command.
        /// </summary>
        public void EnqueueBackup()
        {
            if (!_adminConfigService.IsGitPatTokenSet())
            {
                // no-op if not configured
                return;
            }

            // best-effort write; if fails, drop the request (worker will still pick up any pending)
            _backupChannel.Writer.TryWrite(true);
        }

        /// <summary>
        /// Existing async entrypoint if a caller absolutely requires awaiting completion.
        /// </summary>
        public async Task CopyAndBackupFilesToGit()
        {
            // Keep backwards-compatible signature; this is the blocking/awaitable path.
            await CopyAndBackupFilesToGitInternal(_shutdownToken.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Internal implementation performing the actual copy & push.
        /// </summary>
        private async Task CopyAndBackupFilesToGitInternal(CancellationToken ct)
        {
            try
            {
                if (!_adminConfigService.IsGitPatTokenSet())
                {
                    Console.WriteLine($"{DateTime.Now} - GitBackupService - Git PAT Token not set. Git Backup Storage not enabled.");
                    return;
                }

                if (ct.IsCancellationRequested)
                    return;

                await CopyJsonFilesToBackupRepo().ConfigureAwait(false);

                if (ct.IsCancellationRequested)
                    return;

                await BackupFiles().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Backup operation cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Error during CopyAndBackupFilesToGit: {ex.Message}");
            }
        }

        /// <summary>
        /// Background worker that processes queued backup requests serially.
        /// Ensures backups run in the background and don't block callers (e.g., Discord commands).
        /// </summary>
        private async Task BackupWorkerAsync(CancellationToken ct)
        {
            try
            {
                await foreach (var _ in _backupChannel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    if (ct.IsCancellationRequested)
                        break;

                    try
                    {
                        // Run the actual work and observe cancellation token
                        await CopyAndBackupFilesToGitInternal(ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{DateTime.Now} - GitBackupService - Error in background backup worker: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) { /* expected on shutdown */ }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} - GitBackupService - Backup worker terminated unexpectedly: {ex.Message}");
            }
        }

        public void Shutdown()
        {
            Console.WriteLine($"{DateTime.Now} - GitBackupService - Shutting down...");
            _shutdownToken.Cancel();

            try
            {
                _backupChannel.Writer.TryComplete();
            }
            catch { /* ignore */ }
        }
    }
}
