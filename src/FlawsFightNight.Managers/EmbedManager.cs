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

        #region Team Embeds
        public Embed TeamNotFound(string teamName)
        {
            var embed = new EmbedBuilder()
                .WithTitle("⚠️ Team Not Found")
                .WithDescription($"No team found with the name: **{teamName}**")
                .WithColor(Color.Red)
                .WithFooter("Team name verification failed.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed TeamAlreadyExists(string teamName)
        {
            var embed = new EmbedBuilder()
                .WithTitle("⚠️ Team Already Exists")
                .WithDescription($"A team with the name **{teamName}** already exists in this tournament.")
                .WithColor(Color.Red)
                .WithFooter("Please choose a different team name.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        public Embed TeamRegistrationSuccess(Team team, Tournament tournament)
        {
            var embed = new EmbedBuilder()
                .WithTitle("🎉 Team Registered Successfully")
                .WithDescription($"The team **{team.Name}** has been successfully registered in the tournament **{tournament.Name}**!")
                .AddField("Members", string.Join(", ", team.Members.Select(m => m.DisplayName)))
                .WithColor(Color.Green)
                .WithFooter("Good luck to your team!")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

        #endregion

        #region Tournament Embeds
        public Embed TournamentNotFound(string tournamentId)
        {
            var embed = new EmbedBuilder()
                .WithTitle("⚠️ Tournament Not Found")
                .WithDescription($"No tournament found with ID: **{tournamentId}**")
                .WithColor(Color.Red)
                .WithFooter("Tournament name verification failed.")
                .WithTimestamp(DateTimeOffset.Now);
            return embed.Build();
        }

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

        public Embed StartTournamentSuccessResolver (Tournament tournament)
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
        #endregion
    }
}
