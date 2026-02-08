using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Interfaces
{
    public interface IRoundBased
    {
        int CurrentRound { get; set; }
        int? TotalRounds { get; set; }
        bool IsRoundComplete { get; set; }
        bool IsRoundLockedIn { get; set; }
        bool CanRoundComplete();
        bool CanLockRound();
        void LockRound();
        bool CanUnlockRound();
        void UnlockRound();
        bool DoesRoundContainByeMatch();
        bool CanAdvanceRound();
        void AdvanceRound();
    }
}
