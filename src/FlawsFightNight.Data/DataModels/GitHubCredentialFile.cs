using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.DataModels
{
    public class GitHubCredentialFile
    {
        public string GitPatToken { get; set; } = "ENTER_GIT_PAT_TOKEN_HERE";
        public string GitUrlPath { get; set; } = "https://github.com/YourUsername/YourGitStorageRepo.git";

        public GitHubCredentialFile() { }
    }
}
