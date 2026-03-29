using Discord;
using System;

namespace FlawsFightNight.Services
{
    public partial class EmbedFactory
    {
        #region Help Embeds

        public Embed HelpSectionEmbed(string section) =>
            section switch
            {
                "tournaments" => HelpTournamentsEmbed(),
                "teams" => HelpTeamsEmbed(),
                "matches" => HelpMatchesEmbed(),
                "settings" => HelpSettingsEmbed(),
                "ut2004stats" => HelpUT2004StatsEmbed(),
                "tournamenttypes" => HelpTournamentTypesEmbed(),
                "liveview" => HelpLiveViewEmbed(),
                _ => HelpOverviewEmbed()
            };

        private Embed HelpOverviewEmbed()
        {
            return new EmbedBuilder()
                .WithTitle("📖 Flaws Fight Night — Help Guide")
                .WithDescription(
                    "Welcome to **Flaws Fight Night Bot**! This bot manages competitive tournaments, tracks UT2004 player statistics, and provides LiveView channels that auto-update.\n\n" +
                    "Use the **dropdown below** to explore each topic in depth.\n\n" +
                    "**Quick Start (Admins):**\n" +
                    "1️⃣ `/tournament create` — Create a tournament\n" +
                    "2️⃣ `/team register` — Register teams with members\n" +
                    "3️⃣ `/tournament lock-teams` — Lock the roster (Round Robin)\n" +
                    "4️⃣ `/tournament start` — Start the tournament\n" +
                    "5️⃣ `/match report-win` — Report match results\n" +
                    "6️⃣ `/tournament end` — End the tournament\n\n" +
                    "**Quick Start (Players):**\n" +
                    "1️⃣ `/stats ut2004 register_guid` — Link your UT2004 GUID\n" +
                    "2️⃣ `/stats ut2004 my_player` — View your stats profile\n" +
                    "3️⃣ `/stats ut2004 leaderboard` — Check the leaderboard\n" +
                    "4️⃣ `/stats ut2004 compare` — Compare yourself with another player\n\n" +
                    "**Tip:** Most commands use autocomplete — start typing and pick from suggestions!")
                .WithColor(Color.Blue)
                .WithFooter("Flaws Fight Night — Help · Overview")
                .WithCurrentTimestamp()
                .Build();
        }

        private Embed HelpTournamentsEmbed()
        {
            return new EmbedBuilder()
                .WithTitle("🏆 Tournament Commands")
                .WithDescription(
                    "All tournament management commands live under `/tournament`. Most require **admin** permissions.\n\n" +
                    "**Creating & Deleting**\n" +
                    "• `/tournament create` — Create a new tournament. Specify name, type, and team size.\n" +
                    "• `/tournament delete` — Opens a modal to confirm deletion by tournament ID.\n\n" +
                    "**Team Locking (Round Robin)**\n" +
                    "• `/tournament lock-teams` — Lock the roster so no teams can be added/removed.\n" +
                    "• `/tournament unlock-teams` — Unlock to make roster changes before starting.\n\n" +
                    "**Starting & Ending**\n" +
                    "• `/tournament start` — Opens a modal; starts the tournament and generates the schedule.\n" +
                    "• `/tournament end` — Opens a modal; finalizes results and crowns a winner.\n\n" +
                    "**Round Management (Normal Round Robin)**\n" +
                    "• `/tournament lock-in-round` — Lock the current round after all matches are reported.\n" +
                    "• `/tournament unlock-round` — Unlock the round to correct results.\n" +
                    "• `/tournament next-round` — Advance to the next round (auto-reports bye matches).\n\n" +
                    "**Round Robin Setup**\n" +
                    "• `/tournament setup_round_robin` — Configure tie-breaker rules and single/double RR **before** starting.\n\n" +
                    "**Viewing**\n" +
                    "• `/tournament show-all` — Lists every tournament with status, teams, and round info.")
                .WithColor(Color.Gold)
                .WithFooter("Flaws Fight Night — Help · Tournament Commands")
                .WithCurrentTimestamp()
                .Build();
        }

