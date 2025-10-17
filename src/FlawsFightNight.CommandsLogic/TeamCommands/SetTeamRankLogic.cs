using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TeamCommands
{
    public class SetTeamRankLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;

        public SetTeamRankLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, TeamManager teamManager, TournamentManager tournamentManager) : base("Set Team Rank")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        public Embed SetTeamRankProcess(string teamName, int newRank)
        {
            if (!_teamManager.DoesTeamExist(teamName))
            {
                return _embedManager.ErrorEmbed(Name, $"No team found with the name: {teamName}\n\nPlease check the name and try again.");
            }

            // Grab tournament from team name
            var tournament = _tournamentManager.GetTournamentFromTeamName(teamName);

            // Check tournament type, only ladder tournaments can have ranks set manually
            if (!tournament.Type.Equals(TournamentType.Ladder))
            {
                return _embedManager.ErrorEmbed(Name, $"Ranks can only be set manually for teams in Ladder tournaments. The tournament '{tournament.Name}' is a {tournament.Type} tournament.");
            }

            // Grab team
            var team = _teamManager.GetTeamByName(teamName);

            // Check if new rank is valid
            if (newRank < 1 || newRank > tournament.Teams.Count)
            {
                return _embedManager.ErrorEmbed(Name, $"The new rank must be between 1 and {tournament.Teams.Count} (the current number of teams in the tournament). Please provide a valid rank and try again.");
            }

            // Check if team is already at that rank
            if (team.Rank == newRank)
            {
                return _embedManager.ErrorEmbed(Name, $"The team '{team.Name}' is already at rank {newRank}. Please provide a different rank and try again.");
            }

            // Moving the team up the ranks (to a lower number)
            if (newRank < team.Rank)
            {
                // Move other teams down
                var teamsToMoveDown = tournament.Teams.Where(t => t.Rank >= newRank && t.Rank < team.Rank).ToList();
                foreach (var t in teamsToMoveDown)
                {
                    t.Rank++;
                }
            }

            // Moving the team down the ranks (to a higher number)
            else
            {
                // Move other teams up
                var teamsToMoveUp = tournament.Teams.Where(t => t.Rank <= newRank && t.Rank > team.Rank).ToList();
                foreach (var t in teamsToMoveUp)
                {
                    t.Rank--;
                }
            }

            // Set the team's new rank
            team.Rank = newRank;

            // Reassign ranks to ensure no duplicates or gaps
            tournament.ReassignRanksInTournament();

            // Save changes
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.SetTeamRankSuccess(team, tournament);
        }
    }
}
