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
        public static ErrorReason GenerateSpecific(string reason)
        {
            return new ErrorReason(reason);
        }

        public static ErrorReason GenerateIsRunningError()
        {
            return new ErrorReason("Tournament is currently running.");
        }

        public static ErrorReason GenerateIsNotRunningError()
        {
            return new ErrorReason("Tournament is not currently running.");
        }

        public static ErrorReason GenerateTeamsNotLockedError()
        {
            return new ErrorReason("Teams are not locked.");
        }

        public static ErrorReason GenerateTeamsAlreadyLockedError()
        {
            return new ErrorReason("Teams are already locked.");
        }

        public static ErrorReason GenerateTeamsAlreadyUnlockedError()
        {
            return new ErrorReason("Teams are already unlocked.");
        }

        public static ErrorReason GenerateInsufficientTeamsError()
        {
            return new ErrorReason($"At least 3 teams are required.");
        }
    }
}
