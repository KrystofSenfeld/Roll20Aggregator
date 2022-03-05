using Microsoft.AspNetCore.Components;
using Roll20Aggregator.Pages;
using Roll20Aggregator.Services;

namespace Roll20Aggregator.Components {
    public partial class ResultsFilterOptions {
        [Inject] ParsingSession ParsingSession { get; set; }
        [Parameter] public Results Parent { get; set; }

        public string GetColorClass(string character) => !IsSelected(character) ? "light" : "primary";
        public string GetIcon(string character) => !IsSelected(character) ? "plus" : "xmark";
        public string GetRollCount(string character) => ParsingSession.CurrentStats[character].TotalRollsCount.ToString();

        public bool IsSelected(string character) => ParsingSession.CurrentStats.ContainsKey(character);

        public async void DieSelected(ChangeEventArgs e) {
            await ParsingSession.SetDie(e.Value.ToString());
            Parent.Refresh();
        }

        public async void ToggleCharacterSelected(string character) {
            if (!ParsingSession.CurrentStats.ContainsKey(character)) {
                await ParsingSession.AddToStatsDict(character);
            } else {
                await ParsingSession.RemoveFromStatsDict(character);
            }

            StateHasChanged();
            Parent.Refresh();
        }

        protected override bool ShouldRender() {
            return ParsingSession.IsInitialized;
        }
    }
}
