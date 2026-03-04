using Discord;
using Discord.Interactions;
using FlawsFightNight.Commands;
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

namespace FlawsFightNight.Commands.TeamCommands
{
    public class RegisterTeamHandler : CommandHandler
    {
        private readonly AdminConfigurationService _adminConfigService;
        private readonly EmbedFactory _embedFactory;
        private readonly GitBackupService _gitBackupService;
        private readonly MemberService _memberService;
        private readonly TournamentService _tournamentService;
        private readonly TeamService _teamService;

        public RegisterTeamHandler(AdminConfigurationService adminConfigService,
                                 EmbedFactory embedFactory,
                                 GitBackupService gitBackupService,
                                 MemberService memberService,
                                 TournamentService tournamentService,
                                 TeamService teamService) : base("Register Team")
        {
            _adminConfigService = adminConfigService;
            _embedFactory = embedFactory;
            _gitBackupService = gitBackupService;
            _memberService = memberService;
            _tournamentService = tournamentService;
            _teamService = teamService;
        }

        public async Task<Embed> RegisterTeamProcess(string teamName, string tournamentId, List<IUser> members)
        {
            // TODO Cannot register a team name with anything that could be a tournament ID#, or Match ID# (TXXX or MXXX)

            // Cannot try to report Bye as a team
            if (teamName.Equals("Bye", StringComparison.OrdinalIgnoreCase))
            {
                return _embedFactory.ErrorEmbed(Name, $"Teams are not allowed to have any variation of the singular name 'Bye' for data purposes. \n\nUser input: {teamName}");
            }

            // Check if the tournament exists, grab it if so
            if (!_tournamentService.IsTournamentIdInDatabase(tournamentId))
            {
                return _embedFactory.ErrorEmbed(Name, $"No tournament found with ID: {tournamentId}. Please check the ID and try again.");
            }
            var tournament = _tournamentService.GetTournamentById(tournamentId);

            // Can register new teams if Ladder Tournament is running, but cannot register them to Round Robin Tournament or SE/DE Bracket once they have started
            if (!tournament.CanAcceptNewTeams())
            {
                return _embedFactory.ErrorEmbed(Name, $"The tournament '{tournament.Name}' can not accept new teams at this time. Check if teams are locked if applicable.");
            }

            // Check if the team name is unique within the tournament
            if (!_teamService.IsTeamNameUnique(teamName))
            {
                return _embedFactory.ErrorEmbed(Name, $"The team name '{teamName}' is already taken in this tournament or another. Team names must be unique across the entire bot. Please choose a different name.");
            }

            // Check if member count is valid based on the tournament's team size
            if (!_memberService.IsMemberCountCorrect(members.Count, tournament.TeamSize))
            {
                return _embedFactory.ErrorEmbed(Name, $"The number of members ({members.Count}) does not match the required team size ({tournament.TeamSize}) for the tournament '{tournament.Name}'.");
            }

            // Convert IUser list to Member objects list
            List<Member> convertedMembersList = _memberService.ConvertIUsersToMembers(members);


            // Check if all members are valid and not already registered in the tournament (If they are, check if they are in debug admin list)
            foreach (Member member in convertedMembersList)
            {
                if (_memberService.IsMemberRegisteredInTournament(member.DiscordId, tournament) && !_adminConfigService.IsDiscordIdInDebugAdminList(member.DiscordId))
                {
                    return _embedFactory.ErrorEmbed(Name, $"Member '{member.DisplayName}' (ID: {member.DiscordId}) is already registered in the tournament '{tournament.Name}'. Each member can only be part of one team per tournament.");
                }
            }

            // Create Team object
            Team newTeam = _teamService.CreateTeam(teamName, convertedMembersList, tournament.Teams.Count + 1);

            // Add new team to the tournament
            tournament.AddTeam(newTeam);

            // Check if all members have a profile made, if not create one for them
            foreach (var member in convertedMembersList)
            {
                if (!_memberService.DoesMemberProfileExist(member.DiscordId))
                {
                    var memberProfile = _memberService.CreateMemberProfile(member.DiscordId, member.DisplayName);
                    _memberService.AddProfileToDatabase(memberProfile);
                }
            }

            // Check if tournament is of type ITeamLocking to handle locking logic
            if (tournament is ITeamLocking lockableTournament)
            {
                // Check if tournament can be locked after adding the team
                // For now, only enable locking if tournament is running and has at least 3 teams
                if (!tournament.IsRunning && tournament.Teams.Count >= 3)
                {
                    _tournamentService.SetCanTeamsBeLocked(lockableTournament, true);
                }
                else
                {
                    _tournamentService.SetCanTeamsBeLocked(lockableTournament, false);
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
                _memberService.IncrementMembersTournamentsPlayed(newTeam.Members);
            }

            // Save and reload the databases
            await _tournamentService.SaveAndReloadTournamentDataFiles(tournament);
            await _memberService.SaveAndReloadMemberProfiles();

            // Backup to git repo
            _gitBackupService.EnqueueBackup();

            return _embedFactory.TeamRegistrationSuccess(newTeam, tournament);
        }
    }
}
