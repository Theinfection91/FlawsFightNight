using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public Embed MatchesLiveView(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"⚔️ {tournament.Name} - {tournament.TeamSizeFormat} Round Robin Tournament Matches")
                .WithDescription($"*ID#: {tournament.Id}*\n**Round {tournament.CurrentRound}/{tournament.TotalRounds ?? 0}**\n")
                .WithColor(Color.Orange)
                .WithCurrentTimestamp();

            if (tournament.IsRoundComplete && tournament.IsRoundLockedIn && !tournament.CanEndTournament)
            {
                embed.AddField("🔒 Locked", "Round is locked and ready to advance.", true);
            }
            if (tournament.IsRoundComplete && tournament.IsRoundLockedIn && tournament.CanEndTournament)
            {
                embed.AddField("🔒 Locked - Ready to end tournament 🏅", "Round is locked and the tournament is ready to end have the results locked in.", true);
            }
            if (tournament.IsRoundComplete && !tournament.IsRoundLockedIn)
            {
                embed.AddField("🔓 Unlocked", "Round is finished but unlocked. Lock to finalize results then advance.", true);
            }

            // --- Matches To Play ---
            if (tournament.MatchLog.MatchesToPlayByRound.TryGetValue(tournament.CurrentRound, out var matchesToPlay)
                && matchesToPlay.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var match in matchesToPlay)
                {
                    if (match.IsByeMatch)
                        continue;
                    else
                        sb.AppendLine($"🔹 **{match.TeamA}** vs **{match.TeamB}**");
                }
                foreach (var match in matchesToPlay)
                {
                    if (match.IsByeMatch)
                        sb.AppendLine($"💤 *{match.GetCorrectNameForByeMatch()} Bye Week*");
                    else
                        continue;
                }
                embed.AddField($"⚔️ Matches To Play (Round {tournament.CurrentRound})", sb.ToString(), false);
            }
            else
            {
                embed.AddField("⚔️ Matches To Play", "No matches left to play this round ✅", false);
            }

            // --- Past Matches (grouped by round) ---
            if (tournament.MatchLog.PostMatchesByRound.Count > 0)
            {
                foreach (var round in tournament.MatchLog.PostMatchesByRound.OrderBy(kvp => kvp.Key))
                {
                    var sb = new StringBuilder();
                    foreach (var postMatch in round.Value.OrderBy(pm => pm.CompletedOn))
                    {
                        if (postMatch.WasByeMatch)
                            sb.AppendLine($"💤 *{postMatch.Winner} Bye Week*");
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

        public Embed RoundRobinStandingsLiveView(Tournament tournament, RoundRobinStandings roundRobinStandings)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"📊 {tournament.Name} - {tournament.TeamSizeFormat} Round Robin Tournament Standings")
                .WithDescription($"*ID#: {tournament.Id}*\n**Round {tournament.CurrentRound}/{tournament.TotalRounds ?? 0}**\n")
                .WithColor(Color.Gold)
                .WithCurrentTimestamp();

            if (roundRobinStandings.Entries.Count == 0)
            {
                embed.Description += "\n_No teams registered._";
                return embed.Build();
            }

            foreach (var teamStanding in roundRobinStandings.Entries.OrderBy(e => e.Rank))
            {
                var (pointsFor, pointsAgainst) = tournament.MatchLog.GetPointsForAndPointsAgainstForTeam(teamStanding.TeamName);

                embed.Description +=
                    $"\n#{teamStanding.Rank} **{teamStanding.TeamName}**\n" +
                    $"✅ Wins: {teamStanding.Wins} | " +
                    $"❌ Losses: {teamStanding.Losses} | " +
                    $"{teamStanding.GetCorrectStreakEmoji()} W/L Streak: {teamStanding.GetFormattedStreakString()}\n" +
                    $"⭐ Points For: {pointsFor} | " +
                    $"🛡️ Points Against: {pointsAgainst}\n";
            }

            return embed.Build();
        }

        public Embed TeamsLiveView(Tournament tournament, RoundRobinStandings standings)
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

            foreach (var teamStanding in standings.Entries.OrderBy(e => e.Rank))
            {
                var team = tournament.Teams.FirstOrDefault(t => t.Name == teamStanding.TeamName);
                if (team != null)
                {
                    embed.Description +=
                        $"\n**{team.Name}** (#{teamStanding.Rank})\n" +
                        $"👤 Members: {string.Join(", ", team.Members.Select(m => m.DisplayName))}\n";
                    // If Ladder Tournament, display challenge status
                    if (tournament.Type == TournamentType.Ladder)
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

        public Embed ReportByeMatch(Tournament tournament, Match match)
        {
            var embed = new EmbedBuilder()
                .WithTitle("☑️ Bye Match Completion Reported")
                .WithDescription($"The bye match for '**{match.GetCorrectNameForByeMatch()}'** has been recorded as complete in **{tournament.Name}**.")
                .AddField("Tournament ID", tournament.Id)
                .WithColor(Color.Green)
                .WithFooter("Bye match completion reported successfully.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed ReportWinSuccess(Tournament tournament, Match match, Team winningTeam, int winningTeamScore, Team losingTeam, int losingTeamScore)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🏆 Match Result Reported")
                .WithDescription($"The match '**{match.TeamA} vs {match.TeamB}**' has been successfully reported in **{tournament.Name}**.")
                .AddField("Winning Team (Score)", $"{winningTeam.Name} ({winningTeamScore})")
                .AddField("Losing Team (Score)", $"{losingTeam.Name} ({losingTeamScore})")
                .AddField("Tournament ID", tournament.Id)
                .WithColor(Color.Green)
                .WithFooter("Match result reported successfully.")
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

        #region Team Embeds
        public Embed TeamRegistrationSuccess(Team team, Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🎉 Team Registered Successfully")
                .WithDescription($"The team **{team.Name}** has been successfully registered in the tournament **{tournament.Name}**!")
                .AddField("Tournament ID", tournament.Id)
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
        #endregion

        #region Tournament Embeds
        public Embed CreateTournamentSuccessResolver(Tournament tournament)
        {
            switch (tournament.Type)
            {
                case TournamentType.RoundRobin:
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
                .WithColor(Color.Green)
                .WithFooter("The tournament has been deleted.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed RoundRobinCreateTournamentSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🎉 Round Robin Tournament Created")
                .WithDescription($"A Round Robin tournament named **{tournament.Name}** has been successfully created!\n\nRemember the following Tournament ID for future commands.\n\nDefault tie breaker rules are 'Traditional' and the default duration is set to 'Double Round Robin'. To change either of these settings, use /tournament setup anytime before starting the tournament.")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Match Format", tournament.TeamSizeFormat)
                .WithColor(Color.Green)
                .WithFooter("Let's get some teams registered to this tournament now.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed SetupTournamentResolver(Tournament tournament)
        {
            switch (tournament.Type)
            {
                case TournamentType.RoundRobin:
                    return RoundRobinSetupTournamentSuccess(tournament);
                default:
                    return ErrorEmbed("Unsupported tournament type.");
            }
        }

        private Embed RoundRobinSetupTournamentSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("⚙️ Tournament Setup Success")
                .WithDescription($"The Round Robin tournament **{tournament.Name}** has been successfully updated.")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Tie Breaker Rules", tournament.TieBreakerRule.Name)
                .AddField("Round Robin Type", tournament.IsDoubleRoundRobin ? RoundRobinType.Double : RoundRobinType.Single)
                .WithColor(Color.Green)
                .WithFooter("You can change the settings again anytime before starting.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed StartTournamentSuccessResolver(Tournament tournament)
        {
            switch (tournament.Type)
            {
                case TournamentType.RoundRobin:
                    return RoundRobinStartTournamentSuccess(tournament);
                default:
                    return ErrorEmbed("Unsupported tournament type.");
            }
        }

        private Embed RoundRobinStartTournamentSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🏆 Tournament Started")
                .WithDescription($"The Round Robin tournament **{tournament.Name}** has been successfully started!")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Tie Breaker Rules", tournament.TieBreakerRule.Name)
                .AddField("Match Format", tournament.TeamSizeFormat)
                .AddField("Total Teams", tournament.Teams.Count)
                .WithColor(Color.Green)
                .WithFooter("Good luck to all teams!")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed EndTournamentSuccessResolver(Tournament tournament, string winner)
        {
            switch (tournament.Type)
            {
                case TournamentType.RoundRobin:
                    return RoundRobinEndTournamentSuccess(tournament, winner);
                default:
                    return ErrorEmbed("Unsupported tournament type.");
            }
        }

        private Embed RoundRobinEndTournamentSuccess(Tournament tournament, string winner)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🏁 Tournament Ended")
                .WithDescription($"The Round Robin tournament **{tournament.Name}** has been successfully ended!")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Winner", $"{winner}")
                .AddField("Total Teams", tournament.Teams.Count)
                .WithColor(Color.Green)
                .WithFooter("Thank you for participating!")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed RoundRobinEndTournamentWithTieBreakerSuccess(Tournament tournament, (string, string) tieBreakerInfo)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🏁 Tournament Ended with Tiebreaker")
                .WithDescription($"The Round Robin tournament **{tournament.Name}** has been successfully ended!\n\nA tiebreaker was needed to determine the winner.")
                .AddField("Tournament ID", tournament.Id)
                .WithDescription(tieBreakerInfo.Item1)
                .AddField("Winner", $"{tieBreakerInfo.Item2}")
                .AddField("Total Teams", tournament.Teams.Count)
                .WithColor(Color.Green)
                .WithFooter("Thank you for participating!")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
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

        public Embed NextRoundSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("➡️ Next Round Started")
                .WithDescription($"The tournament **{tournament.Name}** has successfully advanced to the next round: **Round {tournament.CurrentRound}**.")
                .AddField("Tournament ID", tournament.Id)
                .WithColor(Color.Green)
                .WithFooter("Good luck to all teams in the next round!")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }
        #endregion
    }
}
