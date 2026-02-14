using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class UT2004StatsManager : BaseDataDriven
    {
        public UT2004StatsManager(DataManager dataManager) : base("UT2004StatsManager", dataManager)
        {

        }

        public bool IsLogFileProcessed(string fileName)
        {
            var processedLogs = _dataManager.GetProcessedLogNames();
            foreach (var logName in processedLogs.ProcessedLogFileNames)
            {
                if (logName == fileName)
                {
                    return true;
                }
            }
            return false;
        }

        public void AddProcessedLogFileName(string fileName)
        {
            var processedLogs = _dataManager.GetProcessedLogNames();
            if (!processedLogs.ProcessedLogFileNames.Contains(fileName))
            {
                processedLogs.ProcessedLogFileNames.Add(fileName);
                _dataManager.SaveAndReloadProcessedLogNamesFile();
            }
        }
    }
}
    