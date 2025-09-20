using Discord.Interactions;
using FlawsFightNight.CommandsLogic.TeamCommands;
using FlawsFightNight.CommandsLogic.TournamentCommands;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Modals
{
    public class ModalHandler : InteractionModuleBase<SocketInteractionContext>
    {
        private DeleteTeamLogic _deleteTeamLogic;
        private DeleteTournamentLogic _deleteTournamentLogic;
        private EmbedManager _embedManager;
        private EndTournamentLogic _endTournamentLogic;
        private StartTournamentLogic _startTournamentLogic;
        private TeamManager _teamManager;
        private TournamentManager _tournamentManager;
        public ModalHandler(DeleteTeamLogic deleteTeamLogic, DeleteTournamentLogic deleteTournamentLogic, EmbedManager embedManager, EndTournamentLogic endTournamentLogic, StartTournamentLogic startTournamentLogic, TeamManager teamManager, TournamentManager tournamentManager)
        {
            _deleteTeamLogic = deleteTeamLogic;
            _deleteTournamentLogic = deleteTournamentLogic;
            _embedManager = embedManager;
            _endTournamentLogic = endTournamentLogic;
            _startTournamentLogic = startTournamentLogic;
            _teamManager = teamManager;
            _tournamentManager = tournamentManager;
        }

        #region Team Delete
        [ModalInteraction("delete_team")]
        public async Task HandleDeleteTeamModalAsync(DeleteTeamModal modal)
        {
            try
            {
                if (modal.TeamNameOne != modal.TeamNameTwo)
                {
                    await RespondAsync(embed: _embedManager.ErrorEmbed(modal.Title, "The Team Names do not match. Please try again. The inputs are case sensitive."), ephemeral: true);
                    return;
                }

                if (!_teamManager.DoesTeamExist(modal.TeamNameOne, true))
                {
                    await RespondAsync(embed: _embedManager.ErrorEmbed(modal.Title, $"No team found with the name: {modal.TeamNameOne}. Please check the name and try again. This is also case sensitive."), ephemeral: true);
                    return;
                }
                var result = _deleteTeamLogic.DeleteTeamProcess(modal.TeamNameOne);
                await RespondAsync(embed: result, ephemeral: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Modal Error: {ex}");
                await RespondAsync("An error occurred while processing this modal.", ephemeral: true);
            }
        }
        #endregion

        #region Tournament Delete/End/Start
        [ModalInteraction("delete_tournament")]
        public async Task HandleDeleteTournamentModalAsync(DeleteTournamentModal modal)
        {
            try
            {
                if (modal.TournamentIdOne != modal.TournamentIdTwo)
                {
                    await RespondAsync(embed: _embedManager.ErrorEmbed(modal.Title, "The Tournament IDs do not match. Please try again. The inputs are case sensitive so the T must be uppercase."), ephemeral: true);
                    return;
                }

                if (!_tournamentManager.IsTournamentIdInDatabase(modal.TournamentIdOne, true))
                {
                    await RespondAsync(embed: _embedManager.ErrorEmbed(modal.Title, $"No tournament found with ID: {modal.TournamentIdOne}. Please check the ID and try again. This is also case sensitive and the T must be uppercase."), ephemeral: true);
                    return;
                }

                var result = _deleteTournamentLogic.DeleteTournamentProcess(modal.TournamentIdOne);
                await RespondAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Modal Error: {ex}");
                await RespondAsync("An error occurred while processing this modal.", ephemeral: true);
            }
        }

        [ModalInteraction("end_tournament")]
        public async Task HandleEndTournamentModalAsync(EndTournamentModal modal)
        {
            try
            {
                if (modal.TournamentIdOne != modal.TournamentIdTwo)
                {
                    await RespondAsync(embed: _embedManager.ErrorEmbed(modal.Title, "The Tournament IDs do not match. Please try again. The inputs are case sensitive so the T must be uppercase."), ephemeral: true);
                    return;
                }

                if (!_tournamentManager.IsTournamentIdInDatabase(modal.TournamentIdOne, true))
                {
                    await RespondAsync(embed: _embedManager.ErrorEmbed(modal.Title, $"No tournament found with ID: {modal.TournamentIdOne}. Please check the ID and try again. This is also case sensitive and the T must be uppercase."), ephemeral: true);
                    return;
                }
                var result = _endTournamentLogic.EndTournamentProcess(modal.TournamentIdOne);
                await RespondAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Modal Error: {ex}");
                await RespondAsync("An error occurred while processing this modal.", ephemeral: true);
            }
        }

        [ModalInteraction("start_tournament")]
        public async Task HandleStartTournamentModalAsync(StartTournamentModal modal)
        {
            try
            {
                if (modal.TournamentIdOne != modal.TournamentIdTwo)
                {
                    await RespondAsync(embed: _embedManager.ErrorEmbed(modal.Title, "The Tournament IDs do not match. Please try again. The inputs are case sensitive so the T must be uppercase."), ephemeral: true);
                    return;
                }
                if (!_tournamentManager.IsTournamentIdInDatabase(modal.TournamentIdOne, true))
                {
                    await RespondAsync(embed: _embedManager.ErrorEmbed(modal.Title, $"No tournament found with ID: {modal.TournamentIdOne}. Please check the ID and try again. This is also case sensitive and the T must be uppercase."), ephemeral: true);
                    return;
                }
                var result = _startTournamentLogic.StartTournamentProcess(modal.TournamentIdOne);
                await RespondAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Modal Error: {ex}");
                await RespondAsync("An error occurred while processing this modal.", ephemeral: true);
            }
        }
        #endregion
    }
}
