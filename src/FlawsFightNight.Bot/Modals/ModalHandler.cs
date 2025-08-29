using Discord.Interactions;
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
        public ModalHandler(EmbedManager embedManager)
        {
            _embedManager = embedManager;
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
        #endregion
    }
}
