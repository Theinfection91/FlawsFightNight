using Newtonsoft.Json;
using System;
using System.IO;

namespace FlawsFightNight.Data.Handlers
{
    public abstract class BaseFolderDataHandler<T> where T : new()
    {
        protected readonly string _folderPath;
        protected readonly string _filePath;

        protected BaseFolderDataHandler(string folderName, string fileName = "tournament.json")
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _folderPath = Path.Combine(baseDir, "Databases", folderName);

            if (!Directory.Exists(_folderPath))
                Directory.CreateDirectory(_folderPath);

            _filePath = Path.Combine(_folderPath, fileName);

            InitializeFile();
        }

        private void InitializeFile()
        {
            if (!File.Exists(_filePath))
                Save(new T());
        }

        public T Load()
        {
            var json = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            }) ?? new T();
        }

        public void Save(T data)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });

            File.WriteAllText(_filePath, json);
        }

        // Optional: save extra files later
        //protected void SaveOtherJson(string name, object data)
        //{
        //    var path = Path.Combine(_folderPath, name + ".json");
        //    var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        //    File.WriteAllText(path, json);
        //}
    }
}
