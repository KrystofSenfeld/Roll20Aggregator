using Microsoft.AspNetCore.Components.Forms;
using Roll20Aggregator.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Roll20Aggregator.Services {
    public class ParsingSession {
        public bool IsInitialized { get; set; } = false;
        public bool IsLoading { get; private set; } = false;
        public ChatLog ChatLog { get; private set; } = new();
        public string CurrentDieType { get; private set; } = string.Empty;
        public Dictionary<string, RollStats> CurrentStats { get; private set; } = new();
        public RollStats CurrentGlobalStats { get; private set; } = new();

        public async Task StartSession(string demoFileText) {
            IsLoading = true;

            FileReader reader = new(ChatLog);
            FileParser parser = new(ChatLog, await reader.ReadDemoAsync(demoFileText));

            await parser.Parse();

            await SetDie(ChatLog.AllDieTypes.First());
            IsInitialized = true;
            IsLoading = false;
        }

        public async Task StartSession(IBrowserFile file) {
            IsLoading = true;

            ChatLog.ChatLogFile = file;

            FileReader reader = new(ChatLog);
            FileParser parser = new(ChatLog, await reader.ReadAsync());

            await parser.Parse();

            await SetDie(ChatLog.AllDieTypes.First());
            IsInitialized = true;
            IsLoading = false;
        }

        public async Task SetDie(string dieType) {
            CurrentDieType = dieType;
            await SetStatsDict();
        }

        private Task SetStatsDict() {
            CurrentStats = CurrentStats.Keys.ToDictionary(
                keySelector:c => c,
                elementSelector: c => new RollStats(CurrentDieType, c, ChatLog.AllRolls));
            
            CurrentGlobalStats = new RollStats(CurrentDieType, ChatLog.AllRolls);
            return Task.CompletedTask;
        }

        public Task AddToStatsDict(string character) {
            CurrentStats.Add(character, new RollStats(CurrentDieType, character, ChatLog.AllRolls));
            return Task.CompletedTask;
        }

        public Task RemoveFromStatsDict(string character) {
            CurrentStats.Remove(character);
            return Task.CompletedTask;
        }
    }
}
