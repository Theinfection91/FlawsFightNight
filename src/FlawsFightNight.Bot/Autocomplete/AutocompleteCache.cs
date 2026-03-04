using Discord;
using Discord.WebSocket;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.IO.Models;
using FlawsFightNight.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Autocomplete
{
    public class AutocompleteCache
    {
        private readonly AdminConfigurationService _adminConfigService;
        private readonly MatchService _matchService;
        private readonly TeamService _teamService;
        private readonly TournamentService _tournamentService;
        private readonly MemberService _memberService;

        // Autocomplete Data
        private List<Match> _allMatches = new();
        private List<PostMatch> _allPostMatches = new();
        private List<PostMatch> _roundRobinPostMatches = new();
        private List<Tournament> _allTournaments = new();
        private List<Tournament> _ladderTournaments = new();
        private List<Tournament> _roundRobinTournaments = new();
        private List<Tournament> _eliminationTournaments = new();
        private List<Team> _ladderTeams = new();
        private List<Team> _roundBasedTeams = new();
        private List<Team> _roundRobinTeams = new();
        private List<Team> _allTeams = new();
        private List<FTPCredential> _ftpCredentials = new();
        private List<MemberProfile> _memberProfiles = new();


        public AutocompleteCache(AdminConfigurationService adminConfigService, MatchService matchService, TeamService teamService, TournamentService tournamentService, MemberService memberService)
        {
            _adminConfigService = adminConfigService;
            _matchService = matchService;
            _teamService = teamService;
            _tournamentService = tournamentService;
            _memberService = memberService;

            // Initialize Autocomplete Data
            Update();
        }

        public void Update()
        {
            // Refresh autocomplete data from services
            _allMatches = _matchService.GetAllActiveMatches();
            _allPostMatches = _matchService.GetAllPostMatches();
            _roundRobinPostMatches = _matchService.GetAllRoundRobinPostMatches();
            _allTournaments = _tournamentService.GetAllTournaments();
            _ladderTournaments = _tournamentService.GetAllLadderTournaments();
            _roundRobinTournaments = _tournamentService.GetAllRoundRobinTournaments();
            //_eliminationTournaments = _tournamentService.GetAllEliminationTournaments();
            _ladderTeams = _teamService.GetAllLadderTeams();
            _roundRobinTeams = _teamService.GetAllRoundRobinTeams();
            _roundBasedTeams = _teamService.GetAllRoundBasedTeams();
            _allTeams = _teamService.GetAllTeams();
            _ftpCredentials = _adminConfigService.GetFTPCredentials()!;
            _memberProfiles = _memberService.GetAllMemberProfiles();
        }

        public List<AutocompleteResult> GetMatchIdsMatchingInput(string input)
        {
            // If the input is empty or only whitespace, return all matches sorted by tournament name and then match ID
            if (string.IsNullOrWhiteSpace(input))
            {
                return _allMatches
                    .OrderBy(match => _allTournaments.FirstOrDefault(t => t.MatchLog.GetAllActiveMatches().Any(m => m.Id == match.Id))?.Name)
                    .ThenBy(match => match.Id)
                    .Select(match =>
                    {
                        var tournament = _allTournaments.FirstOrDefault(t => t.MatchLog.GetAllActiveMatches().Any(m => m.Id == match.Id));
                        string tournamentName = tournament != null ? tournament.Name : "Unknown Tournament";
                        return new AutocompleteResult($"#{match.Id} | {match.TeamA} vs {match.TeamB} - {tournamentName} ({tournament.TeamSizeFormat} {tournament.GetFormattedType()})", match.Id);
                    })
                    .ToList();
            }

            // Filter matches based on the input (case-insensitive)
            var matchingMatches = _allMatches
                .Where(match =>
                {
                    var tournament = _allTournaments.FirstOrDefault(t => t.MatchLog.GetAllActiveMatches().Any(m => m.Id == match.Id));
                    string tournamentName = tournament != null ? tournament.Name : "Unknown Tournament";
                    return match.Id.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                           match.TeamA.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                           match.TeamB.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                           tournamentName.Contains(input, StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(match => _allTournaments.FirstOrDefault(t => t.MatchLog.GetAllActiveMatches().Any(m => m.Id == match.Id))?.Name)
                .ThenBy(match => match.Id)
                .Select(match =>
                {
                    var tournament = _allTournaments.FirstOrDefault(t => t.MatchLog.GetAllActiveMatches().Any(m => m.Id == match.Id));
                    string tournamentName = tournament != null ? tournament.Name : "Unknown Tournament";
                    return new AutocompleteResult($"#{match.Id} | {match.TeamA} vs {match.TeamB} - {tournamentName} ({tournament.TeamSizeFormat} {tournament.GetFormattedType()})", match.Id);
                })
                .ToList();

            return matchingMatches;
        }

        public List<AutocompleteResult> GetPostMatchIdsMatchingInput(string input)
        {
            // If the input is empty or only whitespace, return all post-matches sorted by tournament name and then match ID
            if (string.IsNullOrWhiteSpace(input))
            {
                return _roundRobinPostMatches
                    .OrderBy(postMatch => _roundRobinTournaments.FirstOrDefault(t => t.MatchLog.GetAllPostMatches().Any(m => m.Id == postMatch.Id))?.Name)
                    .ThenBy(postMatch => postMatch.Id)
                    .Select(postMatch =>
                    {
                        var tournament = null as Tournament;
                        foreach (var t in _roundRobinTournaments)
                        {
                            var editablePostMatches = t.MatchLog.GetAllPostMatches();
                            foreach (var pm in editablePostMatches)
                            {
                                if (pm.Id == postMatch.Id)
                                {
                                    tournament = t;
                                    break;
                                }
                            }
                            if (tournament != null)
                                break;
                        }
                        //string tournamentName = tournament != null ? tournament.Name : "Unknown Tournament";
                        return new AutocompleteResult($"#{postMatch.Id} | {postMatch.Winner} vs {postMatch.Loser} - {tournament?.Name} ({tournament?.TeamSizeFormat} {tournament?.GetFormattedType()})", postMatch.Id);
                    })
                    .ToList();
            }
            // Filter post-matches based on the input (case-insensitive)
            var matchingPostMatches = _roundRobinPostMatches
                .Where(postMatch =>
                {
                    var tournament = _roundRobinTournaments.FirstOrDefault(t => t.MatchLog.GetAllPostMatches().Any(m => m.Id == postMatch.Id));
                    string tournamentName = tournament != null ? tournament.Name : "Unknown Tournament";
                    return postMatch.Id.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                           postMatch.Winner.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                           postMatch.Loser.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                           tournamentName.Contains(input, StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(postMatch => _roundRobinTournaments.FirstOrDefault(t => t.MatchLog.GetAllPostMatches().Any(m => m.Id == postMatch.Id))?.Name)
                .ThenBy(postMatch => postMatch.Id)
                .Select(postMatch =>
                {
                    var tournament = _roundRobinTournaments.FirstOrDefault(t => t.MatchLog.GetAllPostMatches().Any(m => m.Id == postMatch.Id));
                    string tournamentName = tournament != null ? tournament.Name : "Unknown Tournament";
                    return new AutocompleteResult($"#{postMatch.Id} | {postMatch.Winner} vs {postMatch.Loser} - {tournamentName} ({tournament?.TeamSizeFormat} {tournament?.GetFormattedType()})", postMatch.Id);
                })
                .ToList();
            return matchingPostMatches;
        }

        public List<AutocompleteResult> GetTeamsFromMatchId(string matchId)
        {
            var match = _allMatches.FirstOrDefault(m => m.Id == matchId);
            if (match == null)
            {
                return new List<AutocompleteResult>();
            }
            // Grab teams - concat all team sources
            var teamA = _ladderTeams.Concat(_roundBasedTeams).Concat(_roundRobinTeams).FirstOrDefault(t => t.Name == match.TeamA);
            var teamB = _ladderTeams.Concat(_roundBasedTeams).Concat(_roundRobinTeams).FirstOrDefault(t => t.Name == match.TeamB);

            // Grab tournament
            var tournament = _allTournaments.FirstOrDefault(t => t.MatchLog.GetAllActiveMatches().Any(m => m.Id == match.Id));

            var results = new List<AutocompleteResult>();
            if (teamA != null)
            {
                results.Add(new AutocompleteResult($"{teamA.Name} - ({tournament.Name} {tournament.TeamSizeFormat} {tournament.GetFormattedType()})", teamA.Name));
            }
            if (teamB != null)
            {
                results.Add(new AutocompleteResult($"{teamB.Name} - ({tournament.Name} {tournament.TeamSizeFormat} {tournament.GetFormattedType()})", teamB.Name));
            }
            return results;
        }

        public List<AutocompleteResult> GetTeamsFromPostMatchId(string postMatchId)
        {
            var postMatch = _allPostMatches.FirstOrDefault(pm => pm.Id == postMatchId);
            if (postMatch == null)
            {
                return new List<AutocompleteResult>();
            }
            // Grab teams - concat all team sources
            var originalWinner = _ladderTeams.Concat(_roundBasedTeams).Concat(_roundRobinTeams).FirstOrDefault(t => t.Name == postMatch.Winner);
            var originalLoser = _ladderTeams.Concat(_roundBasedTeams).Concat(_roundRobinTeams).FirstOrDefault(t => t.Name == postMatch.Loser);

            // Grab tournament
            var tournament = _allTournaments.FirstOrDefault(t => t.MatchLog.GetAllPostMatches().Any(pm => pm.Id == postMatch.Id));

            var results = new List<AutocompleteResult>();
            if (originalWinner != null)
            {
                results.Add(new AutocompleteResult($"{originalWinner.Name} - ({tournament.Name} {tournament.TeamSizeFormat} {tournament.GetFormattedType()})", originalWinner.Name));
            }
            if (originalLoser != null)
            {
                results.Add(new AutocompleteResult($"{originalLoser.Name} - ({tournament.Name} {tournament.TeamSizeFormat} {tournament.GetFormattedType()})", originalLoser.Name));
            }
            return results;
        }

        public List<AutocompleteResult> GetLadderTeamNames(string input)
        {
            // Get all teams from all tournaments
            // If the input is empty or only whitespace, return all teams sorted alphabetically
            // Must use the cache data and not call the services directly
            if (string.IsNullOrWhiteSpace(input))
            {
                return _ladderTeams
                    //.OrderBy(team => _tournamentService.GetTournamentFromTeamName(team.Name).Name)
                    .OrderBy(team => _ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.Name)
                    .ThenBy(team => team.Rank)
                    .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.Name} ({_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.TeamSizeFormat} {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.GetFormattedType()})", team.Name))
                    .ToList();
            }
            // Filter teams based on the input (case-insensitive)
            var matchingTeams = _ladderTeams
                .Where(team => team.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(team => _ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.Name)
                .OrderBy(team => team.Rank)
                .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.Name} ({_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.TeamSizeFormat} {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.GetFormattedType()})", team.Name))
                .ToList();
            return matchingTeams;
        }

        public List<AutocompleteResult> GetTeamsForSendChallenge(string input)
        {
            // If the input is empty or only whitespace, return all teams sorted alphabetically
            if (string.IsNullOrWhiteSpace(input))
            {
                return _ladderTeams
                    .Where(team => team.IsChallengeable)
                    .OrderBy(team => team.Rank)
                    .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.Name} ({_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.TeamSizeFormat} {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.GetFormattedType()})", team.Name))
                    .ToList();
            }
            // Filter teams based on the input (case-insensitive)
            var matchingTeams = _ladderTeams
                .Where(team => team.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .Where(team => team.IsChallengeable)
                .OrderBy(team => team.Rank)
                .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.Name} ({_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.TeamSizeFormat} {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.GetFormattedType()})", team.Name))
                .ToList();
            return matchingTeams;
        }

        public List<AutocompleteResult> GetTeamsForCancelChallenge(string input)
        {
            // Filter to only teams that are a Challenger in a challenge
            var challengerTeams = _ladderTournaments.Where(t => t.MatchLog.GetAllActiveMatches().Any(m => m.Challenge != null))
                .SelectMany(t => t.Teams.Where(team => t.MatchLog.GetAllActiveMatches().Any(m => m.Challenge != null && m.Challenge.Challenger == team.Name)))
                .Distinct()
                .ToList();

            // If the input is empty or only whitespace, return all challenger teams sorted alphabetically
            if (string.IsNullOrWhiteSpace(input))
            {
                return challengerTeams
                    .OrderBy(team => team.Rank)
                    .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.Name} ({_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.TeamSizeFormat} {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.GetFormattedType()})", team.Name))
                    .ToList();
            }
            // Filter challenger teams based on the input (case-insensitive)
            var matchingTeams = challengerTeams
                .Where(team => team.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(team => team.Rank)
                .Select(team => new AutocompleteResult($"#{team.Rank} | {team.Name} - {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.Name} ({_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.TeamSizeFormat} {_ladderTournaments.Where(t => t.Teams.Contains(team)).FirstOrDefault()?.GetFormattedType()})", team.Name))
                .ToList();
            return matchingTeams;
        }

        public List<AutocompleteResult> GetTournamentIdsMatchingInput(string input)
        {
            // If the input is empty or only whitespace, return all tournaments sorted alphabetically
            if (string.IsNullOrWhiteSpace(input))
            {
                // Return all tournaments, sorted alphabetically by name
                return _allTournaments
                    .OrderBy(tournament => tournament.Name)
                    .Take(25)
                    .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedType()})", tournament.Id))
                    .ToList();
            }

            // Filter tournaments based on the input (case-insensitive)
            var matchingTournaments = _allTournaments
                .Where(tournament => tournament.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(tournament => tournament.Name)
                .Take(25)
                .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedType()})", tournament.Id))
                .ToList();

            return matchingTournaments;
        }

        public List<AutocompleteResult> GetRoundRobinTournamentIds(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                // Return all RR tournaments, sorted alphabetically by name
                return _roundRobinTournaments
                    .OrderBy(tournament => tournament.Name)
                    .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedType()})", tournament.Id))
                    .ToList();
            }
            // Filter RR tournaments based on the input (case-insensitive)
            var matchingTournaments = _roundRobinTournaments
                .Where(tournament => tournament.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(tournament => tournament.Name)
                .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedType()})", tournament.Id))
                .ToList();
            return matchingTournaments;
        }

        public List<AutocompleteResult> GetRoundBasedTournamentIds(string input)
        {
            // Get all normal RR tournaments
            // TODO: Extend this to include elimination tournaments when needed
            var tournaments = _allTournaments.Where(t => t.Type.Equals(TournamentType.NormalRoundRobin));

            // If the input is empty or only whitespace, return all round-based tournaments sorted alphabetically
            if (string.IsNullOrWhiteSpace(input))
            {
                // Return all round-based tournaments, sorted alphabetically by name
                return tournaments
                    .OrderBy(tournament => tournament.Name)
                    .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedType()})", tournament.Id))
                    .ToList();
            }
            // Filter round-based tournaments based on the input (case-insensitive)
            var matchingTournaments = tournaments
                .Where(tournament => tournament.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                .OrderBy(tournament => tournament.Name)
                .Select(tournament => new AutocompleteResult($"{tournament.Name} - ({tournament.TeamSizeFormat} {tournament.GetFormattedType()})", tournament.Id))
                .ToList();
            return matchingTournaments;
        }

        public List<AutocompleteResult> GetFTPCredentialsMatchingInput(string input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    return _ftpCredentials
                        .OrderBy(cred => cred.ServerName)
                        .Select(cred => new AutocompleteResult($"{cred.ServerName} ({cred.IPAddress}:{cred.Port})", cred.ServerName))
                        .ToList();
                }
                // Filter FTP credentials based on the input (case-insensitive)
                var matchingCredentials = _ftpCredentials
                    .Where(cred => cred.ServerName.Contains(input, StringComparison.OrdinalIgnoreCase) || cred.Username.Contains(input, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(cred => cred.ServerName)
                    .Select(cred => new AutocompleteResult($"{cred.ServerName} ({cred.IPAddress}:{cred.Port})", cred.ServerName))
                    .ToList();
                return matchingCredentials;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating FTP credential suggestions: {ex.Message}");
                return new List<AutocompleteResult>();
            }
        }

        public List<AutocompleteResult> GetAllTeams(string input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    return _allTeams
                        .OrderBy(team => team.Name)
                        .Select(team => new AutocompleteResult(team.Name, team.Name))
                        .ToList();
                }
                // Filter all teams based on the input (case-insensitive)
                var matchingTeams = _allTeams
                    .Where(team => team.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(team => team.Name)
                    .Select(team => new AutocompleteResult(team.Name, team.Name))
                    .ToList();
                return matchingTeams;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating teams for add/remove member suggestions: {ex.Message}");
                return new List<AutocompleteResult>();
            }
        }

        public List<AutocompleteResult> GetMemberUT2004Guids(ulong discordId)
        {
            try
            {
                if (discordId == 0)
                {
                    return new List<AutocompleteResult>();
                }

                // Prefer the cached member profiles; fall back to service lookup if missing
                var profile = _memberProfiles.FirstOrDefault(p => p.DiscordId == discordId)
                              ?? _memberService.GetMemberProfile(discordId);

                if (profile == null || profile.RegisteredUT2004GUIDs == null || profile.RegisteredUT2004GUIDs.Count == 0)
                    return new List<AutocompleteResult>();

                // Show GUIDs with a small hint for the primary (index 0 = oldest/primary)
                var results = profile.RegisteredUT2004GUIDs
                    .Select((g, idx) =>
                    {
                        var label = idx == 0 ? $"{g} (primary)" : g;
                        return new AutocompleteResult(label, g);
                    })
                    .ToList();

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating UT2004 GUID suggestions: {ex.Message}");
                return new List<AutocompleteResult>();
            }
        }
    }
}