using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Autocomplete
{
    public class TagLogToMatchAutocomplete : AutocompleteHandler
    {
        private readonly AutocompleteCache _autocompleteCache;

        public TagLogToMatchAutocomplete(AutocompleteCache autocompleteCache)
        {
            _autocompleteCache = autocompleteCache;
        }

        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            // Extract the user's previously typed tournament_id value from the options
            var tournamentIdOption = autocompleteInteraction.Data.Options.FirstOrDefault(x => x.Name == "tournament_id");
            var tournamentId = tournamentIdOption?.Value?.ToString();

            var currentInput = autocompleteInteraction.Data.Current.Value?.ToString() ?? string.Empty;
            
            var suggestions = _autocompleteCache.GetMatchesForTagging(tournamentId, currentInput);
            
            return AutocompletionResult.FromSuccess(suggestions);
        }
    }
}
