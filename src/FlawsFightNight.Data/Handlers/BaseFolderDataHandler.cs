using Newtonsoft.Json;
using System;
using System.IO;

namespace FlawsFightNight.Data.Handlers
{
    public abstract class BaseFolderDataHandler<T> where T : new()
    {
        protected string _folderPath;
        protected string _filePath;

        //protected BaseFolderDataHandler(string folderName, string fileName = "tournament.json")
        //{
        //    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        //    _folderPath = Path.Combine(baseDir, "Databases", folderName);

        //    if (!Directory.Exists(_folderPath))
        //        Directory.CreateDirectory(_folderPath);

        //    _filePath = Path.Combine(_folderPath, fileName);

        //    InitializeFile();
        //}

        protected BaseFolderDataHandler()
        {
            
        }

        private void InitializeFile()
        {
            if (!File.Exists(_filePath))
                Save(new T());
        }

        public void SetFilePath(string folderName, string fileName = "tournament.json")
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _folderPath = Path.Combine(baseDir, "Databases", folderName);
            if (!Directory.Exists(_folderPath))
                Directory.CreateDirectory(_folderPath);
            _filePath = Path.Combine(_folderPath, fileName);
            InitializeFile();
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

        public List<T> LoadAll()
        {
            // Get all JSON from each file in each folder
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var folderPath = Path.Combine(baseDir, "Databases");
            var files = Directory.GetFiles(folderPath, "tournament.json", SearchOption.AllDirectories);
            var list = new List<T>();
            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var data = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
                if (data != null)
                    list.Add(data);
            }
            return list;
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
