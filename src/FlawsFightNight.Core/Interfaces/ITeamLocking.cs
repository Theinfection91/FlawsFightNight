using FlawsFightNight.Core.Models;
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
        bool CanLockTeams(out ErrorReason errorReason);
        bool CanUnlockTeams(out ErrorReason errorReason);
        void LockTeams();
        void UnlockTeams();
    }
}
