using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class TeamManager : BaseDataDriven
    {
        public TeamManager(DataManager dataManager) : base("TeamManager", dataManager)
        {

        }
        
        public bool IsTeamNameUnique(string teamName)
        {
            List<Tournament> tournaments = _dataManager.TournamentsDatabaseFile.Tournaments;
            foreach (Tournament tournament in tournaments)
            {
                if (tournament.Teams.Any(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase)))
                {
                    return false; // Team name already exists in the tournament
                }
            }
            return true; // Team name is unique across all tournaments
        }

        public bool DoesTeamExist(string teamName)
        {
            List<Tournament> tournaments = _dataManager.TournamentsDatabaseFile.Tournaments;
            foreach (Tournament tournament in tournaments)
            {
                if (tournament.Teams.Any(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsDiscordIdOnTeam(Team team, ulong discordId)
        {
            return team.Members.Any(m => m.DiscordId == discordId);
        }

        public Team GetTeamByName(string teamName)
        {
            List<Tournament> tournaments = _dataManager.TournamentsDatabaseFile.Tournaments;
            foreach (Tournament tournament in tournaments)
            {
                Team? team = tournament.Teams.FirstOrDefault(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase));
                if (team != null)
                {
                    return team; // Return the first matching team found
                }
            }
            return null; // No team found with the given name
        }

        public Team? GetTeamByName(Tournament tournament, string teamName)
        {
            return tournament.Teams
                .FirstOrDefault(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase));
        }

        public Team CreateTeam(string teamName, string tournamentId, int teamSize, string tournamentSizeFormat, List<Member> members, int rank)
        {
            return new Team()
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
