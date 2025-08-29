using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Modals
{
    public class DeleteTeamModal : IModal
    {
        public string Title => "Delete Team";

        [InputLabel("Team Name (Case Sensitive)")]
        [ModalTextInput("team_name_one", placeholder: "Enter the team name...", maxLength: 50)]
        public string TeamNameOne { get; set; }

        [InputLabel("Team ID (Case Sensitive)")]
        [ModalTextInput("team_id_two", placeholder: "Re-enter the team name...", maxLength: 50)]
        public string TeamNameTwo { get; set; }
    }
}
