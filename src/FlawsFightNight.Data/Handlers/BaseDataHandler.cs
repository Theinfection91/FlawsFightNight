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
            string directory = Path.GetDirectoryName(filePath);
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


        //public T Load()
        //{
        //    Console.WriteLine($"Loading data from {_filePath}");
        //    var json = File.ReadAllText(_filePath);
        //    return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
        //    {
        //        TypeNameHandling = TypeNameHandling.Auto
        //    }) ?? new T();
        //}

        public T Load(T existing = default)
        {
            Console.WriteLine($"Loading data from {_filePath}");
            var json = File.ReadAllText(_filePath);

            if (string.IsNullOrWhiteSpace(json))
                return existing ?? new T();

            // Detect type in JSON
            var typeInJson = JsonConvert.DeserializeObject<JObject>(json)?["$type"]?.ToString();
            var existingType = existing?.GetType().FullName;

            if (existing == null || existingType != typeInJson)
            {
                // Create a new instance of the correct type
                return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                }) ?? new T();
            }
            else
            {
                // Populate the existing instance
                JsonConvert.PopulateObject(json, existing, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
                return existing;
            }
        }



        public void Save(T data)
        {
            Console.WriteLine($"Saving data to {_filePath}");
            var json = JsonConvert.SerializeObject(data, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            File.WriteAllText(_filePath, json);
        }
    }
}
