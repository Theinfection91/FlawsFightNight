using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.MatchCommands
{
    public class SendChallengeLogic : Logic
    {
        private GitBackupManager _gitBackupManager;
        public SendChallengeLogic(GitBackupManager gitBackupManager) : base("Send Challenge")
        {
            // Initialize any necessary components or services here
            _gitBackupManager = gitBackupManager;
        }
    }
}
