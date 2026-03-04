using Discord;

namespace FlawsFightNight.Bot.Components
{
    public static class ComponentFactory
    {
        /// <summary>
        /// Creates a confirmation component with ✅ Confirm and ❌ Cancel buttons.
        /// Custom IDs follow the pattern: {actionKey}_confirm:{userId} / {actionKey}_cancel:{userId}
        /// </summary>
        public static ComponentBuilder CreateConfirmationCancelButtons(string actionKey, ulong userId)
        {
            string confirmId = $"{actionKey}_confirm:{userId}";
            string cancelId = $"{actionKey}_cancel:{userId}";

            return new ComponentBuilder()
                .WithButton("✅ Confirm", customId: confirmId, style: ButtonStyle.Success)
                .WithButton("❌ Cancel", customId: cancelId, style: ButtonStyle.Danger);
        }

        public static ComponentBuilder CreateUT2004ProfileSelectMenu(ulong discordId)
        {
            string selectId = $"ut2004profile_select:{discordId}";

            var selectMenu = new SelectMenuBuilder()
                .WithCustomId(selectId)
                .WithPlaceholder("📊 Select a stats category...")
                .AddOption("📊 General", "general", "Overall career stats and career bests")
                .AddOption("🚩 iCTF", "ictf", "Capture the Flag statistics")
                .AddOption("🎯 TAM", "tam", "Team Arena Master statistics")
                .AddOption("💣 iBR", "ibr", "Bombing Run statistics");

            return new ComponentBuilder().WithSelectMenu(selectMenu);
        }
    }
}