        private Embed HelpTeamsEmbed()
        {
            return new EmbedBuilder()
                .WithTitle("👥 Team Commands")
                .WithDescription(
                    "All team management commands live under `/team`. Most require **admin** permissions.\n\n" +
                    "**Registration & Deletion**\n" +
                    "• `/team register` — Register a new team to a tournament. Provide a name, tournament ID, and up to 20 members.\n" +
                    "• `/team delete` — Opens a modal to confirm team deletion.\n\n" +
                    "**Rank Management (Ladders)**\n" +
                    "• `/team set_rank` — Manually set a team's rank in a ladder tournament. Other ranks adjust automatically.\n\n" +
                    "**Adding to a Team**\n" +
                    "• `/team add member` — Add up to 20 members to an existing team.\n" +
                    "• `/team add win` — Manually add win(s) to a team's record.\n" +
                    "• `/team add loss` — Manually add loss(es) to a team's record.\n\n" +
                    "**Removing from a Team**\n" +
                    "• `/team remove member` — Remove members from a team.\n" +
                    "• `/team remove win` — Remove win(s) from a team's record.\n" +
                    "• `/team remove loss` — Remove loss(es) from a team's record.\n\n" +
                    "**Notes:**\n" +
                    "• Team names are used throughout the system — keep them unique.\n" +
                    "• Members are Discord users; their display names are shown in embeds.\n" +
                    "• Manually adding/removing wins/losses is for admin corrections only.")
                .WithColor(Color.Blue)
                .WithFooter("Flaws Fight Night — Help · Team Commands")
                .WithCurrentTimestamp()
                .Build();
        }

        private Embed HelpMatchesEmbed()
        {
            return new EmbedBuilder()
                .WithTitle("⚔️ Match Commands")
                .WithDescription(
                    "All match commands live under `/match`.\n\n" +
                    "**Reporting Results**\n" +
                    "• `/match report-win` — Report the winner of a match. Uses autocomplete for match ID and winning team name. Works for all tournament types.\n\n" +
                    "**Editing (Admin)**\n" +
                    "• `/match edit` — Edit a completed match's result (Round Robin & Elimination). Change the winner, scores, etc.\n\n" +
                    "**Challenges (Ladder Tournaments)**\n" +
                    "• `/match challenge send` — Send a challenge from one team to another.\n" +
                    "  · *Normal Ladder:* Can only challenge up to 2 ranks above.\n" +
                    "  · *DSR Ladder:* Can challenge any rank, up or down.\n" +
                    "  · A team can only have one active challenge at a time.\n" +
                    "• `/match challenge cancel` — Cancel a previously sent challenge.\n\n" +
                    "**How Wins Work by Tournament Type:**\n" +
                    "• *Normal Ladder:* Winner takes the loser's rank; loser drops one.\n" +
                    "• *DSR Ladder:* Rating adjusts based on ELO-like system; ranks re-sort.\n" +
                    "• *Round Robin:* Win/loss recorded; points tracked for tie-breaking.\n\n" +
                    "**Tips:**\n" +
                    "• Autocomplete suggestions update after every command that changes data.\n" +
                    "• Team members on both sides receive DM notifications for ladder challenges.")
                .WithColor(Color.Orange)
                .WithFooter("Flaws Fight Night — Help · Match Commands")
                .WithCurrentTimestamp()
                .Build();
        }

