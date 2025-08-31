using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class GitBackupManager : BaseDataDriven
    {
        private readonly string _repoPath;
        private readonly string _remoteUrl;
        private readonly string _token;
        private readonly string _databasesFolderPath;
        private readonly string _configFolderPath;
        public GitBackupManager(DataManager dataManager) : base("Git Backup Manager", dataManager)
        {
            // Grab info from GitHub Credential File
        }
    }
}
