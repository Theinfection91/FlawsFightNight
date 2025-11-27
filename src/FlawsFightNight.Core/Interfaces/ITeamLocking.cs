using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Interfaces
{
    public interface ITeamLocking
    {
        bool IsTeamsLocked { get; set; }
        bool CanTeamsBeLocked { get; set; }
        bool CanTeamsBeUnlocked { get; set; }
        bool CanLockTeams();
        bool CanUnlockTeams();
        void LockTeams();
        void UnlockTeams();
    }
}
