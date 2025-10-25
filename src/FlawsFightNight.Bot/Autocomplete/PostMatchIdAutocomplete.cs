using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Autocomplete
{
    public class PostMatchIdAutocomplete : AutocompleteHandler
    {
        private readonly AutocompleteCache _cache;
        public PostMatchIdAutocomplete(AutocompleteCache cache)
        {
            _cache = cache;
        }
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            var value = (autocompleteInteraction.Data.Current.Value as string ?? string.Empty).ToLower();
            var matches = string.IsNullOrWhiteSpace(value)
                            ? _cache.GetPostMatchIdsMatchingInput("")
                            : _cache.GetPostMatchIdsMatchingInput(value);

            return AutocompletionResult.FromSuccess(matches);
        }
    }
}
