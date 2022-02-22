using HtmlAgilityPack;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roll20Aggregator.Models {
    public class ChatLog {
        public HtmlDocument HtmlDoc { get; set; }
        public List<Roll> AllRolls { get; set; }
        public List<string> AllCharacters { get; set; }
        public List<string> AllDieTypes { get; set; }

        public async Task LoadHtml(string html) {
            HtmlDoc = new();
            HtmlDoc.LoadHtml(html);
            await Task.CompletedTask;
        }
    }
}