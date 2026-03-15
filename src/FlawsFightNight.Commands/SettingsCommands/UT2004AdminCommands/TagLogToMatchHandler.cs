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
        public TagLogToMatchHandler(GitBackupService gitBackupService, TournamentService tournamentService, MatchService matchService, UT2004StatsService ut2004StatsService) : base("Tag Log To Match")
        {
            _gitBackupService = gitBackupService;
            _tournamentService = tournamentService;
            _matchService = matchService;
            _ut2004StatsService = ut2004StatsService;
        }

        public async Task<Embed> TagLogToMatchProcess(string statLogId, string tournamentId, string matchId)
        {
            if (!_ut2004StatsService.DoesStatLogExist(statLogId))
                return _embedFactory.ErrorEmbed(Name, $"The provided stat log ID `{statLogId}` does not exist in the index.");

            if (!_tournamentService.IsTournamentIdInDatabase(tournamentId))
                return _embedFactory.ErrorEmbed(Name, $"The provided tournament ID `{tournamentId}` does not exist.");

            if (!_matchService.IsMatchIdInDatabase(matchId))
                return _embedFactory.ErrorEmbed(Name, $"The provided match ID `{matchId}` does not exist in the database.");

            var tournament = _tournamentService.GetTournamentById(tournamentId);
            var match = _matchService.GetPostMatchByIdInTournament(tournament!, matchId);
            if (match == null)
                return _embedFactory.ErrorEmbed(Name, $"The provided match ID `{matchId}` does not exist in tournament `{tournamentId}`.");

            string previousTagInfo = string.Empty;
            // If tagged to another match, untag from that match first before tagging to the new match
            if (_ut2004StatsService.IsTournamentMatchTagged(tournament.Id, match.Id))
            {
                var statLog = _ut2004StatsService.GetStatLogIndexEntryByTournamentMatch(tournament.Id, match.Id);
                previousTagInfo = $"The stat log `{statLog.Id}` was previously tagged to the match `{statLog.MatchId}` in tournament `{statLog.TournamentId}`. It has been untagged from that match.";
                statLog.UnTagTournamentMatch();
            }

            await _ut2004StatsService.TagTournamentMatchToStatLog(statLogId, tournament.Name, tournament.Id, match.Id);

            _gitBackupService.EnqueueBackup();

            return _embedFactory.GenericEmbed(Name + " Success", $"The stat log `{statLogId}` has been successfully tagged to the match `{matchId}` in tournament `{tournamentId}`. {previousTagInfo}", Color.DarkBlue);
        }
    }
}
