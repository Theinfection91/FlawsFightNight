using Discord;
using Discord.Interactions;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.MatchLogs;
using FlawsFightNight.Core.Models.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class EmbedManager
    {
        public EmbedManager() { }

        public Embed ToDoEmbed(string message = "This feature is not yet implemented.")
        {
            var embed = new EmbedBuilder()
                .WithTitle("🚧 Work In Progress")
                .WithDescription(message)
                .WithColor(Color.Orange)
                .WithFooter("Feature coming soon!")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed ErrorEmbed(string commandName, string message = "An unexpected error occurred.")
        {
            var embed = new EmbedBuilder()
                .WithTitle($"⚠️ {commandName} Error")
                .WithDescription(message)
                .WithColor(Color.Red)
                .WithFooter("Please try again after correcting.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        #region Debug Admin Embeds
        public Embed DebugAdminAddSuccess(ulong userId)
        {
            var embed = new EmbedBuilder()
                .WithTitle("✅ Debug Admin Added Successfully")
                .WithDescription($"The user with ID **{userId}** has been successfully added as a Debug Admin.")
                .WithColor(Color.Green)
                .WithFooter("The user now has special permissions.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed DebugAdminRemoveSuccess(ulong userId)
        {
            var embed = new EmbedBuilder()
                .WithTitle("✅ Debug Admin Removed Successfully")
                .WithDescription($"The user with ID **{userId}** has been successfully removed from the Debug Admin list.")
                .WithColor(Color.Green)
                .WithFooter("The user no longer has special permissions.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }
        #endregion

        #region LiveView Embeds
        public Embed MatchesLiveViewResolver(Tournament tournament)
        {
            if (tournament is DSRLadderTournament dsrLadderTournament)
            {
                return DSRLadderMatchesLiveView(dsrLadderTournament);
            }
            if (tournament is NormalLadderTournament ladderTournament)
            {
                return NormalLadderMatchesLiveView(ladderTournament);
            }
            if (tournament is NormalRoundRobinTournament normalRoundRobinTournament)
            {
                return RoundRobinNormalMatchesLiveView(normalRoundRobinTournament);
            }
            if (tournament is OpenRoundRobinTournament openRoundRobinTournament)
            {
                return RoundRobinOpenMatchesLiveView(openRoundRobinTournament);
            }
            else
            {
                return ToDoEmbed();
            }
        }

        private Embed DSRLadderMatchesLiveView(DSRLadderTournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"⚔️ {tournament.Name} - {tournament.TeamSizeFormat} DSR Ladder Tournament Challenge Matches")
                .WithDescription($"*ID#: {tournament.Id}*\n**Total Teams: {tournament.Teams.Count}**\n")
                .WithColor(Color.Orange)
                .WithCurrentTimestamp();
            // --- Pending Challenges ---
            if (tournament.MatchLog.GetAllActiveMatches().Count > 0)
            {
                var challenges = tournament.MatchLog.GetAllActiveMatches()
                    .Select(m => $"⚔️ *Match ID#: {m.Id}* | " +
                                  $"(#{m.Challenge.ChallengerRank} - Rating: {m.Challenge.ChallengerRating}) **{m.Challenge.Challenger}** " +
                                  $"has challenged (#{m.Challenge.ChallengedRank} - Rating: {m.Challenge.ChallengedRating}) **{m.Challenge.Challenged}**")
                    .ToList();
                AddMatchesInPages(embed, "⚔️ Pending Challenges", challenges);
            }
            else
            {
                embed.AddField("⚔️ Pending Challenges", "No pending challenges at the moment.", false);
            }
            // --- Previous Matches ---
            if (tournament.MatchLog.GetAllPostMatches().Count > 0)
            {
                var matches = tournament.MatchLog.GetAllPostMatches()
                    .Select(pm => $"✅ *Match ID#: {pm.Id}* | " +
                                  $"**{pm.Winner}** defeated **{pm.Loser}** " +
                                  $"by **{pm.WinnerScore}** to **{pm.LoserScore}** " +
                                  $"\n{pm.GetRatingChangeText()}")
                    .ToList();
                AddMatchesInPages(embed, "📜 Previous Matches (Oldest to Newest)", matches);
            }
            else
            {
                embed.AddField("📜 Previous Matches", "No matches completed yet.", false);
            }
            return embed.Build();
        }

        private Embed NormalLadderMatchesLiveView(NormalLadderTournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"⚔️ {tournament.Name} - {tournament.TeamSizeFormat} Ladder Tournament Challenge Matches")
                .WithDescription($"*ID#: {tournament.Id}*\n**Total Teams: {tournament.Teams.Count}**\n")
                .WithColor(Color.Orange)
                .WithCurrentTimestamp();
            // --- Pending Challenges ---
            if (tournament.MatchLog.GetAllActiveMatches().Count > 0)
            {
                var challenges = tournament.MatchLog.GetAllActiveMatches()
                    .Select(m => $"⚔️ *Match ID#: {m.Id}* | " +
                                  $"(#{m.Challenge.ChallengerRank}) **{m.Challenge.Challenger}** " +
                                  $"has challenged (#{m.Challenge.ChallengedRank}) **{m.Challenge.Challenged}**")
                    .ToList();
                AddMatchesInPages(embed, "⚔️ Pending Challenges", challenges);
            }
            else
            {
                embed.AddField("⚔️ Pending Challenges", "No pending challenges at the moment.", false);
            }
            // --- Previous Matches ---
            if (tournament.MatchLog.GetAllPostMatches().Count > 0)
            {
                var matches = tournament.MatchLog.GetAllPostMatches()
                    .Select(pm => $"✅ *Match ID#: {pm.Id}* | " +
                                  $"**{pm.Winner}** defeated **{pm.Loser}** " +
                                  $"by **{pm.WinnerScore}** to **{pm.LoserScore}** " +
                                  $"\n{pm.GetRankTransitionText()}\n")
                    .ToList();
                AddMatchesInPages(embed, "📜 Previous Matches (Oldest to Newest)", matches);
            }
            else
            {
                embed.AddField("📜 Previous Matches", "No matches completed yet.", false);
            }
            return embed.Build();
        }

        private Embed RoundRobinOpenMatchesLiveView(OpenRoundRobinTournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"⚔️ {tournament.Name} - {tournament.TeamSizeFormat} Open Round Robin Tournament Matches")
                .WithDescription($"*ID#: {tournament.Id}*\n**")
                .WithColor(Color.Orange)
                .WithCurrentTimestamp();

            // --- Matches To Play ---
            if (tournament.MatchLog.GetAllActiveMatches().Count > 0)
            {
                var normalMatches = tournament.MatchLog.GetAllActiveMatches()
                    .Where(m => !m.IsByeMatch)
                    .Select(m => $"🔹 *Match ID#: {m.Id}* | **{m.TeamA}** vs **{m.TeamB}**");

                var byeMatches = tournament.MatchLog.GetAllActiveMatches()
                    .Where(m => m.IsByeMatch)
                    .Select(m => $"💤 *Match ID#: {m.Id}* | *{m.GetCorrectByeNameForByeMatch()} Bye Match*");

                var orderedMatches = normalMatches.Concat(byeMatches).ToList();

                AddMatchesInPages(embed, "⚔️ Matches To Play", orderedMatches);
            }
            else
            {
                embed.AddField("⚔️ Matches To Play", "No matches left to play ✅", false);
            }

            // --- Previous Matches ---
            if (tournament.MatchLog.GetAllPostMatches().Count > 0)
            {
                var matches = tournament.MatchLog.GetAllPostMatches()
                    .Select(pm => pm.WasByeMatch
                        ? $"💤 *{pm.Winner} Bye Match*"
                        : $"✅ *Match ID#: {pm.Id}* | " +
                          $"**{pm.Winner}** defeated **{pm.Loser}** " +
                          $"by **{pm.WinnerScore}** to **{pm.LoserScore}**")
                    .ToList();

                AddMatchesInPages(embed, "📜 Previous Matches", matches);
            }
            else
            {
                embed.AddField("📜 Previous Matches", "No matches completed yet.", false);
            }

            return embed.Build();
        }

        /// <summary>
        /// Splits a list of match strings into pages of 15 per embed field.
        /// </summary>
        private void AddMatchesInPages(EmbedBuilder embed, string fieldName, List<string> matches)
        {
            const int maxFieldLength = 1024;
            var currentChunk = new StringBuilder();
            int pageIndex = 0;

            foreach (var match in matches)
            {
                if (currentChunk.Length + match.Length + 1 > maxFieldLength)
                {
                    // Add previous chunk
                    embed.AddField(pageIndex == 0 ? fieldName : $"{fieldName} (cont.)", currentChunk.ToString(), false);
                    pageIndex++;
                    currentChunk.Clear();
                }

                if (currentChunk.Length > 0) currentChunk.Append("\n");
                currentChunk.Append(match);
            }

            if (currentChunk.Length > 0)
                embed.AddField(pageIndex == 0 ? fieldName : $"{fieldName} (cont.)", currentChunk.ToString(), false);
        }


        private Embed RoundRobinNormalMatchesLiveView(NormalRoundRobinTournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"⚔️ {tournament.Name} - {tournament.TeamSizeFormat} Normal Round Robin Tournament Matches")
                .WithDescription($"*ID#: {tournament.Id}*\n**Round {tournament.CurrentRound}/{tournament.TotalRounds ?? 0}**\n")
                .WithColor(Color.Orange)
                .WithCurrentTimestamp();

            if (tournament.IsRoundComplete && tournament.IsRoundLockedIn && !tournament.CanEnd())
            {
                embed.AddField("🔒 Locked", "Round is locked and ready to advance.", true);
            }
            if (tournament.IsRoundComplete && tournament.IsRoundLockedIn && tournament.CanEnd())
            {
                embed.AddField("🔒 Locked - Ready to end tournament 🏅", "Round is locked and the tournament is ready to end have the results locked in.", true);
            }
            if (tournament.IsRoundComplete && !tournament.IsRoundLockedIn)
            {
                embed.AddField("🔓 Unlocked", "Round is finished but unlocked. Lock to finalize results then advance.\n\nBye matches will be automatically reported on a successful use of the `/tournament next-round` command.", true);
            }

            // --- Matches To Play ---
            if ((tournament.MatchLog as NormalRoundRobinMatchLog).MatchesToPlayByRound.TryGetValue(tournament.CurrentRound, out var matchesToPlay)
                && matchesToPlay.Count > 0)
            {
                var sb = new StringBuilder();

                // Normal matches first
                foreach (var match in matchesToPlay.Where(m => !m.IsByeMatch))
                {
                    sb.AppendLine($"🔹 *Match ID#: {match.Id}* | **{match.TeamA}** vs **{match.TeamB}**");
                }

                // Bye matches after
                foreach (var match in matchesToPlay.Where(m => m.IsByeMatch))
                {
                    sb.AppendLine($"💤 *{match.GetCorrectByeNameForByeMatch()} Bye Match*");
                }

                embed.AddField($"⚔️ Matches To Play (Round {tournament.CurrentRound})", sb.ToString(), false);
            }
            else
            {
                embed.AddField("⚔️ Matches To Play", "No matches left to play this round ✅", false);
            }


            // --- Past Matches (grouped by round) ---
            if ((tournament.MatchLog as NormalRoundRobinMatchLog).PostMatchesByRound.Count > 0)
            {
                foreach (var round in (tournament.MatchLog as NormalRoundRobinMatchLog).PostMatchesByRound.OrderBy(kvp => kvp.Key))
                {
                    var sb = new StringBuilder();
                    foreach (var postMatch in round.Value.OrderBy(pm => pm.CompletedOn))
                    {
                        if (postMatch.WasByeMatch)
                            sb.AppendLine($"💤 *{postMatch.Winner} Bye Match*");
                        else
                            sb.AppendLine($"✅ *Match ID#: {postMatch.Id}* | " + $"**{postMatch.Winner}** defeated **{postMatch.Loser}** " + $"by **{postMatch.WinnerScore}** to **{postMatch.LoserScore}**");
                    }

                    embed.AddField($"📜 Previous Matches - Round {round.Key}", sb.ToString(), false);
                }
            }
            else
            {
                embed.AddField("📜 Previous Matches", "No matches completed yet.", false);
            }

            return embed.Build();
        }

        public Embed StandingsLiveViewResolver(Tournament tournament)
        {
            if (tournament is NormalLadderTournament)
            {
                return LadderStandingsLiveView(tournament);
            }
            if (tournament is NormalRoundRobinTournament or OpenRoundRobinTournament)
            {
                return RoundRobinStandingsLiveView(tournament);
            }
            else
            {
                return ToDoEmbed();
            }
        }

        public Embed LadderStandingsLiveView(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"📊 {tournament.Name} - {tournament.TeamSizeFormat} Ladder Tournament Standings")
                .WithDescription($"*ID#: {tournament.Id}*\n**Total Teams: {tournament.Teams.Count}**\n")
                .WithColor(Color.Gold)
                .WithCurrentTimestamp();
            if (tournament.Teams.Count == 0)
            {
                embed.Description += "\n_No teams registered._";
                return embed.Build();
            }
            foreach (var team in tournament.Teams.OrderBy(e => e.Rank))
            {
                //var (pointsFor, pointsAgainst) = tournament.MatchLog.GetPointsForAndPointsAgainstForTeam(team.Name);
                embed.Description +=
                    $"\n#{team.Rank} **{team.Name}**\n" +
                    $"✅ Wins: {team.Wins} | " +
                    $"❌ Losses: {team.Losses} | " +
                    $"{team.GetCorrectStreakEmoji()} W/L Streak: {team.GetFormattedStreakString()}\n" +
                    //$"⭐ Points For: {pointsFor} | " +
                    //$"🛡️ Points Against: {pointsAgainst}\n" +
                    $"Challenge Status: {team.GetFormattedChallengeStatus()}\n";
            }
            return embed.Build();
        }

        public Embed RoundRobinStandingsLiveView(Tournament tournament)
        {
            string displayRoundInfo = string.Empty;
            if (tournament is IRoundBased roundBasedTournament)
            {
                displayRoundInfo = $"**Round {roundBasedTournament.CurrentRound}/{roundBasedTournament.TotalRounds ?? 0}**\n";
            }
            var embed = new EmbedBuilder()
                .WithTitle($"📊 {tournament.Name} - {tournament.TeamSizeFormat} {tournament.GetFormattedType()} Tournament Standings")
                .WithDescription($"*ID#: {tournament.Id}*\n{displayRoundInfo}")
                .WithColor(Color.Gold)
                .WithCurrentTimestamp();

            if (tournament.Teams.Count == 0)
            {
                embed.Description += "\n_No teams registered._";
                return embed.Build();
            }

            foreach (var team in tournament.Teams.OrderBy(e => e.Rank))
            {
                var (pointsFor, pointsAgainst) = tournament.MatchLog.GetPointsForAndAgainst(team.Name);

                embed.Description +=
                    $"\n#{team.Rank} **{team.Name}**\n" +
                    $"✅ Wins: {team.Wins} | " +
                    $"❌ Losses: {team.Losses} | " +
                    $"{team.GetCorrectStreakEmoji()} W/L Streak: {team.GetFormattedStreakString()}\n" +
                    $"⭐ Points For: {pointsFor} | " +
                    $"🛡️ Points Against: {pointsAgainst}\n";
            }

            return embed.Build();
        }

        public Embed TeamsLiveView(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"👥 {tournament.Name} - {tournament.TeamSizeFormat} Tournament Teams")
                .WithDescription($"*ID#: {tournament.Id}*\n**Total Teams: {tournament.Teams.Count}**\n")
                .WithColor(Color.Blue)
                .WithCurrentTimestamp();
            if (tournament.Teams.Count == 0)
            {
                embed.Description += "\n_No teams have been registered yet._";
                return embed.Build();
            }

            foreach (var team in tournament.Teams)
            {
                if (team != null)
                {
                    embed.Description +=
                        $"\n**{team.Name}** (#{team.Rank})\n" +
                        $"👤 Members: {string.Join(", ", team.Members.Select(m => m.DisplayName))}\n";
                    // If Ladder Tournament, display challenge status
                    if (tournament.Type == TournamentType.NormalLadder || tournament.Type == TournamentType.DSRLadder)
                    {
                        embed.Description += $"Challenge Status: {team.GetFormattedChallengeStatus()}\n";
                    }
                }
            }
            return embed.Build();
        }

        #endregion

        #region Match Embeds

        public Embed RoundRobinEditMatchSuccess(Tournament tournament, PostMatch match)
        {
            var embed = new EmbedBuilder()
                .WithTitle("✏️ Match Edited Successfully")
                .WithDescription($"The match between '**{match.Winner}**' and '**{match.Loser}**' has been successfully edited in **{tournament.Name}**.")
                .AddField("Winning Team (Score)", $"{match.Winner} ({match.WinnerScore})")
                .AddField("Losing Team (Score)", $"{match.Loser} ({match.LoserScore})")
                .AddField("Tournament ID", tournament.Id)
                .WithColor(Color.Green)
                .WithFooter("Match edited successfully.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed ReportWinSuccess(Tournament tournament, Match match, Team winningTeam, int winningTeamScore, Team losingTeam, int losingTeamScore, bool isGuildAdminReporting)
        {
            string reporterText = isGuildAdminReporting
                ? "An **admin** reported this match result."
                : "The match result was reported normally.";

            var embed = new EmbedBuilder()
                .WithTitle("🏆 Match Result Reported")
                .WithDescription(
                    $"The match '**{match.TeamA} vs {match.TeamB}**' has been recorded in **{tournament.Name}**.\n\n{reporterText}"
                )
                .AddField("Winning Team (Score)", $"{winningTeam.Name} ({winningTeamScore})", true)
                .AddField("Losing Team (Score)", $"{losingTeam.Name} ({losingTeamScore})", true)
                .AddField("Tournament ID", tournament.Id, false)
                .AddField("Match ID", match.Id, false)
                .WithColor(Color.Green)
                .WithFooter("Match result reported successfully.")
                .WithTimestamp(DateTimeOffset.Now);

            return embed.Build();
        }

        #endregion

        #region Challenge Embeds
        public Embed SendChallengeSuccess(Tournament tournament, Match match, bool isGuildAdminReporting)
        {
            string reporterText = isGuildAdminReporting
                ? "An **admin** sent this challenge."
                : "This challenge was sent normally.";

            var embed = new EmbedBuilder()
                .WithTitle("🏅 Challenge Sent Successfully")
                .WithDescription($"The challenge from (#{match.Challenge.ChallengerRank})**{match.Challenge.Challenger}** to (#{match.Challenge.ChallengedRank})**{match.Challenge.Challenged}** has been successfully sent in the tournament **{tournament.Name}**!\n\n{reporterText}")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Match ID", match.Id)
                .AddField("Challenger Team", match.Challenge.Challenger)
                .AddField("Challenger Rank", $"#{match.Challenge.ChallengerRank}", true)
                .AddField("Challenged Team", match.Challenge.Challenged)
                .AddField("Challenged Rank", $"#{match.Challenge.ChallengedRank}", true)
                .WithColor(Color.Green)
                .WithFooter("Good luck to both teams!")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed CancelChallengeSuccess(Tournament tournament, Match match, bool isGuildAdminReporting)
        {
            string reporterText = isGuildAdminReporting
                ? "An **admin** canceled this challenge."
                : "This challenge was canceled normally.";
            var embed = new EmbedBuilder()
                .WithTitle("🗑️ Challenge Canceled Successfully")
                .WithDescription($"The challenge from (#{match.Challenge.ChallengerRank})**{match.Challenge.Challenger}** to (#{match.Challenge.ChallengedRank})**{match.Challenge.Challenged}** has been successfully canceled in the tournament **{tournament.Name}**.\n\n{reporterText}")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Challenger Team", match.Challenge.Challenger)
                .AddField("Challenged Team", match.Challenge.Challenged)
                .WithColor(Color.Green)
                .WithFooter("The challenge has been canceled.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }
        #endregion

        #region Set LiveView Embeds
        public Embed SetMatchesChannelSuccess(IMessageChannel channel, Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("✅ Matches Channel Set Successfully")
                .WithDescription($"The matches/challenges channel for the tournament **{tournament.Name}** has been successfully set to {channel.Name} (ID#: {channel.Id}).")
                .AddField("Tournament ID", tournament.Id)
                .WithColor(Color.Green)
                .WithFooter("The Matches/Challenges LiveView will now be posted in this channel.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed RemoveMatchesChannelSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("✅ Matches Channel Removed Successfully")
                .WithDescription($"The matches/challenges channel for the tournament **{tournament.Name}** has been successfully removed. No channel is currently set.")
                .AddField("Tournament ID", tournament.Id)
                .WithColor(Color.Green)
                .WithFooter("The Matches/Challenges LiveView will no longer be posted in a channel.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed SetStandingsChannelSuccess(IMessageChannel channel, Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("✅ Standings Channel Set Successfully")
                .WithDescription($"The standings channel for the tournament **{tournament.Name}** has been successfully set to {channel.Name} (ID#: {channel.Id}).")
                .AddField("Tournament ID", tournament.Id)
                .WithColor(Color.Green)
                .WithFooter("The Standings LiveView will now be posted in this channel.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed RemoveStandingsChannelSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("✅ Standings Channel Removed Successfully")
                .WithDescription($"The standings channel for the tournament **{tournament.Name}** has been successfully removed. No channel is currently set.")
                .AddField("Tournament ID", tournament.Id)
                .WithColor(Color.Green)
                .WithFooter("The Standings LiveView will no longer be posted in a channel.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed SetTeamsChannelSuccess(IMessageChannel channel, Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("✅ Teams Channel Set Successfully")
                .WithDescription($"The teams channel for the tournament **{tournament.Name}** has been successfully set to {channel.Name} (ID#: {channel.Id}).")
                .AddField("Tournament ID", tournament.Id)
                .WithColor(Color.Green)
                .WithFooter("The Teams LiveView will now be posted in this channel.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed RemoveTeamsChannelSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("✅ Teams Channel Removed Successfully")
                .WithDescription($"The teams channel for the tournament **{tournament.Name}** has been successfully removed. No channel is currently set.")
                .AddField("Tournament ID", tournament.Id)
                .WithColor(Color.Green)
                .WithFooter("The Teams LiveView will no longer be posted in a channel.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }
        #endregion

        #region Direct Message Notification Embeds
        public Embed NormalRoundRobinMatchScheduleNotification(NormalRoundRobinTournament tournament, List<Match> matches, string userName, ulong discordId, string teamName)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"📅 {tournament.Name} - {tournament.TeamSizeFormat} Round Robin Schedule")
                .WithColor(Color.Gold)
                .AddField("👥 Team", teamName, true)
                .AddField("🏷️ Tournament ID", tournament.Id, true)
                .AddField("🎯 Total Rounds", tournament.TotalRounds?.ToString() ?? "N/A", true);

            var sb = new StringBuilder();
            sb.AppendLine("**Your Match Schedule:**");
            sb.AppendLine("────────────────────────");

            foreach (var match in matches.OrderBy(m => m.RoundNumber))
            {
                if (match.IsByeMatch)
                {
                    sb.AppendLine($"`Round {match.RoundNumber}` - *Bye Match* 💤");
                }
                else
                {
                    sb.AppendLine($"`Round {match.RoundNumber}` - **{match.TeamA}** vs. **{match.TeamB}**");
                }
            }

            embed.WithDescription(sb.ToString())
                .WithFooter($"Scheduled for: {userName}")
                .WithTimestamp(DateTimeOffset.Now);

            return embed.Build();
        }

        public Embed OpenRoundRobinMatchScheduleNotification(Tournament tournament, List<Match> matches, string userName, ulong discordId, string teamName)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"📅 {tournament.Name} - {tournament.TeamSizeFormat} Round Robin Schedule")
                .WithColor(Color.Gold)
                .AddField("👥 Team", teamName, true)
                .AddField("🏷️ Tournament ID", tournament.Id, true);

            var sb = new StringBuilder();
            sb.AppendLine("**Your Match Schedule:**");
            sb.AppendLine("────────────────────────");

            foreach (var match in matches.OrderBy(m => m.RoundNumber))
            {
                if (match.IsByeMatch)
                {
                    sb.AppendLine($"*Bye Match* 💤");
                }
                else
                {
                    sb.AppendLine($"**{match.TeamA}** vs. **{match.TeamB}**");
                }
            }

            embed.WithDescription(sb.ToString())
                .WithFooter($"Scheduled for: {userName}")
                .WithTimestamp(DateTimeOffset.Now);

            return embed.Build();
        }

        public Embed SendLadderChallengeMatchNotificationResolver(Tournament tournament, Team challengerTeam, Team challengedTeam, bool isChallenger)
        {
            if (tournament is NormalLadderTournament)
            {
                return NormalLadderSendChallengeMatchNotification(tournament, challengerTeam, challengedTeam, isChallenger);
            }
            if (tournament is DSRLadderTournament)
            {
                return DSRLadderSendChallengeMatchNotification(tournament, challengerTeam, challengedTeam, isChallenger);
            }
            else
            {
                return ToDoEmbed();
            }
        }

        public Embed CancelLadderChallengeMatchNotificationResolver(Tournament tournament, Team challengerTeam, Team challengedTeam, bool isChallenger)
        {
            if (tournament is NormalLadderTournament)
            {
                return NormalLadderCancelChallengeMatchNotification(tournament, challengerTeam, challengedTeam, isChallenger);
            }
            if (tournament is DSRLadderTournament)
            {
                return DSRLadderCancelChallengeMatchNotification(tournament, challengerTeam, challengedTeam, isChallenger);
            }
            else
            {
                return ToDoEmbed();
            }
        }

        private Embed NormalLadderSendChallengeMatchNotification(Tournament tournament, Team challengerTeam, Team challengedTeam, bool isChallenger)
        {
            switch (isChallenger)
            {
                case true:
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle($"🏅 {tournament.Name} - {tournament.TeamSizeFormat} {tournament.GetFormattedType()} Challenge Notification")
                            .WithColor(Color.Gold)
                            .WithDescription($"Your team (#{challengerTeam.Rank})**{challengerTeam.Name}** has successfully sent a challenge to (#{challengedTeam.Rank})**{challengedTeam.Name}**!\n\nGood luck!")
                            .AddField("👥 Your Team", challengerTeam.Name, true)
                            .AddField("🏷️ Tournament ID", tournament.Id, true)
                            .AddField("🏆 Challenged Team", challengedTeam.Name, true)
                            .WithFooter("Challenge Sent")
                            .WithTimestamp(DateTimeOffset.Now);
                        return embed.Build();
                    }
                case false:
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle($"🏅 {tournament.Name} - {tournament.TeamSizeFormat} {tournament.GetFormattedType()} Challenge Notification")
                            .WithColor(Color.Gold)
                            .WithDescription($"Your team (#{challengedTeam.Rank})**{challengedTeam.Name}** has received a challenge from (#{challengerTeam.Rank})**{challengerTeam.Name}**!\n\nGood luck!")
                            .AddField("👥 Your Team", challengedTeam.Name, true)
                            .AddField("🏷️ Tournament ID", tournament.Id, true)
                            .AddField("🏆 Challenger Team", challengerTeam.Name, true)
                            .WithFooter("Challenge Received")
                            .WithTimestamp(DateTimeOffset.Now);
                        return embed.Build();
                    }
            }
        }

        private Embed NormalLadderCancelChallengeMatchNotification(Tournament tournament, Team challengerTeam, Team challengedTeam, bool isChallenger)
        {
            switch (isChallenger)
            {
                case true:
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle($"🗑️ {tournament.Name} - {tournament.TeamSizeFormat} {tournament.GetFormattedType()} Challenge Canceled")
                            .WithColor(Color.Orange)
                            .WithDescription($"Your team (#{challengerTeam.Rank})**{challengerTeam.Name}** has canceled the challenge to (#{challengedTeam.Rank})**{challengedTeam.Name}**.")
                            .AddField("👥 Your Team", challengerTeam.Name, true)
                            .AddField("🏷️ Tournament ID", tournament.Id, true)
                            .AddField("🏆 Challenged Team", challengedTeam.Name, true)
                            .WithFooter("Challenge Canceled")
                            .WithTimestamp(DateTimeOffset.Now);
                        return embed.Build();
                    }
                case false:
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle($"🗑️ {tournament.Name} - {tournament.TeamSizeFormat} {tournament.GetFormattedType()} Challenge Canceled")
                            .WithColor(Color.Orange)
                            .WithDescription($"The challenge from (#{challengerTeam.Rank})**{challengerTeam.Name}** to your team (#{challengedTeam.Rank})**{challengedTeam.Name}** has been canceled.")
                            .AddField("👥 Your Team", challengedTeam.Name, true)
                            .AddField("🏷️ Tournament ID", tournament.Id, true)
                            .AddField("🏆 Challenger Team", challengerTeam.Name, true)
                            .WithFooter("Challenge Canceled")
                            .WithTimestamp(DateTimeOffset.Now);
                        return embed.Build();
                    }
            }
        }

        private Embed DSRLadderSendChallengeMatchNotification(Tournament tournament, Team challengerTeam, Team challengedTeam, bool isChallenger)
        {
            switch (isChallenger)
            {
                case true:
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle($"🏅 {tournament.Name} - {tournament.TeamSizeFormat} {tournament.GetFormattedType()} Challenge Notification")
                            .WithColor(Color.Gold)
                            .WithDescription($"Your team [#{challengerTeam.Rank} - Rating: {challengerTeam.Rating}]**{challengerTeam.Name}** has successfully sent a challenge to [#{challengedTeam.Rank} - Rating: {challengedTeam.Rating}]**{challengedTeam.Name}**!\n\nGood luck!")
                            .AddField("👥 Your Team", challengerTeam.Name, true)
                            .AddField("🏷️ Tournament ID", tournament.Id, true)
                            .AddField("🏆 Challenged Team", challengedTeam.Name, true)
                            .WithFooter("Challenge Sent")
                            .WithTimestamp(DateTimeOffset.Now);
                        return embed.Build();
                    }
                case false:
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle($"🏅 {tournament.Name} - {tournament.TeamSizeFormat} {tournament.GetFormattedType()} Challenge Notification")
                            .WithColor(Color.Gold)
                            .WithDescription($"Your team [#{challengedTeam.Rank} - Rating: {challengedTeam.Rating}]**{challengedTeam.Name}** has received a challenge from [#{challengerTeam.Rank} - Rating: {challengerTeam.Rating}]**{challengerTeam.Name}**!\n\nGood luck!")
                            .AddField("👥 Your Team", challengedTeam.Name, true)
                            .AddField("🏷️ Tournament ID", tournament.Id, true)
                            .AddField("🏆 Challenger Team", challengerTeam.Name, true)
                            .WithFooter("Challenge Received")
                            .WithTimestamp(DateTimeOffset.Now);
                        return embed.Build();
                    }
            }
        }

        private Embed DSRLadderCancelChallengeMatchNotification(Tournament tournament, Team challengerTeam, Team challengedTeam, bool isChallenger)
        {
            switch (isChallenger)
            {
                case true:
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle($"🗑️ {tournament.Name} - {tournament.TeamSizeFormat} {tournament.GetFormattedType()} Challenge Canceled")
                            .WithColor(Color.Orange)
                            .WithDescription($"Your team [#{challengerTeam.Rank} - Rating: {challengerTeam.Rating}]**{challengerTeam.Name}** has canceled the challenge to [#{challengedTeam.Rank} - Rating: {challengedTeam.Rating}]**{challengedTeam.Name}**.")
                            .AddField("👥 Your Team", challengerTeam.Name, true)
                            .AddField("🏷️ Tournament ID", tournament.Id, true)
                            .AddField("🏆 Challenged Team", challengedTeam.Name, true)
                            .WithFooter("Challenge Canceled")
                            .WithTimestamp(DateTimeOffset.Now);
                        return embed.Build();
                    }
                case false:
                    {
                        var embed = new EmbedBuilder()
                            .WithTitle($"🗑️ {tournament.Name} - {tournament.TeamSizeFormat} {tournament.GetFormattedType()} Challenge Canceled")
                            .WithColor(Color.Orange)
                            .WithDescription($"The challenge from [#{challengerTeam.Rank} - Rating: {challengerTeam.Rating}]**{challengerTeam.Name}** to your team [#{challengedTeam.Rank} - Rating: {challengedTeam.Rating}]**{challengedTeam.Name}** has been canceled.")
                            .AddField("👥 Your Team", challengedTeam.Name, true)
                            .AddField("🏷️ Tournament ID", tournament.Id, true)
                            .AddField("🏆 Challenger Team", challengerTeam.Name, true)
                            .WithFooter("Challenge Canceled")
                            .WithTimestamp(DateTimeOffset.Now);
                        return embed.Build();
                    }
            }
        }
        #endregion

        #region Team Embeds
        public Embed TeamRegistrationSuccess(Team team, Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🎉 Team Registered Successfully")
                .WithDescription($"The team **{team.Name}** has been successfully registered in the **{tournament.GetFormattedType()}** tournament **{tournament.Name}**!")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Tournament Type", tournament.GetFormattedType())
                .AddField("Members", string.Join(", ", team.Members.Select(m => m.DisplayName)))
                .WithColor(Color.Green)
                .WithFooter("Good luck to your team!")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed TeamDeleteSuccess(Team team, Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🗑️ Team Deleted Successfully")
                .WithDescription($"The team **{team.Name}** has been successfully deleted from the tournament **{tournament.Name}**.")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Members", string.Join(", ", team.Members.Select(m => m.DisplayName)))
                .WithColor(Color.Green)
                .WithFooter("The team has been deleted.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed AddTeamLossSuccess(Team team, Tournament tournament, int numberOfLosses)
        {
            var embed = new EmbedBuilder()
                .WithTitle("❌ Team Loss Recorded Successfully")
                .WithDescription($"The team **{team.Name}** has been assigned **{numberOfLosses}** loss(es) in the tournament **{tournament.Name}**.")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Total Losses", team.Losses)
                .WithColor(Color.Green)
                .WithFooter("The team's losses have been updated.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed AddTeamWinSuccess(Team team, Tournament tournament, int numberOfWins)
        {
            var embed = new EmbedBuilder()
                 .WithTitle("✅ Team Win Recorded Successfully")
                 .WithDescription($"The team **{team.Name}** has been assigned **{numberOfWins}** win(s) in the tournament **{tournament.Name}**.")
                 .AddField("Tournament ID", tournament.Id)
                 .AddField("Total Wins", team.Wins)
                 .WithColor(Color.Green)
                 .WithFooter("The team's wins have been updated.")
                 .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed RemoveTeamWinSuccess(Team team, Tournament tournament, int numberOfWins)
        {
            var embed = new EmbedBuilder()
                 .WithTitle("✅ Team Win(s) Removed Successfully")
                 .WithDescription($"The team **{team.Name}** has had **{numberOfWins}** win(s) removed in the tournament **{tournament.Name}**.")
                 .AddField("Tournament ID", tournament.Id)
                 .AddField("Total Wins", team.Wins)
                 .WithColor(Color.Green)
                 .WithFooter("The team's wins have been updated.")
                 .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed RemoveTeamLossSuccess(Team team, Tournament tournament, int numberOfLosses)
        {
            var embed = new EmbedBuilder()
                 .WithTitle("✅ Team Loss(es) Removed Successfully")
                 .WithDescription($"The team **{team.Name}** has had **{numberOfLosses}** loss(es) removed in the tournament **{tournament.Name}**.")
                 .AddField("Tournament ID", tournament.Id)
                 .AddField("Total Losses", team.Losses)
                 .WithColor(Color.Green)
                 .WithFooter("The team's losses have been updated.")
                 .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed SetTeamRankSuccess(Team team, Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🏅 Team Rank Updated Successfully")
                .WithDescription($"The team **{team.Name}** has been assigned a new rank of **#{team.Rank}** in the tournament **{tournament.Name}**. All other ranks have been adjusted accordingly.")
                .AddField("Tournament ID", tournament.Id)
                .AddField("New Rank", $"#{team.Rank}")
                .WithColor(Color.Green)
                .WithFooter("The team's rank has been updated.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }
        #endregion

        #region Tournament Embeds
        public Embed CreateTournamentSuccessResolver(Tournament tournament)
        {
            switch (tournament.Type)
            {
                case TournamentType.DSRLadder:
                    // TODO DSR Create Tournament Embed
                    return ToDoEmbed("Need DSR Create Tournament Embed");
                case TournamentType.NormalLadder:
                    return NormalLadderCreateTournamentSuccess((NormalLadderTournament)tournament);
                case TournamentType.NormalRoundRobin:
                case TournamentType.OpenRoundRobin:
                    return RoundRobinCreateTournamentSuccess(tournament);
                default:
                    return ToDoEmbed();
            }
        }

        public Embed DeleteTournamentSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🗑️ Tournament Deleted Successfully")
                .WithDescription($"The tournament **{tournament.Name}** has been successfully deleted.")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Type", tournament.Type.ToString())
                .WithColor(Color.Green)
                .WithFooter("The tournament has been deleted.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        private Embed RoundRobinCreateTournamentSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🎉 Round Robin Tournament Created")
                .WithDescription($"A Round Robin tournament named **{tournament.Name}** has been successfully created!\n\nRemember the Tournament ID at the bottom for future commands.\n\nDefault **Tie Breaker Rules** are *'Traditional'* meaning it looks at each of the following steps and if its a tie it checks the next: head to head matches, then point differential between tied teams, then total points scored vs tied teams, then total points overall, then least points against. If all is tied it comes down to a random 'coinflip' to determine the winner.\n\nDefault **Length** is **'Double Round Robin'** meaning every team plays twice.\n\nDefault **Match Type** is *'Normal'*, meaning there will be the classic round structure of having every team play their match before the round can be advanced.\nThere is also the **Match Type** of *Open* where there are no rounds or bye matches, and teams can report any of their matches at any time. This allows more flexibility in scheduling matches, allowing teams to play their two required matches back to back if need be.\n\nTo change any of these settings, use **/tournament setup_round_robin** anytime before starting the tournament. \n\n**After the tournament starts you may not change any of these settings.\nApply setting changes now to be safe.**")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Match Format", tournament.TeamSizeFormat)
                .WithColor(Color.Green)
                .WithFooter("Let's get some teams registered to this tournament now.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        private Embed NormalLadderCreateTournamentSuccess(NormalLadderTournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🎉 Normal Ladder Tournament Created")
                .WithDescription($"A Normal Ladder tournament named **{tournament.Name}** has been successfully created!\n\nRemember the Tournament ID at the bottom for future commands.\n\nLadders are *'Challenged Based'*, meaning teams send out challenges but can only challenge teams ranked 2 spots above them, and may not challenge below their current rank. A team may only have one challenge sent out or be on the receiving end of a challenge meaning if a team has been challenge they cannot be challenge again or send out their own challenge until the intial one is resolved.")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Match Format", tournament.TeamSizeFormat)
                .WithColor(Color.Green)
                .WithFooter("Let's get some teams registered to this tournament now.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed RoundRobinSetupTournamentSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("⚙️ Tournament Setup Success")
                .WithDescription($"The Round Robin tournament **{tournament.Name}** has been successfully updated.")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Tie Breaker Rules", (tournament as ITieBreakerRankSystem)?.TieBreakerRule.Name)
                .AddField("Round Robin Type", (bool)((tournament as IRoundRobinLength)?.IsDoubleRoundRobin) ? RoundRobinLengthType.Double : RoundRobinLengthType.Single)
                .WithColor(Color.Green)
                .WithFooter("You can change the settings again anytime before starting.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed StartTournamentSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🏆 Tournament Started")
                .WithDescription($"The {tournament.GetFormattedType()} Tournament **{tournament.Name}** has been successfully started!")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Match Format", tournament.TeamSizeFormat)
                .AddField("Total Teams", tournament.Teams.Count)
                .WithColor(Color.Green)
                .WithFooter("Good luck to all teams!")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed NormalLadderEndTournamentSuccess(Tournament tournament)
        {
            // Grab top 3 teams
            Team? firstPlace = tournament.Teams.Count > 0 ? tournament.Teams.OrderBy(t => t.Rank).First() : null;
            Team? secondPlace = tournament.Teams.Count > 1 ? tournament.Teams.OrderBy(t => t.Rank).Skip(1).First() : null;
            Team? thirdPlace = tournament.Teams.Count > 2 ? tournament.Teams.OrderBy(t => t.Rank).Skip(2).First() : null;

            // Grab member names for each team
            string firstPlaceMembers = firstPlace.GetMembersAsString();
            string secondPlaceMembers = secondPlace.GetMembersAsString();
            string thirdPlaceMembers = thirdPlace.GetMembersAsString();

            var embedBuilder = new EmbedBuilder()
                .WithTitle("🏁 Ladder Ended")
                .WithColor(Color.Gold)
                .WithDescription($"The tournament **{tournament.Name}** ({tournament.TeamSizeFormat} {tournament.Type}) has officially ended.");

            if (firstPlace != null)
            {
                embedBuilder.AddField("🏆 1st Place - Winner", $"{firstPlace.Name}\n" +
                                                                   $"**Wins**: {firstPlace.Wins} | **Losses**: {firstPlace.Losses}\n" +
                                                                   $"**Members**: {firstPlaceMembers}", inline: false);
            }

            if (secondPlace != null)
            {
                embedBuilder.AddField("🥈 2nd Place", $"{secondPlace.Name}\n" +
                                                     $"**Wins**: {secondPlace.Wins} | **Losses**: {secondPlace.Losses}\n" +
                                                     $"**Members**: {secondPlaceMembers}", inline: false);
            }

            if (thirdPlace != null)
            {
                embedBuilder.AddField("🥉 3rd Place", $"{thirdPlace.Name}\n" +
                                                     $"**Wins**: {thirdPlace.Wins} | **Losses**: {thirdPlace.Losses}\n" +
                                                     $"**Members**: {thirdPlaceMembers}", inline: false);
            }

            var remainingTeams = tournament.Teams.Except(new[] { firstPlace, secondPlace, thirdPlace }).OrderBy(t => t.Rank).ToList();
            if (remainingTeams.Any())
            {
                var remainingTeamsInfo = new StringBuilder();
                foreach (var team in remainingTeams)
                {
                    string members = team.GetMembersAsString();
                    remainingTeamsInfo.AppendLine($"{team.Rank}. {team.Name} - **Wins**: {team.Wins} | **Losses**: {team.Losses} | **Members**: {members}");
                }
                embedBuilder.AddField("🔹 Other Teams", remainingTeamsInfo.ToString(), inline: false);
            }

            // Footer and timestamp
            embedBuilder.WithFooter("Thank you for participating!")
                        .WithTimestamp(DateTimeOffset.Now);
            return embedBuilder.Build();
        }

        public Embed RoundRobinEndTournamentSuccess(Tournament tournament, bool isTieBreakerNeeded = false, string tieBreakerInfo = null)
        {
            // Grab top 3 teams
            Team? firstPlace = tournament.Teams.Count > 0 ? tournament.Teams.OrderBy(t => t.Rank).First() : null;
            Team? secondPlace = tournament.Teams.Count > 1 ? tournament.Teams.OrderBy(t => t.Rank).Skip(1).First() : null;
            Team? thirdPlace = tournament.Teams.Count > 2 ? tournament.Teams.OrderBy(t => t.Rank).Skip(2).First() : null;

            // Grab member names
            string firstPlaceMembers = firstPlace?.GetMembersAsString() ?? "";
            string secondPlaceMembers = secondPlace?.GetMembersAsString() ?? "";
            string thirdPlaceMembers = thirdPlace?.GetMembersAsString() ?? "";

            var description = $"The tournament **{tournament.Name}** ({tournament.TeamSizeFormat} {tournament.GetFormattedType()}) has officially ended.";

            if (isTieBreakerNeeded)
            {
                if (!string.IsNullOrWhiteSpace(tieBreakerInfo) && tieBreakerInfo.Length < 1000)
                {
                    // Safe to display directly in embed
                    description += $"\n\n**⚠️ Tie-Breaker Details:**\n{tieBreakerInfo}";
                }
                else
                {
                    // Too long — mention that it’s attached
                    description += "\n\n**⚠️ Tie-Breaker details are too long to display and have been attached as a file below.**";
                }
            }

            var embedBuilder = new EmbedBuilder()
                .WithTitle("🏁 Ladder Ended")
                .WithColor(Color.Gold)
                .WithDescription(description);

            if (firstPlace != null)
            {
                embedBuilder.AddField("🏆 1st Place - Winner", $"{firstPlace.Name}\n" +
                    $"**Wins**: {firstPlace.Wins} | **Losses**: {firstPlace.Losses}\n" +
                    $"**Points For**: {tournament.MatchLog.GetPointsForAndAgainst(firstPlace.Name).Item1} | **Points Against**: {tournament.MatchLog.GetPointsForAndAgainst(firstPlace.Name).Item2}\n" +
                    $"**Members**: {firstPlaceMembers}", inline: false);
            }

            if (secondPlace != null)
            {
                embedBuilder.AddField("🥈 2nd Place", $"{secondPlace.Name}\n" +
                    $"**Wins**: {secondPlace.Wins} | **Losses**: {secondPlace.Losses}\n" +
                    $"**Points For**: {tournament.MatchLog.GetPointsForAndAgainst(secondPlace.Name).Item1} | **Points Against**: {tournament.MatchLog.GetPointsForAndAgainst(secondPlace.Name).Item2}\n" +
                    $"**Members**: {secondPlaceMembers}", inline: false);
            }

            if (thirdPlace != null)
            {
                embedBuilder.AddField("🥉 3rd Place", $"{thirdPlace.Name}\n" +
                    $"**Wins**: {thirdPlace.Wins} | **Losses**: {thirdPlace.Losses}\n" +
                    $"**Points For**: {tournament.MatchLog.GetPointsForAndAgainst(thirdPlace.Name).Item1} | **Points Against**: {tournament.MatchLog.GetPointsForAndAgainst(thirdPlace.Name).Item2}\n" +
                    $"**Members**: {thirdPlaceMembers}", inline: false);
            }

            var remainingTeams = tournament.Teams
                .Except(new[] { firstPlace, secondPlace, thirdPlace })
                .OrderBy(t => t.Rank)
                .ToList();

            if (remainingTeams.Any())
            {
                var remainingTeamsInfo = new StringBuilder();
                foreach (var team in remainingTeams)
                {
                    var (pointsFor, pointsAgainst) = tournament.MatchLog.GetPointsForAndAgainst(team.Name);
                    string members = team.GetMembersAsString();
                    remainingTeamsInfo.AppendLine($"{team.Rank}. {team.Name} - **Wins**: {team.Wins} | **Losses**: {team.Losses} | **Points For**: {pointsFor} | **Points Against**: {pointsAgainst} | **Members**: {members}");
                }
                embedBuilder.AddField("🔹 Other Teams", remainingTeamsInfo.ToString(), inline: false);
            }

            embedBuilder.WithFooter("Thank you for participating!")
                        .WithTimestamp(DateTimeOffset.Now);

            return embedBuilder.Build();
        }

        public Embed LockTeamsSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🔒 Teams Locked")
                .WithDescription($"The teams in the {tournament.TeamSizeFormat} tournament **{tournament.Name}** have been successfully locked. No more teams may be added or removed while locked. Unlock to make any changes, this is your last chance before starting.")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Teams", string.Join(", ", tournament.Teams.Select(m => m.Name)))
                .WithColor(Color.Green)
                .WithFooter("Teams are now locked for the tournament.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed UnlockTeamsSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🔓 Teams Unlocked")
                .WithDescription($"The teams in the {tournament.TeamSizeFormat} tournament **{tournament.Name}** have been successfully unlocked. More teams may now be registered and removal of teams is allowed again.")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Teams", string.Join(", ", tournament.Teams.Select(m => m.Name)))
                .WithColor(Color.Green)
                .WithFooter("Teams are now unlocked for the tournament.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed LockInRoundSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🔒 Round Locked In")
                .WithDescription($"The round for the tournament **{tournament.Name}** has been successfully **locked** in.")
                .AddField("Tournament ID", tournament.Id)
                .WithColor(Color.Green)
                .WithFooter("Teams can now advance to the next round. Changes can no longer be made unless you unlock the round first.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed UnlockRoundSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🔓 Round Unlocked")
                .WithDescription($"The round for the tournament **{tournament.Name}** has been successfully **unlocked**.")
                .AddField("Tournament ID", tournament.Id)
                .WithColor(Color.Green)
                .WithFooter("Teams can now make changes before locking in the round again.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed NextRoundSuccess(Tournament tournament, int currentRound)
        {
            var embed = new EmbedBuilder()
                .WithTitle("➡️ Next Round Started")
                .WithDescription($"The tournament **{tournament.Name}** has successfully advanced to the next round: **Round {currentRound}**.")
                .AddField("Tournament ID", tournament.Id)
                .WithColor(Color.Green)
                .WithFooter("Good luck to all teams in the next round!")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed ShowAllTournamentsSuccess(List<Tournament> tournaments)
        {
            var embed = new EmbedBuilder()
                .WithTitle("📋 All Tournaments")
                .WithColor(Color.Blue)
                .WithCurrentTimestamp();

            if (tournaments == null || tournaments.Count == 0)
            {
                embed.WithDescription("_No tournaments found._");
                return embed.Build();
            }

            foreach (var tournament in tournaments.OrderByDescending(t => t.CreatedOn))
            {
                string status = tournament.IsRunning ? "🟢 Running" : "🔴 Not Running";
                string teamsLockedStatus = (tournament as ITeamLocking).IsTeamsLocked ? "🔒 Locked" : "🔓 Unlocked";
                string roundInfo = (tournament as IRoundBased).TotalRounds.HasValue
                    ? $"{(tournament as IRoundBased).CurrentRound}/{(tournament as IRoundBased).TotalRounds.Value}"
                    : $"{(tournament as IRoundBased).CurrentRound}/N/A";
                string description = string.Join("\n", new[]
                {
                    $"**Type:** {tournament.Type}",
                    $"**Status:** {status}",
                    $"**Teams:** {tournament.Teams.Count} ({teamsLockedStatus})",
                    $"**Round:** {roundInfo}",
                    $"**Created On:** {tournament.CreatedOn:yyyy-MM-dd}",
                    tournament.Description != null ? $"**Description:** {tournament.Description}" : null
                }.Where(s => !string.IsNullOrWhiteSpace(s)));

                embed.AddField($"{tournament.Name} (ID#: {tournament.Id})", description, false);
            }

            return embed.Build();
        }
        #endregion
    }
}
