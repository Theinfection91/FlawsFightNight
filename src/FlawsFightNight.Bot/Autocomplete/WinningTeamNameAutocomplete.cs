using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Autocomplete
{
    public class WinningTeamNameAutocomplete : AutocompleteHandler
    {
        private readonly AutocompleteCache _cache;
        public WinningTeamNameAutocomplete(AutocompleteCache cache)
        {
            _cache = cache;
        }
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            // For handling match_id parameter
            var matchId = autocompleteInteraction.Data.Options.FirstOrDefault(o => o.Name == "match_id")?.Value as string;

            // For handling post_match_id parameter
            var postMatchId = autocompleteInteraction.Data.Options.FirstOrDefault(o => o.Name == "post_match_id")?.Value as string;

            List<AutocompleteResult> suggestions = new();

            if (!string.IsNullOrEmpty(matchId))
            {
                suggestions = string.IsNullOrWhiteSpace(matchId)
                                ? _cache.GetTeamsFromMatchId("")
                                : _cache.GetTeamsFromMatchId(matchId);
            }
            if (!string.IsNullOrEmpty(postMatchId))
            {
                suggestions = string.IsNullOrWhiteSpace(postMatchId)
                                ? _cache.GetTeamsFromPostMatchId("")
                                : _cache.GetTeamsFromPostMatchId(postMatchId);
            }

            return AutocompletionResult.FromSuccess(suggestions);
        }
    }
}
