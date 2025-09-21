using Discord;
using Discord.Interactions;
using FlawsFightNight.Bot.Modals;
using FlawsFightNight.Bot.PreconditionAttributes;
using FlawsFightNight.CommandsLogic.TeamCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.SlashCommands
{
    [Group("team", "Commands related to teams like creating, removal, etc.")]
    public class TeamCommands : InteractionModuleBase<SocketInteractionContext>
    {
        
        private RemoveLossLogic _removeLossLogic;

        private RemoveWinLogic _removeWinLogic;

        private RemoveMemberLogic _removeMemberLogic;
        private RegisterTeamLogic _registerTeamLogic;

        public TeamCommands(RegisterTeamLogic registerTeamLogic)
        {
            _registerTeamLogic = registerTeamLogic;
        }

        [SlashCommand("register", "Register a new team for a chosen Tournament")]
        [RequireGuildAdmin]
        public async Task RegisterTeamAsync(
            [Summary("name", "The name of the team")] string name,
            [Summary("tournament_id", "The ID of the tournament to register for")] string tournamentId,
            [Summary("member1", "A member to add to the team.")] IUser member1,
            [Summary("member2", "A member to add to the team.")] IUser? member2 = null,
            [Summary("member3", "A member to add to the team.")] IUser? member3 = null,
            [Summary("member4", "A member to add to the team.")] IUser? member4 = null,
            [Summary("member5", "A member to add to the team.")] IUser? member5 = null,
            [Summary("member6", "A member to add to the team.")] IUser? member6 = null,
            [Summary("member7", "A member to add to the team.")] IUser? member7 = null,
            [Summary("member8", "A member to add to the team.")] IUser? member8 = null,
            [Summary("member9", "A member to add to the team.")] IUser? member9 = null,
            [Summary("member10", "A member to add to the team.")] IUser? member10 = null,
            [Summary("member11", "A member to add to the team.")] IUser? member11 = null,
            [Summary("member12", "A member to add to the team.")] IUser? member12 = null,
            [Summary("member13", "A member to add to the team.")] IUser? member13 = null,
            [Summary("member14", "A member to add to the team.")] IUser? member14 = null,
            [Summary("member15", "A member to add to the team.")] IUser? member15 = null,
            [Summary("member16", "A member to add to the team.")] IUser? member16 = null,
            [Summary("member17", "A member to add to the team.")] IUser? member17 = null,
            [Summary("member18", "A member to add to the team.")] IUser? member18 = null,
            [Summary("member19", "A member to add to the team.")] IUser? member19 = null,
            [Summary("member20", "A member to add to the team.")] IUser? member20 = null)
        {
            try
            {
                // Initialize the list of members
                var members = new List<IUser>() { member1 };

                // Add members to the list if they are not null
                if (member2 != null) members.Add(member2);
                if (member3 != null) members.Add(member3);
                if (member4 != null) members.Add(member4);
                if (member5 != null) members.Add(member5);
                if (member6 != null) members.Add(member6);
                if (member7 != null) members.Add(member7);
                if (member8 != null) members.Add(member8);
                if (member9 != null) members.Add(member9);
                if (member10 != null) members.Add(member10);
                if (member11 != null) members.Add(member11);
                if (member12 != null) members.Add(member12);
                if (member13 != null) members.Add(member13);
                if (member14 != null) members.Add(member14);
                if (member15 != null) members.Add(member15);
                if (member16 != null) members.Add(member16);
                if (member17 != null) members.Add(member17);
                if (member18 != null) members.Add(member18);
                if (member19 != null) members.Add(member19);
                if (member20 != null) members.Add(member20);

                var result = _registerTeamLogic.RegisterTeamProcess(name, tournamentId, members);
                await RespondAsync(embed: result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [SlashCommand("delete", "Remove a team from the bot's database.")]
        [RequireGuildAdmin]
        public async Task DeleteTeamAsync()
        {
            try
            {
                await RespondWithModalAsync<DeleteTeamModal>("delete_team");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Command Error: {ex}");
                await RespondAsync("An error occurred while processing this command.", ephemeral: true);
            }
        }

        [Group("add", "Commands related to addings things to a team.")]
        public class TeamAddCommands : InteractionModuleBase<SocketInteractionContext>
        {
            private AddTeamLossLogic _addTeamLossLogic;
            private AddWinLogic _addTeamWinLogic;
            private AddMemberLogic _addTeamMemberLogic;
            public TeamAddCommands(AddTeamLossLogic addTeamLossLogic, AddWinLogic addTeamWinLogic, AddMemberLogic addTeamMemberLogic)
            {
                _addTeamLossLogic = addTeamLossLogic;
                _addTeamWinLogic = addTeamWinLogic;
                _addTeamMemberLogic = addTeamMemberLogic;
            }

            [SlashCommand("member", "Add a member to an existing team.")]
            [RequireGuildAdmin]
            public async Task AddMemberAsync(
                [Summary("team_name", "The name of the team to add a member to.")] string teamName,
                [Summary("member", "The member to add to the team.")] IUser member)
            {
                try
                {
                    //var result = ;
                    //await RespondAsync(embed: result);
                    await RespondAsync("Not yet implemented.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await RespondAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("win", "Admin command - Add number of wins to a team.")]
            [RequireGuildAdmin]
            public async Task AddWinAsync(
                [Summary("team_name", "The name of the team to add wins.")] string teamName,
                [Summary("number_of_wins", "The amount of wins to add.")] int number_of_wins)
            {
                try
                {
                    //var result = ;
                    //await RespondAsync(embed: result);
                    await RespondAsync("Not yet implemented.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await RespondAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("loss", "Admin command - Add number of losses to a team.")]
            [RequireGuildAdmin]
            public async Task AddLossAsync(
                [Summary("team_name", "The name of the team to add losses.")] string teamName,
                [Summary("number_of_losses", "The amount of losses to add.")] int number_of_losses)
            {
                try
                {
                    var result = _addTeamLossLogic.AddLossProcess(teamName, number_of_losses);
                    await RespondAsync(embed: result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await RespondAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }

        [Group("remove", "Commands related to removing things to a team.")]
        public class TeamRemoveCommands : InteractionModuleBase<SocketInteractionContext>
        {
            public TeamRemoveCommands()
            {

            }

            [SlashCommand("member", "Add a member to an existing team.")]
            [RequireGuildAdmin]
            public async Task AddMemberAsync(
                [Summary("team_name", "The name of the team to add a member to.")] string teamName,
                [Summary("member", "The member to add to the team.")] IUser member)
            {
                try
                {
                    //var result = ;
                    //await RespondAsync(embed: result);
                    await RespondAsync("Not yet implemented.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await RespondAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("win", "Admin command - Add number of wins to a team.")]
            [RequireGuildAdmin]
            public async Task AddWinAsync(
                [Summary("team_name", "The name of the team to add wins.")] string teamName,
                [Summary("number_of_wins", "The amount of wins to add.")] int number_of_wins)
            {
                try
                {
                    //var result = ;
                    //await RespondAsync(embed: result);
                    await RespondAsync("Not yet implemented.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await RespondAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }

            [SlashCommand("loss", "Admin command - Add number of losses to a team.")]
            [RequireGuildAdmin]
            public async Task AddLossAsync(
                [Summary("team_name", "The name of the team to add losses.")] string teamName,
                [Summary("number_of_losses", "The amount of losses to add.")] int number_of_losses)
            {
                try
                {
                    //var result = ;
                    //await RespondAsync(embed: result);
                    await RespondAsync("Not yet implemented.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command Error: {ex}");
                    await RespondAsync("An error occurred while processing this command.", ephemeral: true);
                }
            }
        }
    }
}
