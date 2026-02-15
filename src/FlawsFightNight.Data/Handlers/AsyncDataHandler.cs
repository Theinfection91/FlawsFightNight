using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Handlers
{
    public abstract class AsyncDataHandler<T> where T : new()
    {
        private readonly string _filePath;

        protected AsyncDataHandler(string fileName, string folderName)
        {
            _filePath = SetFilePath(fileName, folderName);
            InitializeFile().Wait();
        }

        private string SetFilePath(string fileName, string folderName)
        {
            string appBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(appBaseDirectory, folderName, fileName);

            // Ensure the directory exists
            string? directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Console.WriteLine($"Directory created: {directory}");
            }

            return filePath;
        }

        private async Task InitializeFile()
        {
            if (!File.Exists(_filePath))
            {
                await Save(new T());
            }
        }


        public async Task<T> Load()
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            }) ?? new T();
        }

        public async Task Save(T data)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            await File.WriteAllTextAsync(_filePath, json);
        }
    }
}
}
