using Discord;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands.UT2004AdminCommands
{
    public class TagLogToMatchHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly TournamentService _tournamentService;
        private readonly MatchService _matchService;
        private readonly UT2004StatsService _ut2004StatsService;
        public TagLogToMatchHandler(EmbedFactory embedFactory, GitBackupService gitBackupService, TournamentService tournamentService, MatchService matchService, UT2004StatsService ut2004StatsService) : base("Tag Log To Match")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _tournamentService = tournamentService;
            _matchService = matchService;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<Embed> TagLogToMatchProcess(string statLogId, string tournamentId, string matchId)
        {
            var canonicalId = _ut2004StatsService.TryResolveStatLogId(statLogId) ?? statLogId;

            if (!_ut2004StatsService.DoesStatLogExist(canonicalId))
                return _embedFactory.ErrorEmbed(Name, $"The provided stat log ID `{statLogId}` does not exist in the index.");

            if (!_tournamentService.IsTournamentIdInDatabase(tournamentId))
                return _embedFactory.ErrorEmbed(Name, $"The provided tournament ID `{tournamentId}` does not exist.");

            var tournament = _tournamentService.GetTournamentById(tournamentId);
            var match = _matchService.GetPostMatchByIdInTournament(tournament!, matchId);
            if (match == null)
                return _embedFactory.ErrorEmbed(Name, $"The provided match ID `{matchId}` does not exist in tournament `{tournamentId}`.");

            string previousTagInfo = string.Empty;

            if (_ut2004StatsService.IsTournamentMatchTagged(tournamentId, matchId))
            {
                var existingEntry = _ut2004StatsService.GetStatLogIndexEntryByTournamentMatch(tournamentId, matchId);
                if (existingEntry != null && !existingEntry.Id.Equals(canonicalId, StringComparison.OrdinalIgnoreCase))
                {
                    previousTagInfo = $" Stat log `{existingEntry.Id}` was previously tagged to this match and has been untagged.";
                    await _ut2004StatsService.UnTagTournamentMatchFromStatLog(existingEntry.Id);
                }
            }

            await _ut2004StatsService.TagTournamentMatchToStatLog(canonicalId, tournament.Name, tournamentId, matchId);
            _gitBackupService.EnqueueBackup();

            return _embedFactory.GenericEmbed(Name + " Success", $"The stat log `{canonicalId}` has been successfully tagged to match `{matchId}` in tournament `{tournamentId}`.{previousTagInfo}", Color.DarkBlue);
        }
    }
}