using Discord;
using FlawsFightNight.Commands;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.MatchLogs;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.TournamentCommands
{
    public class EndTournamentHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly MatchService _matchService;
        private readonly MemberService _memberManager;
        private readonly TournamentService _tournamentService;
        public EndTournamentHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, MatchService matchService, MemberService memberManager, TournamentService tournamentService) : base("End Tournament")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _matchService = matchService;
            _memberManager = memberManager;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> EndTournamentProcess(string tournamentId)
        {
            // Grab tournament, modal should have ensured it exists
            var tournament = _tournamentService.GetTournamentById(tournamentId);

            if (!tournament.CanEnd(out var errorReason))
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' cannot be ended at this time: {errorReason.Info}");
            }

            // Award completion experience to all members
            _memberManager.AwardCompletionTournamentForMembers(tournament.GetAllMembers());

            // Handle Normal and Open Round Robin Tournaments
            if (tournament is ITieBreakerRankSystem tbTournament)
            {
                // Convert any bye matches in Normal Round Robin if any
                if (tournament is NormalRoundRobinTournament normalRRTournament)
                {
                    if (normalRRTournament.DoesRoundContainByeMatch())
                    {
                        if (normalRRTournament.MatchLog is NormalRoundRobinMatchLog normalRRMatchLog)
                        {
                            normalRRMatchLog.ConvertByeMatch(normalRRTournament.CurrentRound);
                        }
                    }
                }
                // Check if tie breaker is needed for first place
                if (_matchService.IsTieBreakerNeededForFirstPlace(tournament.MatchLog))
                {
                    // Resolve tie breaker info and get the winner
                    var tiedTeams = _matchService.GetTiedTeams(tournament.MatchLog);
                    var (tieBreakerInfo, winnerTeamName) = tbTournament.TieBreakerRule.ResolveTie(tiedTeams, tournament.MatchLog);

                    // Apply tie-breaker rankings to the tied teams
                    _tournamentService.ApplyTieBreakerRankings(tournament, tiedTeams, winnerTeamName);

                    // Handle tournament end awards (1st, 2nd, 3rd place)
                    _memberManager.HandleTournamentEndAwards(tournament.Teams);

                    // End the tournament
                    tournament.End();

                    // Save the updated tournament state
                    await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

                    // Backup to git repo
                    _gitBackupService.EnqueueBackup();

                    return _embedFactory.RoundRobinEndTournamentSuccess(tournament, true, tieBreakerInfo);
                }
                else
                {
                    // Handle tournament end awards (1st, 2nd, 3rd place)
                    _memberManager.HandleTournamentEndAwards(tournament.Teams);

                    // End the tournament, no tie breaker needed
                    tournament.End();

                    // Save the updated tournament state
                    await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

                    // Backup to git repo
                    _gitBackupService.EnqueueBackup();

                    return _embedFactory.RoundRobinEndTournamentSuccess(tournament);
                }
            }

            // Handle Normal Ladder Tournament
            if (tournament is NormalLadderTournament ladderTournament)
            {
                // Handle tournament end awards (1st, 2nd, 3rd place)
                _memberManager.HandleTournamentEndAwards(tournament.Teams);

                // End the tournament, normal ladder tournaments can be ended as long as they are running
                ladderTournament.End();

                // Save the updated tournament state
                await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

                // Backup to git repo
                _gitBackupService.EnqueueBackup();

                return _embedFactory.NormalLadderEndTournamentSuccess(tournament);
            }

            // Handle DSR Tournament
            if (tournament is DSRLadderTournament dsrTournament)
            {
                // Handle tournament end awards (1st, 2nd, 3rd place)
                _memberManager.HandleTournamentEndAwards(tournament.Teams);

                // End the tournament, DSR tournaments can be ended as long as they are running
                dsrTournament.End();

                // Save the updated tournament state
                await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

                // Backup to git repo
                _gitBackupService.EnqueueBackup();

                return _embedFactory.DSRLadderEndTournamentSuccess(tournament);
            }

            return _embedFactory.ErrorEmbed(Name, "An error occurred while trying to end the tournament. Please try again later.");
        }
    }
}
