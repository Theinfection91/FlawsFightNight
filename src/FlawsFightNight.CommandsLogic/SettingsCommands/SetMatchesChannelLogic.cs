using Discord;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.SetCommands
{
    public class SetMatchesChannelLogic : Logic
    {
        private EmbedManager _embedManager;
        private TournamentManager _tournamentManager;

        public SetMatchesChannelLogic(EmbedManager embedManager, TournamentManager tournamentManager) : base("Set Matches Channel")
        {
            _embedManager = embedManager;
            _tournamentManager = tournamentManager;
        }

        public Embed SetMatchesChannelProcess(string tournamentId, IMessageChannel channel)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }

            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // TODO Expand further if needed
            tournament.MatchesChannelId = channel.Id;

            // Save and reload the tournaments database
            _tournamentManager.SaveAndReloadTournamentsDatabase();

            return _embedManager.SetMatchesChannelSuccess(channel, tournament);
        }
    }
}
