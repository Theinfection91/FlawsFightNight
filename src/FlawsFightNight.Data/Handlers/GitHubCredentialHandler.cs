using FlawsFightNight.Data.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Handlers
{
    public class GitHubCredentialHandler : BaseDataHandler<GitHubCredentialFile>
    {
        public GitHubCredentialHandler() : base("github_credentials.json", "Credentials")
        {

        }
    }
}
