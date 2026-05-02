using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Autocomplete
{
    public class UT2004WinProbTeamAutocomplete : AutocompleteHandler
    {
        private readonly AutocompleteCache _cache;

        public UT2004WinProbTeamAutocomplete(AutocompleteCache cache)
        {
            _cache = cache;
        }

        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            var focusedOption = autocompleteInteraction.Data.Current;
            var input = focusedOption?.Value?.ToString()?.Trim() ?? string.Empty;

            var suggestions = _cache.GetAllTeams(input);
            return AutocompletionResult.FromSuccess(suggestions);
        }
    }
}