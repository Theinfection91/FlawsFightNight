using Discord;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.MatchCommands
{
    public class EditMatchLogic : Logic
    {
        private EmbedManager _embedManager;
        private MatchManager _matchManager;
        private TournamentManager _tournamentManager;
        public EditMatchLogic(EmbedManager embedManager, MatchManager matchManager, TournamentManager tournamentManager) : base("Edit Match")
        {
            _embedManager = embedManager;
            _matchManager = matchManager;
            _tournamentManager = tournamentManager;
        }

        public Embed EditMatchProcess(string tournamentId, string matchId, string winningTeamName, int winningTeamScore, int losingTeamScore)
        {
            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Send through switch resolver based on tournament type
            switch (tournament.Type)
            {
                case Core.Enums.TournamentType.RoundRobin:
                    return RoundRobinEditMatchProcess(tournament, tournamentId, matchId, winningTeamName, winningTeamScore, losingTeamScore);
                default:
                    break;
            }
            return _embedManager.ErrorEmbed(Name, $"Editing matches is not supported for {tournament.Type} tournaments at this time.");
        }

        private Embed RoundRobinEditMatchProcess(Tournament tournament, string tournamentId, string matchId, string winningTeamName, int winningTeamScore, int losingTeamScore)
        {
            return _embedManager.ToDoEmbed("Need Success Embed");
        }
    }
}
