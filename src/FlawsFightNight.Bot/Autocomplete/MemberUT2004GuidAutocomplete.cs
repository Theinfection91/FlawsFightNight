using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Autocomplete
{
    public class MemberUT2004GuidAutocomplete : AutocompleteHandler
    {
        private readonly AutocompleteCache _cache;

        public MemberUT2004GuidAutocomplete(AutocompleteCache cache)
        {
            _cache = cache;
        }

        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(
            IInteractionContext context,
            IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter,
            IServiceProvider services)
        {
            ulong memberId = 0;
            var memberOption = autocompleteInteraction.Data.Options.FirstOrDefault(o => o.Name == "member");
            if (memberOption?.Value is ulong uid) memberId = uid;
            else if (memberOption?.Value is string s && ulong.TryParse(s, out var parsed)) memberId = parsed;
            else memberId = context.User.Id;

            var input = autocompleteInteraction.Data.Current?.Value?.ToString()?.Trim() ?? string.Empty;

            if (memberId == 0)
                return AutocompletionResult.FromSuccess(Enumerable.Empty<AutocompleteResult>());

            var suggestions = _cache
                .GetMemberUT2004Guids(memberId)
                .Where(g => string.IsNullOrWhiteSpace(input) || g.Value.ToString()!.Contains(input, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return AutocompletionResult.FromSuccess(suggestions);
        }
    }
}
