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

        #region Bools
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

        public bool IsDiscordIdOnTeam(Team team, ulong discordId)
        {
            return team.Members.Any(m => m.DiscordId == discordId);
        }
        #endregion

        #region Gets
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
        #endregion

        #region Wins/Losses and Streaks
        public void RecordTeamWin(Team team, int points = 0)
        {
            team.Wins++;
            team.WinStreak++;
            team.LoseStreak = 0; // Reset loss streak
            team.TotalScore += points;
        }

        public void RecordTeamLoss(Team team, int points = 0)
        {
            team.Losses++;
            team.LoseStreak++;
            team.WinStreak = 0; // Reset win streak
            team.TotalScore += points;
        }
        #endregion

        public Team CreateTeam(string teamName, List<Member> members, int rank)
        {
            return new Team()
            {
                Name = teamName,
                Members = members,
                Rank = rank
            };
        }

        public void DeleteTeamFromDatabase(string teamName)
        {
            foreach (Tournament tournament in _dataManager.TournamentsDatabaseFile.Tournaments)
            {
                Team? teamToRemove = tournament.Teams
                    .FirstOrDefault(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase));
                if (teamToRemove != null)
                {
                    tournament.Teams.Remove(teamToRemove);
                    break; // Exit after removing the team
                }
            }
        }
    }
}
