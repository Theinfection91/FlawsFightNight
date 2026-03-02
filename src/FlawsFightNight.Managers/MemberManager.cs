using Discord;
using Discord.WebSocket;
using FlawsFightNight.Core.Models;
using FlawsFightNight.Core.Models.Tournaments;
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

        public async Task SaveAndReloadMemberProfiles(MemberProfile profileToSave)
        {
            await SaveMemberProfile(profileToSave);
            await LoadAllMemberProfiles();
        }
        #endregion

        public async Task<MemberProfile>? CreateNewMemberProfile(ulong discordId, string displayName)
        {
            MemberProfile newProfile = new(discordId, displayName);
            if (newProfile == null) return null;
            return newProfile;
        }

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

        public List<Member> ConvertMembersListToObjects(List<IUser> members)
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
    }
}
