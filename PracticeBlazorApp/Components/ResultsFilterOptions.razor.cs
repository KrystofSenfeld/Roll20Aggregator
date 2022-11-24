using Microsoft.AspNetCore.Components;
using Roll20Aggregator.Pages;
using Roll20Aggregator.Services;

namespace Roll20Aggregator.Components {
    public partial class ResultsFilterOptions {
        [Inject] ParsingSession ParsingSession { get; set; }
        [Parameter] public Results Parent { get; set; }

        public string GetColorClass(string character) => !IsSelected(character) ? "light" : "primary";
        public string GetIcon(string character) => !IsSelected(character) ? "plus" : "xmark";
        public string GetRollCount(string character) => ParsingSession.CurrentStatsByName[character].TotalRollsCount.ToString();

        public bool IsSelected(string character) => ParsingSession.CurrentStatsByName.ContainsKey(character);

        public void DieSelected(ChangeEventArgs e) {
            ParsingSession.SetCurrentDieType(e.Value.ToString());
            Parent.Refresh();
        }

        public void ToggleCharacterSelected(string character) {
            if (!ParsingSession.CurrentStatsByName.ContainsKey(character)) {
                ParsingSession.AddToVisibleStats(character);
            } else {
                ParsingSession.RemoveFromVisibleStats(character);
            }

            StateHasChanged();
            Parent.Refresh();
        }

        protected override bool ShouldRender() {
            return ParsingSession.IsInitialized;
        }
    }
}
