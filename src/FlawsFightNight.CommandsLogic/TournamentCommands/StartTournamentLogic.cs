using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
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
            // Grab tournament, modal should have ensured it exists
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if the tournament is already running
            if (tournament.IsRunning)
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' is already running.");
            }
            
            switch (tournament.Type)
            {
                case TournamentType.Ladder:
                    return LadderStartTournamentProcess(tournament);
                case TournamentType.RoundRobin:
                    return RoundRobinStartTournamentProcess(tournament);
                default:
                    return _embedManager.ErrorEmbed(Name, "Only Round Robin tournaments are implemented right now. Can not start any other time at this point.");
            }
        }

        private Embed LadderStartTournamentProcess(Tournament tournament)
        {
            // Check if there are enough teams to start
            if (!tournament.IsLadderTournamentReadyToStart())
            {
                return _embedManager.ErrorEmbed(Name, $"The Ladder tournament '{tournament.Name}' does not have enough teams to start. Please ensure there are at least 3 teams registered.");
            }
            // Ensure all teams start with no wins/losses or points
            foreach (var team in tournament.Teams)
            {
                team.ResetTeamToZero();
            }

            tournament.LadderStartTournamentProcess();

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.StartTournamentSuccessResolver(tournament);
        }

        private Embed RoundRobinStartTournamentProcess(Tournament tournament)
        {
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
            tournament.RoundRobinStartTournamentProcess();

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
