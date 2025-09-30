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
                    return RoundRobinStartTournamentResolver(tournament);
                default:
                    return _embedManager.ErrorEmbed(Name, "Only Round Robin tournaments are implemented right now. Can not start any other time at this point.");
            }
        }
     
        private Embed RoundRobinStartTournamentResolver(Tournament tournament)
        {
            // Check if teams are locked
            if (!tournament.IsTeamsLocked)
            {
                return _embedManager.ErrorEmbed(Name, $"The teams in the tournament '{tournament.Name}' are not locked. Please lock the teams before starting the tournament.");
            }
            switch (tournament.Type)
            {
                case TournamentType.RoundRobin:
                    switch (tournament.RoundRobinMatchType)
                    {
                        case RoundRobinMatchType.Open:
                            return RoundRobinOpenStartTournament(tournament);
                        case RoundRobinMatchType.Normal:
                            return RoundRobinNormalStartTournament(tournament);
                        default:
                            return _embedManager.ErrorEmbed(Name, "Only Normal and Open Round Robin tournaments are implemented right now. Can not start any other type at this point.");
                    }
                default:
                    return _embedManager.ErrorEmbed(Name, "Only Round Robin tournaments are implemented right now. Can not start any other time at this point.");
            }
        }

        private Embed LadderStartTournamentProcess(Tournament tournament)
        {
            // TODO Expand later if needed, all Ladder needs for most things is IsRunning
            tournament.IsRunning = true;

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            // Return Embed with tournament information
            return _embedManager.StartTournamentSuccessResolver(tournament);
        }

        private Embed RoundRobinNormalStartTournament(Tournament tournament)
        {
            // Start the tournament, build normal schedule with rounds
            _matchManager.BuildMatchScheduleResolver(tournament);
            tournament.InitiateStartNormalRoundRobinTournament();

            // Send team match schedules to each user
            _matchManager.SendMatchSchedulesToTeamsResolver(tournament);

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            // Return Embed with tournament information
            return _embedManager.StartTournamentSuccessResolver(tournament);
        }

        private Embed RoundRobinOpenStartTournament(Tournament tournament)
        {
            // Start the tournament, build schedule without rounds
            _matchManager.BuildMatchScheduleResolver(tournament);
            tournament.InitiateStartOpenRoundRobinTournament();

            // Send out messages, no schedule since it is open
            _matchManager.SendMatchSchedulesToTeamsResolver(tournament);

            // Save and reload the tournament database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            // Backup to git repo
            _gitBackupManager.CopyAndBackupFilesToGit();

            // Return Embed with tournament information
            return _embedManager.StartTournamentSuccessResolver(tournament);
        }
    }
}
