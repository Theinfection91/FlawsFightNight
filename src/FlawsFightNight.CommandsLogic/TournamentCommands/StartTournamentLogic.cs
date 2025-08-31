using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TournamentCommands
{
    public class StartTournamentLogic : Logic
    {
        private EmbedManager _embedManager;
        private GitBackupManager _gitBackupManager;
        private MatchManager _matchManager;
        private TournamentManager _tournamentManager;

        public StartTournamentLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, MatchManager matchManager, TournamentManager tournamentManager) : base("Start Tournament")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _matchManager = matchManager;
            _tournamentManager = tournamentManager;
        }

        public Embed StartTournamentProcess(string tournamentId)
        {
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if the tournament is already running
            if (tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is already running.");
            }
            // Check if teams are locked
            if (!tournament.IsTeamsLocked)
            {
                return _embedManager.ErrorEmbed(Name, $"The teams in the tournament '{tournament.Name}' are not locked. Please lock the teams before starting the tournament.");
            }
            // Ensure all teams start with no wins/losses or points
            foreach (var team in tournament.Teams)
            {
                team.ResetTeamToZero();
            }

            // Start the tournament
            _matchManager.BuildMatchScheduleResolver(tournament);
            tournament.InitiateStartTournament();

            // Send team match schedules to each user
            _matchManager.SendMatchSchedulesToTeams(tournament);

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            // Return Embed with tournament information
            return _embedManager.StartTournamentSuccessResolver(tournament);
        }
    }
}
