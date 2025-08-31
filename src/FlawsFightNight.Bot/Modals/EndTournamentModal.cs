using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Bot.Modals
{
    public class EndTournamentModal : IModal
    {
        public string Title => "End Tournament";

        [InputLabel("Tournament ID (Case Sensitive)")]
        [ModalTextInput("tournament_id_one", placeholder: "Enter the Tournament ID...", maxLength: 4)]
        public string TournamentIdOne { get; set; }

        [InputLabel("Tournament ID (Case Sensitive)")]
        [ModalTextInput("tournament_id_two", placeholder: "Re-enter the Tournament ID...", maxLength: 4)]
        public string TournamentIdTwo { get; set; }
    }
}
