using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
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
        public bool DoesTeamExist(string teamName, bool isCaseSensitive = false)
        {
            List<TournamentBase> tournaments = _dataManager.TournamentsDatabaseFile.NewTournaments;
            foreach (TournamentBase tournament in tournaments)
            {
                if (isCaseSensitive)
                {
                    if (tournament.Teams.Any(t => t.Name.Equals(teamName)))
                    {
                        return true; // Team name exists in the tournament
                    }
                }
                else
                {
                    if (tournament.Teams.Any(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true; // Team name exists in the tournament
                    }
                }
            }
            return false; // Team name does not exist in any tournament
        }

        public bool IsTeamNameUnique(string teamName)
        {
            List<TournamentBase> tournaments = _dataManager.TournamentsDatabaseFile.NewTournaments;
            foreach (TournamentBase tournament in tournaments)
            {
                if (tournament.Teams.Any(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase)))
                {
                    return false; // Team name already exists in the database somewhere
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
        public List<Team> GetAllLadderTeams()
        {
            List<Team> ladderTeams = new List<Team>();
            var tournaments = _dataManager.TournamentsDatabaseFile.NewTournaments;
            foreach (var tournament in tournaments)
            {
                if (tournament is NormalLadderTournament)
                {
                    ladderTeams.AddRange(tournament.Teams);
                }
            }
            return ladderTeams;
        }

        public List<Team> GetAllRoundBasedTeams()
        {
            List<Team> roundBasedTeams = new();
            List<TournamentBase> tournaments = _dataManager.TournamentsDatabaseFile.NewTournaments;
            foreach (var tournament in tournaments)
            {
                if (tournament is IRoundBased) // Will add elimination later
                {
                    roundBasedTeams.AddRange(tournament.Teams);
                }
            }
            return roundBasedTeams;
        }

        public Team GetTeamByName(string teamName)
        {
            List<TournamentBase> tournaments = _dataManager.TournamentsDatabaseFile.NewTournaments;
            foreach (var tournament in tournaments)
            {
                Team? team = tournament.Teams.FirstOrDefault(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase));
                if (team != null)
                {
                    return team; // Return the first matching team found
                }
            }
            return null; // No team found with the given name
        }

        public Team? GetTeamByName(TournamentBase tournament, string teamName)
        {
            return tournament.Teams
                .FirstOrDefault(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase));
        }
        #endregion

        #region Edit Match Helpers
        public void EditMatchRollback(TournamentBase tournament, PostMatch postMatch)
        {
            var winner = GetTeamByName(tournament, postMatch.Winner);
            var loser = GetTeamByName(tournament, postMatch.Loser);

            // Roll back stats
            winner.Wins--;
            winner.TotalScore -= postMatch.WinnerScore;

            loser.Losses--;
            loser.TotalScore -= postMatch.LoserScore;
        }

        public void EditMatchApply(TournamentBase tournament, PostMatch postMatch)
        {
            var winner = GetTeamByName(tournament, postMatch.Winner);
            var loser = GetTeamByName(tournament, postMatch.Loser);

            // Apply stats
            winner.Wins++;
            winner.TotalScore += postMatch.WinnerScore;

            loser.Losses++;
            loser.TotalScore += postMatch.LoserScore;
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
    }
}
