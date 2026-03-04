using Discord;
using Discord.Interactions;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.CommandsLogic.TeamCommands
{
    public class RegisterTeamLogic : Logic
    {
        private readonly AdminConfigurationService _configManager;
        private readonly EmbedFactory _embedManager;
        private readonly GitBackupService _gitBackupManager;
        private readonly MemberService _memberManager;
        private readonly TournamentService _tournamentManager;
        private readonly TeamService _teamManager;

        public RegisterTeamLogic(AdminConfigurationService configManager,
                                 EmbedFactory embedManager,
                                 GitBackupService gitBackupManager,
                                 MemberService memberManager,
                                 TournamentService tournamentManager,
                                 TeamService teamManager) : base("Register Team")
        {
            _configManager = configManager;
            _embedManager = embedManager;
            _gitBackupManager = gitBackupManager;
            _memberManager = memberManager;
            _tournamentManager = tournamentManager;
            _teamManager = teamManager;
        }

        public async Task<Embed> RegisterTeamProcess(string teamName, string tournamentId, List<IUser> members)
        {
            // TODO Cannot register a team name with anything that could be a tournament ID#, or Match ID# (TXXX or MXXX)

            // Cannot try to report Bye as a team
            if (teamName.Equals("Bye", StringComparison.OrdinalIgnoreCase))
            {
                return _embedManager.ErrorEmbed(Name, $"Teams are not allowed to have any variation of the singular name 'Bye' for data purposes. \n\nUser input: {teamName}");
            }

            // Check if the tournament exists, grab it if so
            if (!_tournamentManager.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedManager.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentManager.GetTournamentById(tournamentId);

            // Can register new teams if Ladder Tournament is running, but cannot register them to Round Robin Tournament or SE/DE Bracket once they have started
            if (!tournament.CanAcceptNewTeams())
            {
                return _embedManager.ErrorEmbed(Name, $"The tournament '{tournament.Name}' can not accept new teams at this time. Check if teams are locked if applicable.");
            }

            // Check if the team name is unique within the tournament
            if (!_teamManager.IsTeamNameUnique(teamName))
            {
                return _embedManager.ErrorEmbed(Name, $"The team name '{teamName}' is already taken in this tournament or another. Team names must be unique across the entire bot. Please choose a different name.");
            }

            // Check if member count is valid based on the tournament's team size
            if (!_memberManager.IsMemberCountCorrect(members.Count, tournament.TeamSize))
            {
                return _embedManager.ErrorEmbed(Name, $"The number of members ({members.Count}) does not match the required team size ({tournament.TeamSize}) for the tournament '{tournament.Name}'.");
            }

            // Convert IUser list to Member objects list
            List<Member> convertedMembersList = _memberManager.ConvertIUsersToMembers(members);


            // Check if all members are valid and not already registered in the tournament (If they are, check if they are in debug admin list)
            foreach (Member member in convertedMembersList)
            {
                if (_memberManager.IsMemberRegisteredInTournament(member.DiscordId, tournament) && !_configManager.IsDiscordIdInDebugAdminList(member.DiscordId))
                {
                    return _embedManager.ErrorEmbed(Name, $"Member '{member.DisplayName}' (ID: {member.DiscordId}) is already registered in the tournament '{tournament.Name}'. Each member can only be part of one team per tournament.");
                }
            }

            // Create Team object
            Team newTeam = _teamManager.CreateTeam(teamName, convertedMembersList, tournament.Teams.Count + 1);

            // Add new team to the tournament
            tournament.AddTeam(newTeam);

            // Check if all members have a profile made, if not create one for them
            foreach (var member in convertedMembersList)
            {
                if (!_memberManager.DoesMemberProfileExist(member.DiscordId))
                {
                    var memberProfile = _memberManager.CreateMemberProfile(member.DiscordId, member.DisplayName);
                    _memberManager.AddProfileToDatabase(memberProfile);
                }
            }

            // Check if tournament is of type ITeamLocking to handle locking logic
            if (tournament is ITeamLocking lockableTournament)
            {
                // Check if tournament can be locked after adding the team
                // For now, only enable locking if tournament is running and has at least 3 teams
                if (!tournament.IsRunning && tournament.Teams.Count >= 3)
                {
                    _tournamentManager.SetCanTeamsBeLocked(lockableTournament, true);
                }
                else
                {
                    _tournamentManager.SetCanTeamsBeLocked(lockableTournament, false);
                }

                // Check if tournament is tie breaker rank system to adjust ranks
                if (tournament is ITieBreakerRankSystem tiebreakerTournament)
                {
                    // Adjust ranks of remaining teams
                    tiebreakerTournament.SetRanksByTieBreakerLogic();
                }
            }

            // If DSR tournament, apply default ratings and adjust ranks
            if (tournament is DSRLadderTournament)
            {
                newTeam.Rating = 1750;
                tournament.AdjustRanks();
            }

            // Give members credit for tournament participation in LIVE DSR and Ladder tournaments since they can join mid tournament
            if (tournament is DSRLadderTournament && tournament.CanEnd(out _) || tournament is NormalLadderTournament && tournament.CanEnd(out _))
            {
                _memberManager.IncrementMembersTournamentsPlayed(newTeam.Members);
            }

            // Save and reload the databases
            await _tournamentManager.SaveAndReloadTournamentDataFiles(tournament);
            await _memberManager.SaveAndReloadMemberProfiles();

            // Backup to git repo
            _gitBackupManager.EnqueueBackup();

            return _embedManager.TeamRegistrationSuccess(newTeam, tournament);
        }
    }
}
