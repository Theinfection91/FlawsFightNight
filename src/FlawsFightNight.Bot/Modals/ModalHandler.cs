using Discord.Interactions;
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
        private EmbedManager _embedManager;
        private EndTournamentLogic _endTournamentLogic;
        private TournamentManager _tournamentManager;
        public ModalHandler(EmbedManager embedManager, EndTournamentLogic endTournamentLogic, TournamentManager tournamentManager)
        {
            _embedManager = embedManager;
            _endTournamentLogic = endTournamentLogic;
            _tournamentManager = tournamentManager;
        }

        #region Tournament Start/End/Delete
        [ModalInteraction("delete_tournament")]
        public async Task HandleDeleteTournamentModalAsync(DeleteTournamentModal modal)
        {
            try
            {
                if (modal.TournamentIdOne != modal.TournamentIdTwo)
                {
                    await RespondAsync(embed: _embedManager.ErrorEmbed("Delete Tournament", "The Tournament IDs do not match. Please try again. The inputs are case sensitive so the T must be uppercase."), ephemeral: true);
                    return;
                }

                
                // var result = _deleteTournamentLogic.DeleteTournamentProcess(modal.TournamentIdOne);
                // await RespondAsync(embed: result);
                await RespondAsync(embed: _embedManager.ToDoEmbed($"TODO Delete Tourament Logic and Embed"), ephemeral: true);
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
                    await RespondAsync(embed: _embedManager.ErrorEmbed("End Tournament", "The Tournament IDs do not match. Please try again. The inputs are case sensitive so the T must be uppercase."), ephemeral: true);
                    return;
                }

                // Check if the tournament exists, grab it if so
                if (!_tournamentManager.IsTournamentIdInDatabase(modal.TournamentIdOne, true))
                {
                    await RespondAsync(embed: _embedManager.ErrorEmbed("End Tournament", $"No tournament found with ID: {modal.TournamentIdOne}. Please check the ID and try again. This is also case sensitive and the T must be uppercase."), ephemeral: true);
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
        #endregion
    }
}
