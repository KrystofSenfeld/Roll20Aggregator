using Microsoft.AspNetCore.Components.Forms;
using Roll20Aggregator.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Roll20Aggregator.Services {
    public class ParsingSession {
        public ChatLog ChatLog { get; set; }
        public bool IsLoading { get; set; }
        public List<string> CurrentCharacters { get; set; } = new();
        public string CurrentDieType { get; set; } = "";
        public Dictionary<string, RollStats> CurrentStats { get; set; } = new();
        public RollStats CurrentGlobalStats { get; set; } = new();

        public async Task StartSession(IBrowserFile file) {
            IsLoading = true;

            ChatLog = new ChatLog();
            ChatLog.ChatLogFile = file;

            Parser parser = new();
            await parser.Parse(ChatLog);

            IsLoading = false;
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
