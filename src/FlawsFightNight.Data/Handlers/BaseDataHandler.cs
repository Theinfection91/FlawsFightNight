using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Handlers
{
    public abstract class BaseDataHandler<T> where T : new()
    {
        private readonly string _filePath;

        private static readonly JsonSerializerSettings _safeJsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            SerializationBinder = new SafeSerializationBinder(),
            Formatting = Formatting.Indented
        };

        protected BaseDataHandler(string fileName, string folderName)
        {
            _filePath = SetFilePath(fileName, folderName);
            InitializeFile();
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

        private void InitializeFile()
        {
            if (!File.Exists(_filePath))
            {
                Save(new T());
            }
        }


        public T Load()
        {
            var json = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<T>(json, _safeJsonSettings) ?? new T();
        }

        public void Save(T data)
        {
            var json = JsonConvert.SerializeObject(data, _safeJsonSettings);
            File.WriteAllText(_filePath, json);
        }
    }
}
