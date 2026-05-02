using Discord;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Interfaces;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Services
{
    public class TournamentService : BaseDataDriven
    {
        public TournamentService(DataContext dataContext) : base("TournamentService", dataContext)
        {   

        }

        public async Task SaveTournament(Tournament tournament)
        {
            foreach (var tournamentData in _dataContext.TournamentDataFiles)
            {
                if (tournamentData.Tournament.Id.Equals(tournament.Id, StringComparison.OrdinalIgnoreCase))
                {
                    await _dataContext.SaveTournamentDataFile(tournamentData);
                    return;
                }
            }
            // No existing tournament data file found, create a new one
            await _dataContext.AddNewTournament(tournament);
        }

        public async Task LoadTournamentDataFiles()
        {
            await _dataContext.LoadTournamentDataFiles();
        }

        public async Task SaveAndReloadTournamentDataFiles(Tournament tournament)
        {
            await SaveTournament(tournament);
            await LoadTournamentDataFiles();
        }

        public bool IsTournamentNameUnique(string tournamentName)
        {
            foreach (var dataFile in _dataContext.TournamentDataFiles)
            {
                if (dataFile.Tournament.Name.Equals(tournamentName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }

        public Tournament CreateNewTournament(string name, TournamentType tournamentType, int teamSize, string? description = null)
        {
            string? id = GenerateTournamentId();
            if (id == null)
            {
                return null;
            }

            switch (tournamentType)
            {
                case TournamentType.DSRLadder:
                    return new DSRLadderTournament(id, name, teamSize);

                case TournamentType.NormalLadder:
                    return new NormalLadderTournament(id, name, teamSize);

                case TournamentType.NormalRoundRobin:
                    return new NormalRoundRobinTournament(id, name, teamSize);

                case TournamentType.OpenRoundRobin:
                    return new OpenRoundRobinTournament(id, name, teamSize);

                default:
                    return null;
            }
        }

        public async Task DeleteTournament(string tournamentId)
        {
            await _dataContext.RemoveTournament(tournamentId);
        }

        public void SetCanTeamsBeLocked(ITeamLocking tournament, bool canTeamsBeLocked)
        {
            tournament.CanTeamsBeLocked = canTeamsBeLocked;
        }

        public bool IsTournamentIdInDatabase(string tournamentId, bool isCaseSensitive = false)
        {
            if (isCaseSensitive)
            {
                foreach (var dataFile in _dataContext.TournamentDataFiles)
                {
                    if (dataFile.Tournament.Id.Equals(tournamentId))
                    {
                        return true;
                    }
                }
            }
            else
            {
                foreach (var dataFile in _dataContext.TournamentDataFiles)
                {
                    if (dataFile.Tournament.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public List<Tournament> GetAllTournaments()
        {
            return _dataContext.GetTournaments();
        }

        public List<Tournament> GetAllLadderTournaments()
        {
            List<Tournament> ladderTournaments = new();
            foreach (var tournament in _dataContext.TournamentDataFiles.Select(df => df.Tournament))
            {
                if (tournament is NormalLadderTournament)
                {
                    ladderTournaments.Add(tournament);
                }
                else if (tournament is DSRLadderTournament)
                {
                    ladderTournaments.Add(tournament);
                }
            }
            return ladderTournaments;
        }

        public List<Tournament> GetAllRoundRobinTournaments()
        {
            List<Tournament> roundRobinTournaments = new();
            foreach (var tournament in _dataContext.TournamentDataFiles.Select(df => df.Tournament))
            {
                if (tournament is NormalRoundRobinTournament || tournament is OpenRoundRobinTournament)
                {
                    roundRobinTournaments.Add(tournament);
                }
            }
            return roundRobinTournaments;
        }

        public List<Tournament> GetAllEliminationTournaments()
        {
            // TODO For when elimination tournaments are finally added

            return null;
        }

        public Tournament? GetTournamentById(string tournamentId)
        {
            foreach (var dataFile in _dataContext.TournamentDataFiles)
            {
                if (dataFile.Tournament.Id.Equals(tournamentId, StringComparison.OrdinalIgnoreCase))
                {
                    return dataFile.Tournament;
                }
            }
            return null;
        }

        public Tournament GetTournamentFromTeamName(string teamName)
        {
            foreach (var tournament in _dataContext.GetTournaments())
            {
                if (tournament.Teams.Any(t => t.Name.Equals(teamName, StringComparison.OrdinalIgnoreCase)))
                {
                    return tournament;
                }
            }
            return null;
        }

        public Tournament GetTournamentFromMatchId(string matchId)
        {
            foreach (var tournament in _dataContext.GetTournaments())
            {
                if (tournament.MatchLog.GetAllActiveMatches().Any(m => m.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase)))
                {
                    return tournament;
                }
                if (tournament.MatchLog.GetAllPostMatches().Any(m => m.Id.Equals(matchId, StringComparison.OrdinalIgnoreCase)))
                {
                    return tournament;
                }
            }
            return null;
        }

        public string? GenerateTournamentId()
        {
            bool isUnique = false;
            string uniqueId;

            while (!isUnique)
            {
                Random random = new();
                int randomInt = random.Next(100, 1000);
                uniqueId = $"T{randomInt}";

                // Check if the generated ID is unique
                if (!IsTournamentIdInDatabase(uniqueId))
                {
                    isUnique = true;
                    return uniqueId;
                }
            }
            return null;
        }

        public void ApplyTieBreakerRankings(Tournament tournament, List<string> tiedTeams, string winnerTeamName)
        {
            // Find the minimum rank among tied teams (should be 1 for first place tie)
            var tiedTeamObjects = tournament.Teams.Where(t => tiedTeams.Contains(t.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            int minRank = tiedTeamObjects.Min(t => t.Rank);

            // Get the winner team
            var winnerTeam = tiedTeamObjects.FirstOrDefault(t => t.Name.Equals(winnerTeamName, StringComparison.OrdinalIgnoreCase));
            if (winnerTeam == null)
            {
                return; // Should not happen, but just in case
            }

            // Assign ranks: winner gets minRank, others get minRank + 1, minRank + 2, etc.
            winnerTeam.Rank = minRank;

            // Get the other tied teams (excluding the winner)
            var otherTiedTeams = tiedTeamObjects.Where(t => !t.Name.Equals(winnerTeamName, StringComparison.OrdinalIgnoreCase)).ToList();

            // Assign sequential ranks to remaining tied teams
            int currentRank = minRank + 1;
            foreach (var team in otherTiedTeams)
            {
                team.Rank = currentRank;
                currentRank++;
            }

            // Adjust ranks of teams that were below the tied teams
            var teamsToShift = tournament.Teams
                .Where(t => !tiedTeams.Contains(t.Name, StringComparer.OrdinalIgnoreCase) && t.Rank >= minRank)
                .ToList();

            foreach (var team in teamsToShift)
            {
                team.Rank += tiedTeams.Count - 1; // Shift by number of tied teams minus 1 (since one already had minRank)
            }
        }
    }
}
