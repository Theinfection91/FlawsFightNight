using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.TournamentCommands
{
    public class StartTournamentHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly MatchService _matchService;
        private readonly MemberService _memberManager;
        private readonly TournamentService _tournamentService;

        public StartTournamentHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, MatchService matchService, MemberService memberManager, TournamentService tournamentService) : base("Start Tournament")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _matchService = matchService;
            _memberManager = memberManager;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> StartTournamentProcess(string tournamentId)
        {
            // Grab tournament, modal should have ensured it exists
            var tournament = _tournamentService.GetTournamentById(tournamentId);

            // Check if the tournament can be started
            if (!tournament.CanStart(out var errorReason))
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' does not meet the requirements to start: {errorReason.Info}");
            }

            // Start tournament before building match schedules to prevent clearing match logs early
            tournament.Start();

            // Update member stats for all members in the tournament before starting
            _memberManager.IncrementMembersTournamentsPlayed(tournament.GetAllMembers());

            // Build match schedules if applicable and start tournament
            if (tournament is NormalRoundRobinTournament normalRRTournament)
            {
                _matchService.BuildRoundRobinMatchSchedule(normalRRTournament);
            }
            if (tournament is OpenRoundRobinTournament openRRTournament)
            {
                _matchService.BuildRoundRobinMatchSchedule(openRRTournament);
            }

            // Send out match schedules to each member of every team
            _matchService.SendMatchSchedulesToTeamsResolver(tournament);

            // Save and reload databases
            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);
            await _memberManager.SaveAndReloadMemberProfiles();

            // Backup to git repo
            _gitBackupService.EnqueueBackup();

            return _embedFactory.StartTournamentSuccess(tournament);
        }
    }
}
