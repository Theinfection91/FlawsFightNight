using Discord.Interactions;
using FlawsFightNight.Bot.Autocomplete;
using FlawsFightNight.Commands.TeamCommands;
using FlawsFightNight.Commands.TournamentCommands;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Services;
using System;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Modals
{
    public class ModalHandler : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AutocompleteCache _autocompleteCache;
        private readonly DeleteTeamHandler _deleteTeamLogic;
        private readonly DeleteTournamentHandler _deleteTournamentLogic;
        private readonly EmbedFactory _embedFactory;
        private readonly EndTournamentHandler _endTournamentLogic;
        private readonly StartTournamentHandler _startTournamentLogic;
        private readonly TeamService _teamService;
        private readonly TournamentService _tournamentService;

        public ModalHandler(
            AutocompleteCache autocompleteCache,
            DeleteTeamHandler deleteTeamLogic,
            DeleteTournamentHandler deleteTournamentLogic,
            EmbedFactory embedFactory,
            EndTournamentHandler endTournamentLogic,
            StartTournamentHandler startTournamentLogic,
            TeamService teamService,
            TournamentService tournamentService)
        {
            _autocompleteCache = autocompleteCache;
            _deleteTeamLogic = deleteTeamLogic;
            _deleteTournamentLogic = deleteTournamentLogic;
            _embedFactory = embedFactory;
            _endTournamentLogic = endTournamentLogic;
            _startTournamentLogic = startTournamentLogic;
            _teamService = teamService;
            _tournamentService = tournamentService;
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
                    await FollowupAsync(embed: _embedFactory.ErrorEmbed(modal.Title,
                        "The Team Names do not match. Please try again. The inputs are case sensitive."));
                    return;
                }

                if (!_teamService.DoesTeamExist(modal.TeamNameOne, true))
                {
                    await FollowupAsync(embed: _embedFactory.ErrorEmbed(modal.Title,
                        $"No team found with the name: {modal.TeamNameOne}. Please check the name and try again. This is also case sensitive."));
                    return;
                }

                var result = await _deleteTeamLogic.DeleteTeamProcess(modal.TeamNameOne);
                await FollowupAsync(embed: result);
                _autocompleteCache.Update();
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
                    await FollowupAsync(embed: _embedFactory.ErrorEmbed(modal.Title,
                        "The Tournament IDs do not match. Please try again. The inputs are case sensitive so the T must be uppercase."));
                    return;
                }

                if (!_tournamentService.IsTournamentIdInDatabase(modal.TournamentIdOne, true))
                {
                    await FollowupAsync(embed: _embedFactory.ErrorEmbed(modal.Title,
                        $"No tournament found with ID: {modal.TournamentIdOne}. Please check the ID and try again. This is also case sensitive and the T must be uppercase."));
                    return;
                }

                var result = await _deleteTournamentLogic.DeleteTournamentProcess(modal.TournamentIdOne);
                await FollowupAsync(embed: result);
                _autocompleteCache.Update();
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
                    await FollowupAsync(embed: _embedFactory.ErrorEmbed(modal.Title,
                        "The Tournament IDs do not match. Please try again. The inputs are case sensitive so the T must be uppercase."));
                    return;
                }

                if (!_tournamentService.IsTournamentIdInDatabase(modal.TournamentIdOne, true))
                {
                    await FollowupAsync(embed: _embedFactory.ErrorEmbed(modal.Title,
                        $"No tournament found with ID: {modal.TournamentIdOne}. Please check the ID and try again. This is also case sensitive and the T must be uppercase."));
                    return;
                }

                var result = await _endTournamentLogic.EndTournamentProcess(modal.TournamentIdOne);
                await FollowupAsync(embed: result);
                _autocompleteCache.Update();
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
                    await FollowupAsync(embed: _embedFactory.ErrorEmbed(modal.Title,
                        "The Tournament IDs do not match. Please try again. The inputs are case sensitive so the T must be uppercase."));
                    return;
                }

                if (!_tournamentService.IsTournamentIdInDatabase(modal.TournamentIdOne, true))
                {
                    await FollowupAsync(embed: _embedFactory.ErrorEmbed(modal.Title,
                        $"No tournament found with ID: {modal.TournamentIdOne}. Please check the ID and try again. This is also case sensitive and the T must be uppercase."));
                    return;
                }

                var result = await _startTournamentLogic.StartTournamentProcess(modal.TournamentIdOne);
                await FollowupAsync(embed: result);
                _autocompleteCache.Update();
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
