using Discord;
using FlawsFightNight.Core.Enums.UT2004;
using FlawsFightNight.Core.Models.UT2004;
using FlawsFightNight.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Commands.StatsCommands.UT2004StatsCommands
{
    public class GetWinProbabilityHandler : CommandHandler
    {
        private readonly EmbedFactory _embedFactory;
        private readonly MemberService _memberService;
        private readonly OpenSkillRatingService _openSkillService;
        private readonly TeamService _teamService;
        private readonly TournamentService _tournamentService;

        public GetWinProbabilityHandler(
            EmbedFactory embedFactory,
            MemberService memberService,
            OpenSkillRatingService openSkillService,
            TeamService teamService,
            TournamentService tournamentService) : base("Win Probability")
        {
            _embedFactory = embedFactory;
            _memberService = memberService;
            _openSkillService = openSkillService;
            _teamService = teamService;
            _tournamentService = tournamentService;
        }

        public async Task<Embed> Handle(string teamOneName, string teamTwoName, UT2004GameMode gameMode)
        {
            if (string.Equals(teamOneName, teamTwoName, System.StringComparison.OrdinalIgnoreCase))
                return _embedFactory.ErrorEmbed(Name, "You can't calculate win probability between a team and itself.");

            var teamOne = _teamService.GetTeamByName(teamOneName);
            var teamTwo = _teamService.GetTeamByName(teamTwoName);

            if (teamOne == null)
                return _embedFactory.ErrorEmbed(Name, $"**{teamOneName}** was not found in any tournament.");
            if (teamTwo == null)
                return _embedFactory.ErrorEmbed(Name, $"**{teamTwoName}** was not found in any tournament.");

            var allTournaments = _tournamentService.GetAllTournaments();
            var tournamentOne = allTournaments.FirstOrDefault(t =>
                t.Teams.Any(team => team.Name.Equals(teamOneName, System.StringComparison.OrdinalIgnoreCase)));
            var tournamentTwo = allTournaments.FirstOrDefault(t =>
                t.Teams.Any(team => team.Name.Equals(teamTwoName, System.StringComparison.OrdinalIgnoreCase)));

            bool sameTournament = tournamentOne?.Id == tournamentTwo?.Id;

            var (teamOneRatings, teamOneMissing) = ResolveTeamRatings(teamOne, gameMode);
            var (teamTwoRatings, teamTwoMissing) = ResolveTeamRatings(teamTwo, gameMode);

            var teamOneMusSigmas = teamOneRatings.Select(r => (r.Mu, r.Sigma)).ToList();
            var teamTwoMusSigmas = teamTwoRatings.Select(r => (r.Mu, r.Sigma)).ToList();

            double winProbOne = _openSkillService.GetTeamAWinProbability(teamOneMusSigmas, teamTwoMusSigmas);

            return BuildEmbed(
                teamOne.Name, teamTwo.Name,
                tournamentOne?.Name, tournamentTwo?.Name,
                teamOneRatings, teamTwoRatings,
                winProbOne, gameMode,
                sameTournament,
                teamOneMissing || teamTwoMissing);
        }

        private (List<PlayerRating> ratings, bool hasMissing) ResolveTeamRatings(
            FlawsFightNight.Core.Models.Team team, UT2004GameMode gameMode)
        {
            var ratings = new List<PlayerRating>();
            bool hasMissing = false;

            foreach (var member in team.Members)
            {
                var memberProfile = _memberService.GetMemberProfile(member.DiscordId);
                if (memberProfile == null || memberProfile.RegisteredUT2004GUIDs.Count == 0)
                {
                    ratings.Add(new PlayerRating(member.DisplayName, 25.0, 25.0 / 3.0, false));
                    hasMissing = true;
                    continue;
                }

                var utProfile = _memberService.GetUT2004PlayerProfile(memberProfile.RegisteredUT2004GUIDs.First());
                if (utProfile == null)
                {
                    ratings.Add(new PlayerRating(member.DisplayName, 25.0, 25.0 / 3.0, false));
                    hasMissing = true;
                    continue;
                }

                var (mu, sigma) = utProfile.GetMuSigmaComposite(gameMode);
                ratings.Add(new PlayerRating(utProfile.CurrentName, mu, sigma, true));
            }

            return (ratings, hasMissing);
        }

        private static Embed BuildEmbed(
            string teamOneName, string teamTwoName,
            string? tournamentOneName, string? tournamentTwoName,
            List<PlayerRating> teamOneRatings, List<PlayerRating> teamTwoRatings,
            double winProbOne, UT2004GameMode gameMode,
            bool sameTournament, bool hasMissingProfiles)
        {
            string modeDisplay = gameMode switch
            {
                UT2004GameMode.iCTF => "🚩 iCTF",
                UT2004GameMode.TAM  => "🎯 TAM",
                UT2004GameMode.iBR  => "💣 iBR",
                _                   => "🎮 General"
            };

            string tournamentLine = sameTournament
                ? $"*{tournamentOneName ?? "Unknown Tournament"}*"
                : $"⚠️ *{teamOneName}: {tournamentOneName ?? "Unknown"} · {teamTwoName}: {tournamentTwoName ?? "Unknown"}*";

            var embed = new EmbedBuilder()
                .WithTitle($"⚖️ {teamOneName} vs {teamTwoName} — {modeDisplay}")
                .WithDescription(
                    $"{tournamentLine}\n\n" +
                    $"🔵 **{teamOneName}:** {winProbOne:P1}  ·  🔴 **{teamTwoName}:** {1 - winProbOne:P1}\n" +
                    $"*Based on {modeDisplay} OpenSkill ratings (μ−3σ)*")
                .WithColor(new Color(0xFF6A00))
                .WithFooter("Flaws Fight Night — UT2004 Win Probability")
                .WithCurrentTimestamp();

            embed.AddField($"🔵 {teamOneName}", BuildTeamField(teamOneRatings), false);
            embed.AddField($"🔴 {teamTwoName}", BuildTeamField(teamTwoRatings), false);

            if (!sameTournament)
                embed.AddField("⚠️ Different Tournaments",
                    "These teams are not in the same tournament. The win probability is still calculated but may not be meaningful for a real match.",
                    false);

            if (hasMissingProfiles)
                embed.AddField("⚠️ Missing Profiles",
                    "One or more players have no registered UT2004 GUID or profile and were rated at default (μ=25, σ=8.33).",
                    false);

            return embed.Build();
        }

        private static string BuildTeamField(List<PlayerRating> players)
        {
            var sb = new StringBuilder();
            foreach (var p in players)
            {
                double displayRating = p.Mu - 3 * p.Sigma;
                string icon = p.HasProfile ? "•" : "⚠️";
                sb.AppendLine($"{icon} **{p.Name}** • {displayRating:F2}  *(μ={p.Mu:F2}, σ={p.Sigma:F2})*");
            }
            return sb.ToString().TrimEnd();
        }

        private record PlayerRating(string Name, double Mu, double Sigma, bool HasProfile);
    }
}
