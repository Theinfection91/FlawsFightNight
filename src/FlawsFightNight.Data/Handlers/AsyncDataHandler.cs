using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlawsFightNight.Data.Interfaces;

namespace FlawsFightNight.Data.Handlers
{
    public enum PathOption
    {
        Databases,
        Tournaments,
        iCTFStatLogs,
        TAMStatLogs,
        iBRStatLogs,
        UT2004PlayerProfiles,
        Credentials,
        UserProfiles
    }

    public abstract class AsyncDataHandler<T> : IAsyncInitializable where T : new()
    {
        protected string _folderPath;
        protected string _filePath;
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        private bool _hasPendingPath = false;
        private bool _hasPendingCustomPath = false;
        private PathOption? _pendingPathOption;
        private string? _pendingFileName;
        private string? _pendingCustomFolderName;

        private static readonly JsonSerializerSettings _safeJsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            SerializationBinder = new SafeSerializationBinder(),
            Formatting = Formatting.Indented
        };

        protected AsyncDataHandler() { }

        protected AsyncDataHandler(PathOption pathOption, string fileName)
        {
            _hasPendingPath = true;
            _pendingPathOption = pathOption;
            _pendingFileName = fileName;
        }

        protected AsyncDataHandler(string fileName, string folderName)
        {
            _hasPendingCustomPath = true;
            _pendingFileName = fileName;
            _pendingCustomFolderName = folderName;
        }

        public async Task InitializePendingPathAsync()
        {
            if (_hasPendingPath && _pendingPathOption.HasValue && !string.IsNullOrEmpty(_pendingFileName))
            {
                await SetFilePath(_pendingPathOption.Value, _pendingFileName);
                _hasPendingPath = false;
                _pendingPathOption = null;
                _pendingFileName = null;
            }

            if (_hasPendingCustomPath && !string.IsNullOrEmpty(_pendingFileName) && !string.IsNullOrEmpty(_pendingCustomFolderName))
            {
                await SetFilePathCustom(_pendingFileName, _pendingCustomFolderName);
                _hasPendingCustomPath = false;
                _pendingFileName = null;
                _pendingCustomFolderName = null;
            }
        }

        private async Task InitializeFile()
        {
            if (!File.Exists(_filePath))
                await Save(new T());
        }

        public async Task SetFilePath(PathOption pathOptions, string fileName = "missingName.json")
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            switch (pathOptions)
            {
                case PathOption.Databases:
                    _folderPath = Path.Combine(baseDir, "Databases");
                    break;
                case PathOption.Tournaments:
                    _folderPath = Path.Combine(baseDir, "Databases", "Tournaments");
                    break;
                case PathOption.iCTFStatLogs:
                    _folderPath = Path.Combine(baseDir, "Databases", "StatLogs", "iCTF");
                    break;
                case PathOption.TAMStatLogs:
                    _folderPath = Path.Combine(baseDir, "Databases", "StatLogs", "TAM");
                    break;
                case PathOption.iBRStatLogs:
                    _folderPath = Path.Combine(baseDir, "Databases", "StatLogs", "iBR");
                    break;
                case PathOption.UT2004PlayerProfiles:
                    _folderPath = Path.Combine(baseDir, "Databases", "UT2004PlayerProfiles");
                    break;
                case PathOption.Credentials:
                    _folderPath = Path.Combine(baseDir, "Credentials");
                    break;
                case PathOption.UserProfiles:
                    _folderPath = Path.Combine(baseDir, "Databases", "UserProfiles");
                    break;
            }

            if (!Directory.Exists(_folderPath))
                Directory.CreateDirectory(_folderPath);

            _filePath = Path.Combine(_folderPath, fileName);
            await InitializeFile();
        }

        public async Task SetFilePathCustom(string fileName, string folderName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _folderPath = Path.Combine(baseDir, folderName);

            if (!Directory.Exists(_folderPath))
                Directory.CreateDirectory(_folderPath);

            _filePath = Path.Combine(_folderPath, fileName);
            await InitializeFile();
        }

        public async Task DeleteFolderAndContents(string tournamentId)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var folderPath = Path.Combine(baseDir, "Databases", "Tournaments", tournamentId);

            if (Directory.Exists(folderPath))
            {
                await Task.Run(() => Directory.Delete(folderPath, true));
            }
        }

        public async Task<T> Load()
        {
            await _fileLock.WaitAsync();
            try
            {
                return await LoadWithRetryAsync();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task Save(T data)
        {
            await _fileLock.WaitAsync();
            try
            {
                await SaveWithRetryAsync(data);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task<T> LoadWithRetryAsync(int maxRetries = 5, int delayMs = 200)
        {
            Exception lastException = null;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_filePath);
                    return JsonConvert.DeserializeObject<T>(json, _safeJsonSettings) ?? new T();
                }
                catch (IOException ex) when (i < maxRetries - 1)
                {
                    lastException = ex;
                    await Task.Delay(delayMs * (i + 1));
                }
            }

            throw new IOException($"Failed to read file '{_filePath}' after {maxRetries} attempts.", lastException);
        }

        private async Task SaveWithRetryAsync(T data, int maxRetries = 5, int delayMs = 200)
        {
            Exception lastException = null;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(data, _safeJsonSettings);

                    // Write to temp file first, then move
                    string tempFile = _filePath + ".tmp";
                    await File.WriteAllTextAsync(tempFile, json);
                    File.Move(tempFile, _filePath, true);

                    return; // Success!
                }
                catch (IOException ex) when (i < maxRetries - 1)
                {
                    lastException = ex;
                    await Task.Delay(delayMs * (i + 1));
                }
                catch (UnauthorizedAccessException ex) when (i < maxRetries - 1)
                {
                    lastException = ex;
                    await Task.Delay(delayMs * (i + 1));
                }
            }

            throw new IOException($"Failed to save file '{_filePath}' after {maxRetries} attempts.", lastException);
        }

        public virtual async Task<List<T>> LoadAll(string searchPattern = "tournament.json", string folderName = null)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var folderPath = Path.Combine(baseDir, "Databases", folderName ?? string.Empty);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var files = Directory.GetFiles(folderPath, searchPattern, SearchOption.AllDirectories);
            var list = new List<T>();

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var data = JsonConvert.DeserializeObject<T>(json, _safeJsonSettings);

                    if (data != null)
                        list.Add(data);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Warning: Could not read file {file}: {ex.Message}");
                }
            }

            return list;
        }

        public async Task DeleteJsonFilesInFolder(PathOption pathOption)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string folderPath = pathOption switch
            {
                PathOption.Databases => Path.Combine(baseDir, "Databases"),
                PathOption.Tournaments => Path.Combine(baseDir, "Databases", "Tournaments"),
                PathOption.iCTFStatLogs => Path.Combine(baseDir, "Databases", "StatLogs", "iCTF"),
                PathOption.TAMStatLogs => Path.Combine(baseDir, "Databases", "StatLogs", "TAM"),
                PathOption.iBRStatLogs => Path.Combine(baseDir, "Databases", "StatLogs", "iBR"),
                PathOption.UT2004PlayerProfiles => Path.Combine(baseDir, "Databases", "UT2004PlayerProfiles"),
                PathOption.UserProfiles => Path.Combine(baseDir, "Databases", "UserProfiles"),
                _ => throw new ArgumentException("Invalid path option")
            };
            if (Directory.Exists(folderPath))
            {
                var files = Directory.GetFiles(folderPath, "*.json*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Warning: Could not delete file {file}: {ex.Message}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Console.WriteLine($"Warning: Could not delete file {file} due to access issues: {ex.Message}");
                    }
                }
            }
        }
    }
}
