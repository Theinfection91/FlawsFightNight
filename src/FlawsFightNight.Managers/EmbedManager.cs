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

        #region Match Embeds
        public Embed ReportByeMatch(Tournament tournament, Match match)
        {
            var embed = new EmbedBuilder()
                .WithTitle("☑️ Bye Match Completion Reported")
                .WithDescription($"The bye match '**{match.TeamA} vs {match.TeamB}**' has been recorded as complete in **{tournament.Name}**.")
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

        public Embed RoundRobinCreateTournamentSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🎉 Round Robin Tournament Created")
                .WithDescription($"A Round Robin tournament named **{tournament.Name}** has been successfully created!\n\nRemember this Tournament ID for future commands. Make sure you have selected your preferred tie breaker rules before starting a round robin tournament. Default rules are 'Traditional'. Refer to documentation for more information.")
                .AddField("Tournament ID", tournament.Id)
                .AddField("Match Format", tournament.TeamSizeFormat)
                .WithColor(Color.Green)
                .WithFooter("Let's get some teams registered to this tournament now.")
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

        public Embed LockTeamsSuccess(Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🔒 Teams Locked")
                .WithDescription($"The teams in the {tournament.TeamSizeFormat} tournament **{tournament.Name}** have been successfully locked.")
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
                .WithDescription($"The teams in the {tournament.TeamSizeFormat} tournament **{tournament.Name}** have been successfully unlocked.")
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
