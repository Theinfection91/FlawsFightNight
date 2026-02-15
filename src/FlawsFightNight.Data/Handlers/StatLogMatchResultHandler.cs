using FlawsFightNight.Data.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Handlers
{
    public class StatLogMatchResultHandler : AsyncDataHandler<StatLogMatchResultsFile>
    {
        public StatLogMatchResultHandler() : base()
        {

        }

        // Override to always load all JSON files from StatLogs folder
        public new async Task<List<StatLogMatchResultsFile>> LoadAll(string searchPattern = "*.json")
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var folderPath = Path.Combine(baseDir, "Databases", "StatLogs");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                return new List<StatLogMatchResultsFile>();
            }

            // Always grab all JSON files in the StatLogs folder
            var files = Directory.GetFiles(folderPath, "*.json");
            var results = new List<StatLogMatchResultsFile>();

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var data = JsonConvert.DeserializeObject<StatLogMatchResultsFile>(json, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    });

                    if (data != null)
                        results.Add(data);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Warning: Could not read file {file}: {ex.Message}");
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Warning: Could not deserialize file {file}: {ex.Message}");
                }
            }

            return results;
        }
    }
}
