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

        public static ComponentBuilder CreateHelpSelectMenu()
        {
            string selectId = "help_select";

            var selectMenu = new SelectMenuBuilder()
                .WithCustomId(selectId)
                .WithPlaceholder("📖 Select a help topic...")
                .AddOption("📖 Overview", "overview", "General overview of the bot and how to get started")
                .AddOption("🏆 Tournament Commands", "tournaments", "Create, start, end, and manage tournaments")
                .AddOption("👥 Team Commands", "teams", "Register, delete, and manage teams")
                .AddOption("⚔️ Match Commands", "matches", "Report wins, edit matches, and send challenges")
                .AddOption("⚙️ Settings & Admin", "settings", "Admin settings, channels, FTP, and permissions")
                .AddOption("📊 UT2004 Stats", "ut2004stats", "Player profiles, leaderboards, compare, and team suggestions")
                .AddOption("📋 Tournament Types", "tournamenttypes", "DSR Ladder, Normal Ladder, Round Robin explained")
                .AddOption("📺 LiveView System", "liveview", "Auto-updating standings, matches, and leaderboard channels");

            return new ComponentBuilder().WithSelectMenu(selectMenu);
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

        /// <summary>
        /// Creates the UT2004 leaderboard select menu with a "Request Full List" button.
        /// The button encodes the currently active <paramref name="section"/> so the handler
        /// knows which full leaderboard to DM.
        /// </summary>
        public static ComponentBuilder CreateUT2004LeaderboardSelectMenu(string section = "general")
        {
            string selectId = "ut2004leaderboard_select";

            var selectMenu = new SelectMenuBuilder()
                .WithCustomId(selectId)
                .WithPlaceholder("📊 Select a leaderboard category...")
                .AddOption("📊 General", "general", "Overall career stats and career bests")
                .AddOption("🚩 iCTF", "ictf", "Capture the Flag leaderboard")
                .AddOption("🎯 TAM", "tam", "Team Arena Master leaderboard")
                .AddOption("💣 iBR", "ibr", "Bombing Run leaderboard");

            string gameModeLabel = section switch
            {
                "ictf" => "iCTF",
                "tam" => "TAM",
                "ibr" => "iBR",
                _ => "General"
            };

            return new ComponentBuilder()
                .WithSelectMenu(selectMenu)
                .WithButton($"📋 Request Full {gameModeLabel} Leaderboard", customId: $"ut2004leaderboard_all:{section}", style: ButtonStyle.Secondary);
        }

        public static ComponentBuilder CreateUT2004CompareSelectMenu(ulong player1Id, ulong player2Id)
        {
            string selectId = $"ut2004compare_select:{player1Id}:{player2Id}";

            var selectMenu = new SelectMenuBuilder()
                .WithCustomId(selectId)
                .WithPlaceholder("📊 Select a comparison category...")
                .AddOption("📊 Overview", "overview", "Overall career stats and 1v1 win prediction")
                .AddOption("🚩 iCTF", "ictf", "Capture the Flag comparison")
                .AddOption("🎯 TAM", "tam", "Team Arena Master comparison")
                .AddOption("💣 iBR", "ibr", "Bombing Run comparison");

            return new ComponentBuilder().WithSelectMenu(selectMenu);
        }
    }
}