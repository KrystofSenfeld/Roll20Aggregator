using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roll20Aggregator.Models {
    public class ChatLog {
        public IBrowserFile ChatLogFile { get; set; }
        public List<RollDto> AllRolls { get; set; }
        public List<string> AllCharacters { get; set; }
        public List<string> AllDieTypes { get; set; }
    }
}