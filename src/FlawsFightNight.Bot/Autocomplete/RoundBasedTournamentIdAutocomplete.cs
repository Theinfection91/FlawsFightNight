using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Autocomplete
{
    public class RoundBasedTournamentIdAutocomplete : AutocompleteHandler
    {
        private readonly AutocompleteCache _cache;
        public RoundBasedTournamentIdAutocomplete(AutocompleteCache cache)
        {
            _cache = cache;
        }
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            var currentInput = autocompleteInteraction.Data.Current.Value as string ?? "";
            var suggestions = string.IsNullOrWhiteSpace(currentInput)
                                ? _cache.GetRoundBasedTournamentIds("")
                                : _cache.GetRoundBasedTournamentIds(currentInput);
            return AutocompletionResult.FromSuccess(suggestions);
        }
    }
}