        private Embed HelpSettingsEmbed()
        {
            return new EmbedBuilder()
                .WithTitle("⚙️ Settings & Admin Commands")
                .WithDescription(
                    "All settings commands live under `/settings` and require **admin** permissions.\n\n" +
                    "**Debug Admins**\n" +
                    "• `/settings add_debug_admin` — Grant a user admin permissions for the bot.\n" +
                    "• `/settings remove_debug_admin` — Revoke a user's admin permissions.\n\n" +
                    "**Admin Feed Channel**\n" +
                    "• `/settings admin_feed_channel set` — Register a channel to receive admin logs and events.\n" +
                    "• `/settings admin_feed_channel remove` — Stop posting admin logs.\n\n" +
                    "**Leaderboard Channel**\n" +
                    "• `/settings leaderboard_channel set` — Register a channel for the UT2004 leaderboard LiveView. Pick a default category (General, iCTF, TAM, iBR).\n" +
                    "• `/settings leaderboard_channel remove` — Unregister a leaderboard channel.\n\n" +
                    "**Tournament LiveView Channels** *(per tournament)*\n" +
                    "• `/settings matches_channel_id set/remove` — Channel for match/challenge LiveView.\n" +
                    "• `/settings standings_channel_id set/remove` — Channel for standings LiveView.\n" +
                    "• `/settings teams_channel_id set/remove` — Channel for teams LiveView.\n\n" +
                    "**FTP Stats Service**\n" +
                    "• `/settings ftp_stats_service run_setup` — Re-run FTP credential setup in the console.\n" +
                    "• `/settings ftp_stats_service remove_credentials` — Remove stored FTP credentials.\n" +
                    "• `/settings ftp_stats_service cancel_setup` — Cancel an in-progress FTP setup.\n\n" +
                    "**UT2004 Admin**\n" +
                    "• `/settings ut2004 register_guid` — Register a GUID to another member's profile.\n" +
                    "• `/settings ut2004 remove_guid` — Remove a GUID from a member's profile.\n" +
                    "• `/settings ut2004 get_logs_by_id` — Retrieve stat logs by ID.\n" +
                    "• `/settings ut2004 ignore_logs_by_id` — Mark logs to be ignored.\n" +
                    "• `/settings ut2004 allow_logs_by_id` — Un-ignore previously ignored logs.\n" +
                    "• `/settings ut2004 last_stat_logs` — View the most recent stat logs.\n" +
                    "• `/settings ut2004 stat_logs_by_date` — Query stat logs by date range.\n" +
                    "• `/settings ut2004 tag_log` — Tag a stat log to a tournament match.\n" +
                    "• `/settings ut2004 untag_log` — Remove a stat log's tournament match tag.")
                .WithColor(Color.DarkGrey)
                .WithFooter("Flaws Fight Night — Help · Settings & Admin")
                .WithCurrentTimestamp()
                .Build();
        }

        private Embed HelpUT2004StatsEmbed()
        {
            return new EmbedBuilder()
                .WithTitle("📊 UT2004 Stats Commands")
                .WithDescription(
                    "All UT2004 stat commands live under `/stats ut2004`. Requires a registered member profile.\n\n" +
                    "**GUID Registration**\n" +
                    "• `/stats ut2004 register_guid` — Link your UT2004 GUID to your Discord account. This is how the bot knows which player you are.\n" +
                    "• `/stats ut2004 remove_guid` — Unlink a GUID from your account.\n\n" +
                    "**Player Profile**\n" +
                    "• `/stats ut2004 my_player` — View your full UT2004 profile with an interactive dropdown (General, iCTF, TAM, iBR sections).\n\n" +
                    "**Leaderboard**\n" +
                    "• `/stats ut2004 leaderboard` — Interactive leaderboard with a dropdown to switch categories. Shows top 15 players per category.\n\n" +
                    "**Player Comparison**\n" +
                    "• `/stats ut2004 compare` — Side-by-side comparison of two players with win probability. Dropdown to switch between Overview, iCTF, TAM, and iBR.\n\n" +
                    "**Team Suggestions**\n" +
                    "• `/stats ut2004 suggest_teams` — Given 4–10 players and a game mode, suggests balanced teams based on OpenSkill ratings (μ−3σ).\n\n" +
                    "**Win Probability**\n" +
                    "• `/stats ut2004 win_probability` — Calculates win probability between two tournament teams based on UT2004 OpenSkill ratings.\n\n" +
                    "**Match Data**\n" +
                    "• `/stats ut2004 match_summary` — View a stat log's full match summary by ID.\n" +
                    "• `/stats ut2004 my_tournament_matches` — List all your tournament-tagged match log IDs.\n" +
                    "• `/stats ut2004 request_all_matches` — Request all match log IDs containing your GUID (sent via DM).\n\n" +
                    "**Tournament Stats**\n" +
                    "• `/stats tournament my_profile` — View your tournament profile with win/loss record across all tournaments.")
                .WithColor(new Color(0xFF6A00))
                .WithFooter("Flaws Fight Night — Help · UT2004 Stats")
                .WithCurrentTimestamp()
                .Build();
        }

