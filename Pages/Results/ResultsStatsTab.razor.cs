using Accord.Statistics.Testing;
using Roll20AggregatorHosted.Models.Enums;
using Roll20AggregatorHosted.Models;
using Roll20AggregatorHosted.Services;
using Microsoft.AspNetCore.Components;
using Roll20AggregatorHosted.Utility;

namespace Roll20AggregatorHosted.Pages.Results {
    public partial class ResultsStatsTab {
        [Parameter] public Results Results {  get; set; }
        [Inject] ParsingSession ParsingSession { get; set; }

        public RollDisplayType DisplayType { get; set; } = RollDisplayType.Count;
        public string CurrentSortProperty { get; private set; } = nameof(DieStatsRowViewModel.Name);
        public SortDirectionEnum SortDirection { get; private set; } = SortDirectionEnum.Ascending;

        public ChiSquareTestResultsDto ChiSquareResults { get; private set; }
        public DieStatsRowViewModel GlobalStats { get; private set; }
        public DieStatsRowViewModel SelectionStats { get; private set; }

        public Dictionary<string, DieStatsRowViewModel> CurrentDieStatsByName { get; private set; } = new();
        public Dictionary<string, DieStatsRowViewModel> SelectedDieStatsByName => CurrentDieStatsByName
            .Where(kvp => Results.SelectedNames.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        public IList<string> ResultGroups => DiceUtility.ResultGroupsByDieType.GetValueOrDefault(Results.CurrentDieType);

        public void Initialize() {
            DieTypeEnum mostUsedDieType = ParsingSession.ParseResults.ValidRollsByDieType
                .Select(kvp => new {
                    DieType = kvp.Key,
                    kvp.Value.Count,
                })
                .OrderByDescending(i => i.Count)
                .Select(i => i.DieType)
                .First();

            Results.CurrentDieType = mostUsedDieType;
        }

        public void OnDieTypeChanged() {
            GlobalStats = new DieStatsRowViewModel(
                Results.CurrentDieType, ParsingSession.ParseResults.ValidRollsByDieType[Results.CurrentDieType]);

            GenerateDieStatViewModels();
            UpdateSelectionStats();
            CalculateSignificanceForCurrentDie();
        }

        public void GenerateDieStatViewModels() {
            CurrentDieStatsByName = new Dictionary<string, DieStatsRowViewModel>();

            foreach (string name in Results.AllNames) {
                if (!Results.IsGroup(name)) {
                    AddCharacterDieStatsViewModel(name);
                } else {
                    AddGroupDieStatsViewModel(name);
                }
            }
        }

        public void AddCharacterDieStatsViewModel(string character) {
            CurrentDieStatsByName.Add(character, new DieStatsRowViewModel(
                Results.CurrentDieType,
                ParsingSession.ParseResults.RawCharacterStats[character].RollsByDieType[Results.CurrentDieType.GetDescription()],
                character));
        }

        public void AddGroupDieStatsViewModel(string groupName) {
            var groupRolls = Results.GroupsByName[groupName]
                .SelectMany(name => ParsingSession.ParseResults.RawCharacterStats[name].RollsByDieType[Results.CurrentDieType.GetDescription()])
                .ToList();

            CurrentDieStatsByName.Add(groupName, new DieStatsRowViewModel(Results.CurrentDieType, groupRolls, groupName));
        }

        private void SwitchRollDisplayType(RollDisplayType type) {
            if (type != DisplayType) {
                DisplayType = type;
            }
        }

        public void UpdateSelectionStats() {
            SelectionStats = new DieStatsRowViewModel(Results.CurrentDieType, Results.AllSelectedCharacters.SelectMany(name =>
                ParsingSession.ParseResults.RawCharacterStats[name].RollsByDieType[Results.CurrentDieType.GetDescription()]).ToList());
        }

        public void SortBy<T>(string propName, Func<DieStatsRowViewModel, T> propGetter) {
            if (CurrentSortProperty == propName) {
                SortDirection = SortDirection == SortDirectionEnum.Ascending
                    ? SortDirectionEnum.Descending
                    : SortDirectionEnum.Ascending;
            } else {
                CurrentSortProperty = propName;
                SortDirection = SortDirectionEnum.Ascending;
            }

            var stats = SortDirection == SortDirectionEnum.Ascending
                ? CurrentDieStatsByName.OrderBy(kvp => propGetter(kvp.Value))
                : CurrentDieStatsByName.OrderByDescending(kvp => propGetter(kvp.Value));

            CurrentDieStatsByName = stats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            StateHasChanged();
        }

        #region Chi Square Test

        private void CalculateSignificanceForCurrentDie() {
            var observedValues = ParsingSession.ParseResults.ValidRollsByDieType[Results.CurrentDieType]
                .SelectMany(i => i.Values)
                .GroupBy(i => i)
                .Select(i => i.Count())
                .Select(i => (double)i)
                .ToArray();

            var expectedValues =
                Enumerable.Repeat((double)GlobalStats.DiceCount / (int)Results.CurrentDieType, (int)Results.CurrentDieType)
                .ToArray();

            int degreesOfFreedom = (int)Results.CurrentDieType - 1;

            var chi = new ChiSquareTest(expectedValues, observedValues, degreesOfFreedom);

            ChiSquareResults = new ChiSquareTestResultsDto {
                ChiSquareStatistic = Math.Round(chi.Statistic, 4),
                DegreesOfFreedom = chi.DegreesOfFreedom,
                SampleSize = GlobalStats.DiceCount,
                PValue = Math.Round(chi.PValue, 4),
                IsSignificant = chi.Significant,
                MinimumRollsRequired = (int)Results.CurrentDieType * 5,
            };
        }

        private ChiSquareTestResultsDto TestResults => ChiSquareResults;
        private string TestConclusion {
            get {
                if (TestResults == null) {
                    return null;
                }

                if (TestResults.SampleSize < TestResults.MinimumRollsRequired) {
                    return "Not enough rolls to test randomness.";
                }

                return TestResults.IsSignificant
                    ? "The rolls for this die deviate significantly from a random distribution. "
                    : "The rolls for this die follow a random distribution. ";
            }
        }

        private string TestResultColorClass {
            get {
                if (TestResults == null) {
                    return null;
                }

                if (TestResults.SampleSize < TestResults.MinimumRollsRequired) {
                    return "secondary";
                }

                return TestResults?.IsSignificant ?? false ? "danger" : "success";
            }
        }

        #endregion
    }
}
