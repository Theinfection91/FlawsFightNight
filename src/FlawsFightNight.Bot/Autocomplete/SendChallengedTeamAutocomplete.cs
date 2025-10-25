using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Autocomplete
{
    public class SendChallengedTeamAutocomplete : AutocompleteHandler
    {
        private readonly AutocompleteCache _cache;
        public SendChallengedTeamAutocomplete(AutocompleteCache cache)
        {
            _cache = cache;
        }
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
    IInteractionContext context,
    IAutocompleteInteraction autocompleteInteraction,
    IParameterInfo parameter,
    IServiceProvider services)
        {
            // TODO: Could add in rank filtering as well

            // Get the challenger team from the command options
            var challengerTeamName = autocompleteInteraction.Data.Options
                .FirstOrDefault(o => o.Name == "challenger_team_name")?.Value as string;

            // Get the user's current input for this field
            var focusedOption = autocompleteInteraction.Data.Current;
            var input = focusedOption?.Value?.ToString()?.Trim() ?? string.Empty;

            // Pull suggestions from cache
            var suggestions = _cache
                .GetTeamsForSendChallenge(input)
                .Where(t => !string.Equals((string?)t.Value, challengerTeamName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return AutocompletionResult.FromSuccess(suggestions);
        }

    }
}
