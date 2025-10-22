using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Autocomplete
{
    public class TournamentIdAutocomplete : AutocompleteHandler
    {
        private readonly AutocompleteCache _cache;

        public TournamentIdAutocomplete(AutocompleteCache cache)
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
            var tournaments = string.IsNullOrWhiteSpace(value)
                            ? _cache.GetTournamentIdsMatchingInput("")
                            : _cache.GetTournamentIdsMatchingInput(value);
            Console.WriteLine("Yup");
            return AutocompletionResult.FromSuccess(tournaments);
        }
    }
}