        private Embed HelpTournamentTypesEmbed()
        {
            return new EmbedBuilder()
                .WithTitle("📋 Tournament Types Explained")
                .WithDescription(
                    "Flaws Fight Night supports four tournament types. Choose wisely — the type **cannot be changed** after creation.\n\n" +
                    "────────────────────────")
                .AddField("🏅 Normal Ladder",
                    "• Challenge-based: teams challenge others ranked up to **2 spots above** them.\n" +
                    "• **Winner takes the loser's rank**; loser drops down one.\n" +
                    "• Cannot challenge below your rank.\n" +
                    "• One active challenge per team at a time.\n" +
                    "• Great for ongoing competitive seasons.", false)
                .AddField("⭐ DSR Ladder",
                    "• Challenge-based: teams can challenge **any rank**, up or down.\n" +
                    "• Uses an **ELO-inspired rating system** instead of rank swaps.\n" +
                    "• Ranks re-sort by rating after each match.\n" +
                    "• Rating changes depend on the difference between teams.\n" +
                    "• One active challenge per team at a time.\n" +
                    "• Best for competitive environments where skill rating matters.", false)
                .AddField("🔄 Normal Round Robin",
                    "• Classic round structure: all teams play their match before advancing.\n" +
                    "• Supports **Single** (play once) or **Double** (play twice) round robin.\n" +
                    "• Configurable **tie-breaker rules** (Traditional by default).\n" +
                    "• Traditional tie-breaking: head-to-head → point differential → total points vs tied → total points overall → least points against → coinflip.\n" +
                    "• Rounds must be **locked** before advancing.\n" +
                    "• Bye matches are auto-handled for odd team counts.", false)
                .AddField("🔓 Open Round Robin",
                    "• No rounds or bye matches — teams report matches **in any order**.\n" +
                    "• Same tie-breaker rules and single/double options as Normal RR.\n" +
                    "• More flexible scheduling; teams can play back-to-back if needed.\n" +
                    "• Best when scheduling is difficult or timezones vary.", false)
                .WithColor(Color.Purple)
                .WithFooter("Flaws Fight Night — Help · Tournament Types")
                .WithCurrentTimestamp()
                .Build();
        }

        private Embed HelpLiveViewEmbed()
        {
            return new EmbedBuilder()
                .WithTitle("📺 LiveView System")
                .WithDescription(
                    "LiveViews are auto-updating Discord messages that keep channels current without manual effort.\n\n" +
                    "**How It Works:**\n" +
                    "When you assign a channel via `/settings`, the bot posts an embed that it **continuously updates** on a cycle. The message stays pinned at the bottom of the channel.\n\n" +
                    "────────────────────────")
                .AddField("⚔️ Matches LiveView",
                    "• Set with `/settings matches_channel_id set`\n" +
                    "• Shows pending challenges / matches to play and completed match results.\n" +
                    "• Updates after every `/match report-win` or `/match challenge send`.", false)
                .AddField("📊 Standings LiveView",
                    "• Set with `/settings standings_channel_id set`\n" +
                    "• Shows current rankings, win/loss records, streaks, and ratings.\n" +
                    "• Ladder tournaments show challenge status; RR shows points for/against.", false)
                .AddField("👥 Teams LiveView",
                    "• Set with `/settings teams_channel_id set`\n" +
                    "• Shows all registered teams, their members, ranks, and challenge status (ladders).", false)
                .AddField("🏆 Leaderboard LiveView",
                    "• Set with `/settings leaderboard_channel set`\n" +
                    "• UT2004 player leaderboard with an interactive dropdown (General, iCTF, TAM, iBR).\n" +
                    "• Choose a default view that resets on each refresh cycle.", false)
                .AddField("📡 Admin Feed",
                    "• Set with `/settings admin_feed_channel set`\n" +
                    "• Receives structured log embeds for errors, warnings, and key events.\n" +
                    "• Color-coded by severity (🔵 Info, ⚠️ Warning, 🔴 Error, 🚨 Critical).", false)
                .AddField("💡 Tips",
                    "• Each tournament can have its own set of LiveView channels.\n" +
                    "• Use `/settings <channel_type> remove` to stop a LiveView.\n" +
                    "• The bot needs **Send Messages** and **Manage Messages** permissions in the target channel.", false)
                .WithColor(Color.Teal)
                .WithFooter("Flaws Fight Night — Help · LiveView System")
                .WithCurrentTimestamp()
                .Build();
        }

        #endregion
    }
}