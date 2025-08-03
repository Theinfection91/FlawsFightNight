using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class MatchManager
    {
        public MatchManager()
        {

        }

        public void BuildMatchScheduleResolver(Tournament tournament)
        {
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    // Ladder tournaments do not have a match schedule resolver
                    break;

                case TournamentType.RoundRobin:
                    BuildRoundRobinMatchSchedule(tournament);
                    break;

                default:
                    Console.WriteLine($"Match schedule resolver not implemented for tournament type: {tournament.Type}");
                    break;
            }
        }

        //public void BuildRoundRobinMatchSchedule(Tournament tournament)
        //{
        //    ClearMatchSchedule(tournament);
        //    // Round Robin match schedule logic goes here
        //    // This will involve creating matches for each team against every other team

        //    // Check if the number of teams is odd or even
        //    bool isOddTeamAmount = tournament.Teams.Count % 2 != 0;

        //    foreach (var teamOne in tournament.Teams)
        //    {
        //        foreach (var teamTwo in tournament.Teams)
        //        {
        //            // Skip if it's the same team or if the match has already been created
        //            if (teamOne == teamTwo || tournament.MatchSchedule.MatchesToPlay.Any(m => (m.Value.TeamA.Equals(teamOne.Name, StringComparison.OrdinalIgnoreCase) && m.Value.TeamB.Equals(teamTwo.Name, StringComparison.OrdinalIgnoreCase) || (m.Value.TeamA.Equals(teamTwo.Name, StringComparison.OrdinalIgnoreCase) && m.Value.TeamB.Equals(teamOne.Name, StringComparison.OrdinalIgnoreCase)))))
        //            {
        //                continue;
        //            }
        //            // Create a match
        //            Match match = new Match(teamOne.Name, teamTwo.Name);
        //            // If the number of teams is odd, create a bye match for the last team
        //            if (isOddTeamAmount && tournament.Teams.Last() == teamOne)
        //            {
        //                match.IsByeMatch = true;
        //                match.TeamB = null; // No opponent for bye matches
        //            }
        //            // Add the match to the schedule
        //            tournament.MatchSchedule.MatchesToPlay.Add(tournament.MatchSchedule.MatchesToPlay.Count + 1, match);
        //        }
        //    }
        //}

        public void BuildRoundRobinMatchSchedule(Tournament tournament)
        {
            ClearMatchSchedule(tournament);

            var teams = new List<string>(tournament.Teams.Select(t => t.Name));
            bool hasBye = false;
            string byePlaceholder = "__BYE__";
            if (teams.Count % 2 != 0)
            {
                hasBye = true;
                teams.Add(byePlaceholder);
            }

            int numRounds = teams.Count - 1;
            int half = teams.Count / 2;
            var rotating = new List<string>(teams); // Will rotate except index 0 fixed

            int key = 1;
            for (int round = 0; round < numRounds; round++)
            {
                // Pairings for this round
                for (int i = 0; i < half; i++)
                {
                    string a = rotating[i];
                    string b = rotating[teams.Count - 1 - i];

                    // Skip bye vs bye or record bye matches if desired
                    bool isByeMatch = false;
                    if (hasBye && (a == byePlaceholder || b == byePlaceholder))
                    {
                        // if both are bye (shouldn't happen) or one is bye: treat as bye match
                        if (a == byePlaceholder && b == byePlaceholder)
                            continue;
                        isByeMatch = true;
                    }

                    // Don't create real match for placeholder vs nothing if you want to skip
                    var match = new Match(a == byePlaceholder ? null : a, b == byePlaceholder ? null : b)
                    {
                        IsByeMatch = isByeMatch,
                        CreatedOn = DateTime.UtcNow
                    };

                    tournament.MatchSchedule.MatchesToPlay.Add(key++, match);
                }

                // Rotate (keep first element fixed)
                var last = rotating[^1];
                rotating.RemoveAt(rotating.Count - 1);
                rotating.Insert(1, last);
            }
        }


        public void ClearMatchSchedule(Tournament tournament)
        {
            // Clear the match schedule for the tournament
            tournament.MatchSchedule.MatchesToPlay.Clear();
        }
    }
}
