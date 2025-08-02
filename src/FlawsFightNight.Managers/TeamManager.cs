using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class TeamManager
    {
        public TeamManager() { }

        public bool IsTeamNameUnique(string teamName, List<Team> teams)
        {
            if (teams.Count == 0) return true; // If no teams exist, the name is unique

            else if (teams.Any(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase)))
            {
                return false; // Team name already exists
            }
            else
            {
                return true; // Team name is unique
            }
        }

        public Team CreateTeam(string teamName, string tournamentId, int teamSize, string tournamentSizeFormat, List<Member> members, int rank)
        {
            return new Team
            {
                Name = teamName,
                TournamentId = tournamentId,
                Size = teamSize,
                TeamSizeFormat = tournamentSizeFormat,
                Members = members,
                Rank = rank
            };
        }
    }
}
