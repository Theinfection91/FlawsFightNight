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
        private readonly EmbedManager _embedManager;
        private readonly GitBackupManager _gitBackupManager;
        private readonly MatchManager _matchManager;
        private readonly MemberManager _memberManager;
        private readonly TournamentManager _tournamentManager;
        public StartTournamentLogic(EmbedManager embedManager, GitBackupManager gitBackupManager, MatchManager matchManager, MemberManager memberManager, TournamentManager tournamentManager) : base("Start Tournament")
        {
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _matchManager = matchManager;
            _memberManager = memberManager;
            _tournamentManager = tournamentManager;
        }

        public async Task<Embed> StartTournamentProcess(string tournamentId)
        {
            // Grab tournament, modal should have ensured it exists
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if the tournament can be started
            if (!tournament.CanStart(out var errorReason))
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' does not meet the requirements to start: {errorReason.Info}");
            }

            // Start tournament before building match schedules to prevent clearing match logs early
            tournament.Start();

            // Update member stats for all members in the tournament before starting
            _memberManager.IncrementMembersTournamentsPlayed(tournament.GetAllMembers());

            // Build match schedules if applicable and start tournament
            if (tournament is NormalRoundRobinTournament normalRRTournament)
            {
                _matchManager.BuildRoundRobinMatchSchedule(normalRRTournament);
            }
            if (tournament is OpenRoundRobinTournament openRRTournament)
            {
                _matchManager.BuildRoundRobinMatchSchedule(openRRTournament);
            }

            // Send out match schedules to each member of every team
            _matchManager.SendMatchSchedulesToTeamsResolver(tournament);

            // Save and reload databases
            await _tournamentManager.SaveAndReloadTournamentDataFiles(tournament);
            await _memberManager.SaveAndReloadMemberProfiles();

            // Backup to git repo
            _gitBackupManager.EnqueueBackup();

            return _embedManager.StartTournamentSuccess(tournament);
        }
    }
}
