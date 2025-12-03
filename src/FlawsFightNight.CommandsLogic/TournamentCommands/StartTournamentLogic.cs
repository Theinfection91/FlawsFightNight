using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
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

            // Check if the tournament can be started
            if (!tournament.CanStart(out var errorReason))
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' does not meet the requirements to start: {errorReason.Info}");
            }

            // Build match schedules if applicable and start tournament
            if (tournament is NormalRoundRobinTournament normalRRTournament)
            {
                _matchManager.BuildRoundRobinMatchSchedule(normalRRTournament);
            }
            if (tournament is OpenRoundRobinTournament openRRTournament)
            {
                _matchManager.BuildRoundRobinMatchSchedule(openRRTournament);
            }
            tournament.Start();

            // Send out match schedules to each member of every team
            _matchManager.SendMatchSchedulesToTeamsResolver(tournament);

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            return _embedManager.StartTournamentSuccess(tournament);
        }
    }
}
