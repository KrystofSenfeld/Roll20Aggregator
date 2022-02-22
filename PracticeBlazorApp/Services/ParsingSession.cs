using Microsoft.AspNetCore.Components.Forms;
using Roll20Aggregator.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Roll20Aggregator.Services {
    public class ParsingSession {
        public ChatLog ChatLog { get; set; }
        public List<string> CurrentCharacters { get; set; } = new();
        public string CurrentDieType { get; set; } = "";
        public Dictionary<string, RollStats> CurrentStats { get; set; } = new();
        public RollStats CurrentGlobalStats { get; set; } = new();

        public async Task GetChatLogFromFile(IBrowserFile file) {
            string text;
            using (StreamReader sr = new(file.OpenReadStream(1024 * 1024 * 20))) {
                text = await sr.ReadToEndAsync();
            }

            ChatLog = new ChatLog();
            await ChatLog.LoadHtml(text);
        }

        public async Task SetStatsDict() {
            CurrentStats = CurrentCharacters.ToDictionary(
                keySelector:c => c,
                elementSelector: c => new RollStats(CurrentDieType, c, ChatLog.AllRolls));
            
            CurrentGlobalStats = new RollStats(CurrentDieType, ChatLog.AllRolls);

            await Task.CompletedTask;
        }

        public async Task AddToStatsDict(string character) {
            CurrentStats.Add(character, new RollStats(CurrentDieType, character, ChatLog.AllRolls));
            await Task.CompletedTask;
        }

        public async Task RemoveFromStatsDict(string character) {
            CurrentStats.Remove(character);
            await Task.CompletedTask;
        }
    }
}
