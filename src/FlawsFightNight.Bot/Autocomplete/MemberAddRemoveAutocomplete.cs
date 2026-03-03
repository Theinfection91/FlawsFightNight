using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Autocomplete
{
    public class MemberAddRemoveAutocomplete : AutocompleteHandler
    {
        private readonly AutocompleteCache _cache;
        public MemberAddRemoveAutocomplete(AutocompleteCache cache)
        {
            _cache = cache;
        }

        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            var teamName = autocompleteInteraction.Data.Options.FirstOrDefault(o => o.Name == "team_name")?.Value as string ?? string.Empty;

            var suggestions = _cache.GetAllTeams(teamName);

            return AutocompletionResult.FromSuccess(suggestions);
        }
    }
}
