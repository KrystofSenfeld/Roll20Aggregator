using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Forms;
using Roll20Aggregator.Models;

namespace Roll20Aggregator.Services {
    public class ParsingSession {
        private readonly HttpClient httpClient;

        public ParsingSession(HttpClient httpClient) {
            this.httpClient = httpClient;
        }

        public bool IsInitialized { get; set; } = false;
        public bool IsLoading { get; private set; } = false;

        public ParseResultsDto ParseResults { get; private set; } = new();

        public string CurrentDieType { get; private set; } = string.Empty;
        public Dictionary<string, RollStatsRowViewModel> CurrentStatsByName { get; private set; } = new();
        public RollStatsRowViewModel CurrentGlobalStats { get; private set; } = new();

        public async Task<bool> StartSession(IBrowserFile file) {
            IsLoading = true;

            HtmlDocument htmlDocument;

            if (file == null) {
                htmlDocument = await FileReader.ReadAsync(await httpClient.GetStringAsync("sample.txt"));
            } else {
                htmlDocument = await FileReader.ReadAsync(file);
            }

            if (htmlDocument == null) {
                Trace.WriteLine("Could not start session as no valid HTML was found.");
                return false;
            }

            FileParser parser = new(htmlDocument);
            ParseResults = parser.Parse();

            if (ParseResults == null || !ParseResults.AllRolls.Any()) {
                Trace.WriteLine("Could not start session as no rolls were parsed.");
                return false;
            }

            SetCurrentDieType(ParseResults.PrimaryDieType);

            IsInitialized = true;
            IsLoading = false;

            return true;
        }

        public void SetCurrentDieType(string dieType) {
            CurrentDieType = dieType;
            SetVisibleStats();
        }

        private void SetVisibleStats() {
            CurrentStatsByName = CurrentStatsByName.Keys.ToDictionary(
                keySelector: c => c,
                elementSelector: c => new RollStatsRowViewModel(CurrentDieType, c, ParseResults.AllRolls));
            
            CurrentGlobalStats = new RollStatsRowViewModel(CurrentDieType, ParseResults.AllRolls);
        }

        public void AddToVisibleStats(string character) {
            CurrentStatsByName.Add(character, new RollStatsRowViewModel(CurrentDieType, character, ParseResults.AllRolls));
        }

        public void RemoveFromVisibleStats(string character) {
            CurrentStatsByName.Remove(character);
        }
    }
}
