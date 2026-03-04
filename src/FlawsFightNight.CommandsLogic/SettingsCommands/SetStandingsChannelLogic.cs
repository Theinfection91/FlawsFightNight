using Discord;
using FlawsFightNight.Commands;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.SettingsCommands
{
    public class SetStandingsChannelLogic : CommandHandler
    {
        private EmbedFactory _embedFactory;
        private GitBackupService _gitBackupService;
        private TournamentService _tournamentService;

        public SetStandingsChannelLogic(EmbedFactory embedFactory, GitBackupService gitBackupService, TournamentService tournamentService) : base("Set Standings Channel")
        {
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> SetStandingsChannelProcess(string tournamentId, IMessageChannel channel)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentService.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedFactory.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentService.GetTournamentById(tournamentId);

            tournament.StandingsChannelId = channel.Id;

            // Save and reload the tournaments database
            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);

            // Backup to git repo
            _gitBackupService.EnqueueBackup();

            return _embedFactory.SetStandingsChannelSuccess(channel, tournament);
        }
    }
}
