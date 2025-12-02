using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Helpers
{
    public static class ErrorReasonGenerator
    {
        public static ErrorReason GenerateGeneric(string reason)
        {
            return new ErrorReason(reason);
        }

        public static ErrorReason GenerateIsRunningError()
        {
            return new ErrorReason("Tournament is currently running.");
        }

        public static ErrorReason GenerateTeamsAlreadyLockedError()
        {
            return new ErrorReason("Teams are already locked.");
        }

        public static ErrorReason GenerateTeamsAlreadyUnlockedError()
        {
            return new ErrorReason("Teams are already unlocked.");
        }

        public static ErrorReason GenerateInsufficientTeamsToLockError()
        {
            return new ErrorReason($"At least 3 teams are required to lock teams.");
        }
    }
}
