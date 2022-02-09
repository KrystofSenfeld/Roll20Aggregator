using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Forms;
using PracticeBlazorApp.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticeBlazorApp {
    public class ChatLogState {
        public ChatLog ChatLog { get; set; } = new("");
        public List<string> CurrentCharacters { get; set; } = new();
        public string CurrentDieType { get; set; } = "";
        public Dictionary<string, RollStatsCharacter> CurrentStats { get; set; } = new();

        public async Task GetLogFromFile(IBrowserFile file) {
            string text;
            using (StreamReader sr = new(file.OpenReadStream())) {
                text = await sr.ReadToEndAsync();
            }

            ChatLog = new ChatLog(text);
        }

        public async Task SetStatsDict() {
            CurrentStats = CurrentCharacters.ToDictionary(
                keySelector: c => c, elementSelector: c => new RollStatsCharacter(CurrentDieType, c, ChatLog.AllRolls));
        }

        public async Task AddToStatsDict(string character) {
            CurrentStats.Add(character, new RollStatsCharacter(CurrentDieType, character, ChatLog.AllRolls));
        }

        public async Task RemoveFromStatsDict(string character) {
            CurrentStats.Remove(character);
        }
    }
}
