using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Handlers
{
    public enum PathOption
    {
        Databases,
        TournamentSystem,
        CTFStatLogs,
        ReTAMStatLogs,
        DMStatLogs,
        BRStatLogs,
        UT2004PlayerProfiles,
    }

    public abstract class AsyncDataHandler<T> where T : new()
    {
        protected string _folderPath;
        protected string _filePath;
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        protected AsyncDataHandler()
        {
        }

        // Constructor with PathOption
        protected AsyncDataHandler(PathOption pathOption, string fileName)
        {
            SetFilePath(pathOption, fileName).Wait();
        }

        // Constructor with custom folder
        protected AsyncDataHandler(string fileName, string folderName)
        {
            SetFilePathCustom(fileName, folderName).Wait();
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
                case PathOption.TournamentSystem:
                    _folderPath = Path.Combine(baseDir, "Databases", "TournamentSystem");
                    break;
                case PathOption.CTFStatLogs:
                    _folderPath = Path.Combine(baseDir, "Databases", "StatLogs", "CTF");
                    break;
                case PathOption.ReTAMStatLogs:
                    _folderPath = Path.Combine(baseDir, "Databases", "StatLogs", "ReTAM");
                    break;
                case PathOption.DMStatLogs:
                    _folderPath = Path.Combine(baseDir, "Databases", "StatLogs", "DeathMatch");
                    break;
                case PathOption.BRStatLogs:
                    _folderPath = Path.Combine(baseDir, "Databases", "StatLogs", "BombingRun");
                    break;
                case PathOption.UT2004PlayerProfiles:
                    _folderPath = Path.Combine(baseDir, "Databases", "UT2004PlayerProfiles");
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
            var folderPath = Path.Combine(baseDir, "Databases", tournamentId);

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
                    return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    }) ?? new T();
                }
                catch (IOException ex) when (i < maxRetries - 1)
                {
                    lastException = ex;
                    await Task.Delay(delayMs * (i + 1)); // Exponential backoff: 200ms, 400ms, 600ms, 800ms, 1000ms
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
                    var json = JsonConvert.SerializeObject(data, Formatting.Indented,
                        new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.Auto
                        });

                    // Write to temp file first, then move (atomic operation)
                    string tempFile = _filePath + ".tmp";
                    await File.WriteAllTextAsync(tempFile, json);
                    File.Move(tempFile, _filePath, true);

                    return; // Success!
                }
                catch (IOException ex) when (i < maxRetries - 1)
                {
                    lastException = ex;
                    await Task.Delay(delayMs * (i + 1)); // Exponential backoff
                }
                catch (UnauthorizedAccessException ex) when (i < maxRetries - 1)
                {
                    lastException = ex;
                    await Task.Delay(delayMs * (i + 1)); // Exponential backoff
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
                    var data = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    });

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
    }
}
