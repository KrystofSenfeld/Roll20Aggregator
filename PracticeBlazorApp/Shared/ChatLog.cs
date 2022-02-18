using HtmlAgilityPack;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PracticeBlazorApp.Shared {
    public class ChatLog {
        public HtmlDocument HtmlDoc { get; set; } = new();
        public List<Roll> AllRolls { get; set; } = new();
        public List<string> AllCharacters { get; set; } = new();
        public List<string> AllDieTypes { get; set; } = new();

        public async Task LoadHtml(string html) {
            HtmlDoc.LoadHtml(html);
            await Task.CompletedTask;
        }
    }
}