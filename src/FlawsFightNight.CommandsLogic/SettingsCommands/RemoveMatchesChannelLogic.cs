using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.SettingsCommands
{
    public class RemoveMatchesChannelLogic : Logic
    {
        private EmbedManager _embedManager;
        private TournamentManager _tournamentManager;
        public RemoveMatchesChannelLogic(EmbedManager embedManager, TournamentManager tournamentManager) : base("Remove Matches Channel")
        {
            _embedManager = embedManager;
            _tournamentManager = tournamentManager;
        }
        public Embed RemoveMatchesChannelProcess(string tournamentId)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Check if a matches channel is set
            if (tournament.MatchesChannelId == 0)
            {
                return _embedManager.ErrorEmbed(Name, $"Tournament {tournament.Name} ({tournament.Id}) does not have a matches channel set.");
            }

            // Remove the matches channel
            tournament.MatchesChannelId = 0;
            tournament.MatchesMessageId = 0;

            // Save and reload the tournaments database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            return _embedManager.RemoveMatchesChannelSuccess(tournament);
        }
    }
}
