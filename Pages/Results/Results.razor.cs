using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Roll20AggregatorHosted.Components;
using Roll20AggregatorHosted.Models.Enums;
using Roll20AggregatorHosted.Services;

namespace Roll20AggregatorHosted.Pages.Results {
    [Route("/results")]
    public partial class Results : ComponentBase {
        [Inject] ParsingSession ParsingSession { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }

        private DieTypeEnum currentDieType;
        private AddGroupDialog addGroupDialog;
        private ResultsOverviewTab resultsOverviewTab;
        private ResultsStatsTab resultsStatsTab;
        private ResultsLogTab resultsLogTab;

        protected override async Task OnAfterRenderAsync(bool firstRender) {
            await JSRuntime.InvokeVoidAsync("initializeTooltips");

            if (firstRender) {
                resultsStatsTab.Initialize();
                resultsOverviewTab.UpdateOverviewData();
                resultsLogTab.UpdateLogRolls();

                StateHasChanged();
            }
        }

        public ResultsTabEnum CurrentTab { get; private set; } = ResultsTabEnum.Overview;
        public bool FilterBySelectedNames { get; set; }

        public DieTypeEnum CurrentDieType {
            get => currentDieType;
            set {
                if (currentDieType != value) {
                    currentDieType = value;

                    if (resultsStatsTab != null) {
                        resultsStatsTab.OnDieTypeChanged();
                    }
                }
            }
        }

        public bool AllowSelection { get; private set; } = true;
        public HashSet<string> SelectedNames { get; set; } = new();
        public Dictionary<string, HashSet<string>> GroupsByName { get; private set; } = new();

        public HashSet<string> AllNames => ParsingSession.ParseResults.ValidCharacterNames
            .Concat(GroupsByName.Keys)
            .OrderBy(name => name)
            .ToHashSet();

        public IList<string> SelectedSingleCharacters => SelectedNames
            .Where(name => !IsGroup(name))
            .ToList();

        public IList<string> SelectedGroups => SelectedNames
            .Where(name => IsGroup(name))
            .ToList();

        public IList<string> AllSelectedCharacters => SelectedSingleCharacters
            .Concat(SelectedGroups.SelectMany(name => GroupsByName[name]))
            .ToList();

        public void ChangeTab(ResultsTabEnum tab) {
            CurrentTab = tab;
        }

        public bool IsGroup(string name) {
            return GroupsByName.ContainsKey(name);
        }

        #region Filter Pane

        public string CharactersTooltip {
            get {
                if (ExcludedCharacterCount == 0) {
                    return string.Empty;
                }

                return "This list contains only those characters that have rolled any dice throughout the game. " +
                    $"{ExcludedCharacterCount} character{(ExcludedCharacterCount > 1 ? "s have" : "has")} been excluded.";
            }
        }

        public bool HasOverlap => AllSelectedCharacters.Count != AllSelectedCharacters.Distinct().Count();

        public string OverlapWarning => "Your selection contains overlap between groups and/or individual characters.";

        private int ExcludedCharacterCount => ParsingSession.ParseResults.RawCharacterNames
            .Except(ParsingSession.ParseResults.ValidCharacterNames)
            .Count();

        public bool IsSelected(string name) => SelectedNames.Contains(name);

        public string GetColorClass(string name) {
            return !IsSelected(name) ? "light" : "primary";
        }

        public string GetDiceCount(string name) {
            return resultsStatsTab.CurrentDieStatsByName[name].DiceCount.ToString("n0");
        }

        public void OnOpenGroupDialogClick() {
            addGroupDialog.Initialize();
        }

        public void CreateGroup(string groupName, HashSet<string> characters) {
            GroupsByName.Add(groupName, characters);
            resultsStatsTab.AddGroupDieStatsViewModel(groupName);
            ToggleNameSelected(groupName);

            StateHasChanged();
        }

        public async Task DeleteGroup(string groupName) {
            AllowSelection = false;

            RemoveNameFromSelection(groupName);
            GroupsByName.Remove(groupName);

            // When the group is deleted, the list of names after the deleted group is shifted upwards.
            // The following should prevent the mouse-up events of the average click from triggering on the next name in the list.
            await Task.Delay(200);
            AllowSelection = true;
        }

        public void ToggleNameSelected(string name) {
            if (!SelectedNames.Contains(name)) {
                AddNameToSelection(name);
            } else {
                RemoveNameFromSelection(name);
            }

            resultsOverviewTab.UpdateOverviewData();
            resultsStatsTab.UpdateSelectionStats();
            resultsLogTab.UpdateLogRolls();
        }

        public void AddNameToSelection(string name) {
            SelectedNames.Add(name);
        }

        public void RemoveNameFromSelection(string name) {
            SelectedNames.Remove(name);
        }

        public void OnSelectAllClick() {
            foreach (string name in AllNames) {
                if (!IsSelected(name)) {
                    AddNameToSelection(name);
                }
            }
        }

        public void OnSelectNoneClick() {
            foreach (string name in AllNames) {
                if (IsSelected(name)) {
                    RemoveNameFromSelection(name);
                }
            }
        }

        #endregion
    }
}