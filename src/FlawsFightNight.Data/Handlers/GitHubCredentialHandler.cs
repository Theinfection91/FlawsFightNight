using FlawsFightNight.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Data.Handlers
{
    public class GitHubCredentialHandler : AsyncDataHandler<GitHubCredentialFile>
    {
        public GitHubCredentialHandler() : base(PathOption.Credentials, "github_credentials.json")
        {

        }
    }
}
