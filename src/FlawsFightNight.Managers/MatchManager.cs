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

        public void BuildRoundRobinMatchSchedule(Tournament tournament)
        {
            ClearMatchSchedule(tournament);
            // Round Robin match schedule logic goes here
            // This will involve creating matches for each team against every other team

            // Check if the number of teams is odd or even
            bool isOddTeamAmount = tournament.Teams.Count % 2 != 0;

            foreach (var teamOne in tournament.Teams)
            {
                foreach (var teamTwo in tournament.Teams)
                {
                    // Skip if it's the same team or if the match has already been created
                    if (teamOne == teamTwo || tournament.MatchSchedule.MatchesToPlay.Any(m => (m.Value.TeamA == teamOne && m.Value.TeamB == teamTwo) || (m.Value.TeamA == teamTwo && m.Value.TeamB == teamOne)))
                    {
                        continue;
                    }
                    // Create a match
                    Match match = new Match(teamOne, teamTwo);
                    // If the number of teams is odd, create a bye match for the last team
                    if (isOddTeamAmount && tournament.Teams.Last() == teamOne)
                    {
                        match.IsByeMatch = true;
                        match.TeamB = null; // No opponent for bye matches
                    }
                    // Add the match to the schedule
                    tournament.MatchSchedule.MatchesToPlay.Add(tournament.MatchSchedule.MatchesToPlay.Count + 1, match);
                }
            }
        }

        public void ClearMatchSchedule(Tournament tournament)
        {
            // Clear the match schedule for the tournament
            tournament.MatchSchedule.MatchesToPlay.Clear();
        }
    }
}
