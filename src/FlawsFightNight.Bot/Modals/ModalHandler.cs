using Discord.Interactions;
using FlawsFightNight.Bot.Autocomplete;
using FlawsFightNight.CommandsLogic.TeamCommands;
using FlawsFightNight.CommandsLogic.TournamentCommands;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Managers;
using System;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Modals
{
    public class ModalHandler : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AutocompleteCache _autocompleteCache;
        private readonly DeleteTeamLogic _deleteTeamLogic;
        private readonly DeleteTournamentLogic _deleteTournamentLogic;
        private readonly EmbedManager _embedManager;
        private readonly EndTournamentLogic _endTournamentLogic;
        private readonly StartTournamentLogic _startTournamentLogic;
        private readonly TeamManager _teamManager;
        private readonly TournamentManager _tournamentManager;

        public ModalHandler(
            AutocompleteCache autocompleteCache,
            DeleteTeamLogic deleteTeamLogic,
            DeleteTournamentLogic deleteTournamentLogic,
            EmbedManager embedManager,
            EndTournamentLogic endTournamentLogic,
            StartTournamentLogic startTournamentLogic,
            TeamManager teamManager,
            TournamentManager tournamentManager)
        {
            _autocompleteCache = autocompleteCache;
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
                await DeferAsync();

                if (modal.TeamNameOne != modal.TeamNameTwo)
                {
                    await FollowupAsync(embed: _embedManager.ErrorEmbed(modal.Title,
                        "The Team Names do not match. Please try again. The inputs are case sensitive."));
                    return;
                }

                if (!_teamManager.DoesTeamExist(modal.TeamNameOne, true))
                {
                    await FollowupAsync(embed: _embedManager.ErrorEmbed(modal.Title,
                        $"No team found with the name: {modal.TeamNameOne}. Please check the name and try again. This is also case sensitive."));
                    return;
                }

                var result = await Task.Run(() => _deleteTeamLogic.DeleteTeamProcess(modal.TeamNameOne));
                await FollowupAsync(embed: result);
                _autocompleteCache.UpdateCache();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Modal Error - Delete Team] {ex}");
                await FollowupAsync("An error occurred while processing this modal.");
            }
        }
        #endregion

        #region Tournament Delete/End/Start
        [ModalInteraction("delete_tournament")]
        public async Task HandleDeleteTournamentModalAsync(DeleteTournamentModal modal)
        {
            try
            {
                await DeferAsync();

                if (modal.TournamentIdOne != modal.TournamentIdTwo)
                {
                    await FollowupAsync(embed: _embedManager.ErrorEmbed(modal.Title,
                        "The Tournament IDs do not match. Please try again. The inputs are case sensitive so the T must be uppercase."));
                    return;
                }

                if (!_tournamentManager.IsTournamentIdInDatabase(modal.TournamentIdOne, true))
                {
                    await FollowupAsync(embed: _embedManager.ErrorEmbed(modal.Title,
                        $"No tournament found with ID: {modal.TournamentIdOne}. Please check the ID and try again. This is also case sensitive and the T must be uppercase."));
                    return;
                }

                var result = await Task.Run(() => _deleteTournamentLogic.DeleteTournamentProcess(modal.TournamentIdOne));
                await FollowupAsync(embed: result);
                _autocompleteCache.UpdateCache();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Modal Error - Delete Tournament] {ex}");
                await FollowupAsync("An error occurred while processing this modal.");
            }
        }

        [ModalInteraction("end_tournament")]
        public async Task HandleEndTournamentModalAsync(EndTournamentModal modal)
        {
            try
            {
                await DeferAsync();

                if (modal.TournamentIdOne != modal.TournamentIdTwo)
                {
                    await FollowupAsync(embed: _embedManager.ErrorEmbed(modal.Title,
                        "The Tournament IDs do not match. Please try again. The inputs are case sensitive so the T must be uppercase."));
                    return;
                }

                if (!_tournamentManager.IsTournamentIdInDatabase(modal.TournamentIdOne, true))
                {
                    await FollowupAsync(embed: _embedManager.ErrorEmbed(modal.Title,
                        $"No tournament found with ID: {modal.TournamentIdOne}. Please check the ID and try again. This is also case sensitive and the T must be uppercase."));
                    return;
                }

                var result = await Task.Run(() => _endTournamentLogic.EndTournamentProcess(modal.TournamentIdOne));
                await FollowupAsync(embed: result);
                _autocompleteCache.UpdateCache();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Modal Error - End Tournament] {ex}");
                await FollowupAsync("An error occurred while processing this modal.");
            }
        }

        [ModalInteraction("start_tournament")]
        public async Task HandleStartTournamentModalAsync(StartTournamentModal modal)
        {
            try
            {
                await DeferAsync();

                if (modal.TournamentIdOne != modal.TournamentIdTwo)
                {
                    await FollowupAsync(embed: _embedManager.ErrorEmbed(modal.Title,
                        "The Tournament IDs do not match. Please try again. The inputs are case sensitive so the T must be uppercase."));
                    return;
                }

                if (!_tournamentManager.IsTournamentIdInDatabase(modal.TournamentIdOne, true))
                {
                    await FollowupAsync(embed: _embedManager.ErrorEmbed(modal.Title,
                        $"No tournament found with ID: {modal.TournamentIdOne}. Please check the ID and try again. This is also case sensitive and the T must be uppercase."));
                    return;
                }

                var result = await Task.Run(() => _startTournamentLogic.StartTournamentProcess(modal.TournamentIdOne));
                await FollowupAsync(embed: result);
                _autocompleteCache.UpdateCache();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Modal Error - Start Tournament] {ex}");
                await FollowupAsync("An error occurred while processing this modal.");
            }
        }
        #endregion
    }
}
