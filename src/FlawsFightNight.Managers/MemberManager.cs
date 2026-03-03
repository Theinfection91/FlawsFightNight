using Discord;
using Discord.WebSocket;
using FlawsFightNight.Core.Enums;
using FlawsFightNight.Core.Helpers;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
using FlawsFightNight.Core.Models.UT2004;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class MemberManager : BaseDataDriven
    {
        public MemberManager(DataManager dataManager) : base("MemberManager", dataManager)
        {

        }

        #region Save and Load
        public async Task SaveMemberProfile(MemberProfile profileToSave)
        {
            foreach (var memberProfileData in _dataManager.MemberProfileFiles)
            {
                if (profileToSave.DiscordId == memberProfileData.MemberProfile.DiscordId)
                {
                    await _dataManager.SaveMemberProfileFile(profileToSave);
                    return;
                }
            }
        }

        public async Task LoadAllMemberProfiles()
        {
            await _dataManager.LoadAllMemberProfileFiles();
        }

        public async Task SaveAndReloadMemberProfiles()
        {
            await _dataManager.SaveAllMemberProfileFiles();
            await LoadAllMemberProfiles();
        }
        #endregion

        #region Discord Command Related
        public bool IsMemberCountCorrect(int membersCount, int teamSize)
        {
            // Case 1: For team sizes of 20 or less, the member count must match the team size.
            if (teamSize <= 20 && membersCount == teamSize)
                return true;

            // Case 2: For team sizes 21 or greater, exactly 20 members must be provided.
            if (teamSize >= 21 && membersCount == 20)
                return true;

            // Any other case is invalid.
            return false;
        }

        public bool IsMemberRegisteredInTournament(ulong memberId, Tournament tournament)
        {
            foreach (Team team in tournament.Teams)
            {
                foreach (Member member in team.Members)
                {
                    if (member.DiscordId == memberId)
                    {
                        return true; // Member is already registered in the tournament
                    }
                }
            }
            return false; // Member is not registered in the tournament
        }

        public List<Member> ConvertIUsersToMembers(List<IUser> members)
        {
            List<Member> membersList = new List<Member>();

            foreach (IUser member in members)
            {
                string displayName;

                // Check if the member can be cast to SocketGuildUser
                if (member is SocketGuildUser guildUser)
                {
                    // If the user has a nickname (DisplayName), use it
                    displayName = !string.IsNullOrEmpty(guildUser.DisplayName) ? guildUser.DisplayName : guildUser.Username;
                }
                else
                {
                    // If it's not a guild user, use the global Username
                    displayName = member.Username;
                }

                // Create a new Member object with the Discord ID and display name
                membersList.Add(new Member(member.Id, displayName));
            }

            return membersList;
        }
        #endregion

        public MemberProfile CreateMemberProfile(ulong discordId, string displayName)
        {
            return new MemberProfile(discordId, displayName);
        }

        public void AddProfileToDatabase(MemberProfile profile)
        {
            var file = _dataManager.CreateNewMemberProfileFile(profile);
            _dataManager.AddNewMemberProfileFile(file);
        }

        public bool DoesMemberProfileExist(ulong discordId)
        {
            foreach (var memberProfileData in _dataManager.MemberProfileFiles)
            {
                if (discordId == memberProfileData.MemberProfile.DiscordId)
                {
                    return true;
                }
            }
            return false;
        }

        public MemberProfile? GetMemberProfile(ulong discordId)
        {
            return _dataManager.GetMemberProfile(discordId);
        }

        #region Tournament Specific Stats
        public void IncrementMembersTournamentsPlayed(List<Member> members)
        {
            foreach (var member in members)
            {
                var profile = GetMemberProfile(member.DiscordId);
                if (profile != null)
                {
                    profile.IncrementTournamentsPlayed();
                    profile.AddExperience(TournamentLevelGuide.GetExperienceForAction(TournamentExperienceAction.ParticipateTournament));
                }
            }
        }

        public void RecordWinLossForMembers(Team winningTeam, Team losingTeam)
        {
            foreach (var member in winningTeam.Members)
            {
                var profile = GetMemberProfile(member.DiscordId);
                if (profile != null)
                {
                    profile.RecordWinLoss(true);
                    profile.AddExperience(TournamentLevelGuide.GetExperienceForAction(TournamentExperienceAction.WinMatch));
                }
            }

            foreach (var member in losingTeam.Members)
            {
                var profile = GetMemberProfile(member.DiscordId);
                if (profile != null)
                {
                    profile.RecordWinLoss(false);
                    profile.AddExperience(TournamentLevelGuide.GetExperienceForAction(TournamentExperienceAction.LoseMatch));
                }
            }
        }

        // TODO Still need this added to logic
        public void AwardFirstPlaceTournamentWinForMembers(Team championTeam)
        {
            foreach (var member in championTeam.Members)
            {
                var profile = GetMemberProfile(member.DiscordId);
                if (profile != null)
                {
                    profile.IncrementTournamentsWon();
                    profile.AddExperience(TournamentLevelGuide.GetExperienceForAction(TournamentExperienceAction.FirstPlaceTournament));
                }
            }
        }

        // TODO Still need this added to logic
        public void AwardSecondPlaceTournamentWinForMembers(Team runnerUpTeam)
        {
            foreach (var member in runnerUpTeam.Members)
            {
                var profile = GetMemberProfile(member.DiscordId);
                if (profile != null)
                {
                    profile.AddExperience(TournamentLevelGuide.GetExperienceForAction(TournamentExperienceAction.SecondPlaceTournament));
                }
            }
        }

        // TODO Still need this added to logic
        public void AwardThirdPlaceTournamentWinForMembers(Team thirdPlaceTeam)
        {
            foreach (var member in thirdPlaceTeam.Members)
            {
                var profile = GetMemberProfile(member.DiscordId);
                if (profile != null)
                {
                    profile.AddExperience(TournamentLevelGuide.GetExperienceForAction(TournamentExperienceAction.ThirdPlaceTournament));
                }
            }
        }

        public void AwardCompletionTournamentForMembers(List<Member> allMembers)
        {
            foreach (var member in allMembers)
            {
                var profile = GetMemberProfile(member.DiscordId);
                if (profile != null)
                {
                    profile.AddExperience(TournamentLevelGuide.GetExperienceForAction(TournamentExperienceAction.CompleteTournament));
                }
            }
        }
        #endregion

        #region UT2004 Player Profile Related
        public bool IsUT2004GUIDRegistered(string guid)
        {
            foreach (var memberProfileData in _dataManager.MemberProfileFiles)
            {
                if (memberProfileData.MemberProfile.RegisteredUT2004GUIDs.Contains(guid, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool DoesUT2004PlayerProfileExist(string playerGuid)
        {
            foreach (var file in _dataManager.UT2004PlayerProfileFiles)
            {
                if (file.PlayerProfile.Guid.Equals(playerGuid, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public UT2004PlayerProfile? GetUT2004PlayerProfile(string playerGuid)
        {
            return _dataManager.GetUT2004PlayerProfile(playerGuid);
        }
        #endregion
    }
}
