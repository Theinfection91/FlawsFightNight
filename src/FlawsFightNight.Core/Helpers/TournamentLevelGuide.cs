using FlawsFightNight.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Core.Helpers
{
    public static class TournamentLevelGuide
    {
        public static int GetExperienceForAction(TournamentExperienceAction action)
        {
            return action switch
            {
                TournamentExperienceAction.WinMatch => 20,
                TournamentExperienceAction.LoseMatch => 10,
                TournamentExperienceAction.ParticipateTournament => 25,
                TournamentExperienceAction.CompleteTournament => 50,
                TournamentExperienceAction.FirstPlaceTournament => 75,
                TournamentExperienceAction.SecondPlaceTournament => 50,
                TournamentExperienceAction.ThirdPlaceTournament => 25,
                _ => throw new ArgumentOutOfRangeException(nameof(action), $"Unhandled action: {action}")
            };
        }
    }
}
