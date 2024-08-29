using Accord.Math;
using Accord.Statistics;
using Microsoft.AspNetCore.Components;
using Roll20AggregatorHosted.Models;
using Roll20AggregatorHosted.Models.Enums;
using Roll20AggregatorHosted.Services;
using Roll20AggregatorHosted.Utility;

namespace Roll20AggregatorHosted.Pages.Results {
    public partial class ResultsOverviewTab {
        [Parameter] public Results Results { get; set; }
        [Inject] ParsingSession ParsingSession { get; set; }

        public string ZScoreTooltip {
            get {
                return "Z Score is a normalized measure of how far a given value is from the mean.\n"
                    + "A positive Z score means the value is above average; a negative Z score means the value is below average.\n"
                    + "In this case, the sum of character's dice rolls is compared with their expected mean sum.";
            }
        }

        public string MessageCount { get; set; }
        public string DiceCount { get; set; }
        public List<DataItemDto> MessageChartData { get; set; } = new();
        public List<DataItemDto> MessageTableData { get; set; } = new();
        public List<DataItemDto> DiceByNameChartData { get; set; } = new();
        public List<DataItemDto> DiceByNameTableData { get; set; } = new();
        public List<DataItemDto> DiceByTypeChartData { get; set; } = new();
        public List<DataItemDto> DiceByTypeTableData { get; set; } = new();
        public List<DataItemDto> RollerData { get; set; } = new();

        public void UpdateOverviewData() {
            MessageCount = GetMessageCount();
            DiceCount = GetDiceCount();
            MessageChartData = GetMessageData(DataDisplayTypeEnum.Chart);
            MessageTableData = GetMessageData(DataDisplayTypeEnum.Table);
            DiceByNameChartData = GetDiceByNameData(DataDisplayTypeEnum.Chart);
            DiceByNameTableData = GetDiceByNameData(DataDisplayTypeEnum.Table);
            DiceByTypeChartData = GetDiceByTypeData(DataDisplayTypeEnum.Chart);
            DiceByTypeTableData = GetDiceByTypeData(DataDisplayTypeEnum.Table);
            RollerData = GetRollerData(DataDisplayTypeEnum.Table);
        }

        private string GetMessageCount() {
            var sum = Results.FilterBySelectedNames
                ? Results.AllSelectedCharacters.Sum(name => ParsingSession.ParseResults.RawCharacterStats[name].MessageCount)
                : ParsingSession.ParseResults.TotalMessageCount;

            return sum.ToString("n0");
        }

        private string GetDiceCount() {
            var sum = Results.FilterBySelectedNames
                ? Results.AllSelectedCharacters.Sum(name =>
                    ParsingSession.ParseResults.RawCharacterStats[name].Rolls.Sum(r => r.DiceRolledCount))

                : ParsingSession.ParseResults.RawCharacterStats.SelectMany(kvp => kvp.Value.Rolls).Sum(r => r.DiceRolledCount);

            return sum.ToString("n0");
        }

        private List<DataItemDto> GetMessageData(DataDisplayTypeEnum displayType) {
            IEnumerable<DataItemDto> data = new List<DataItemDto>();

            if (!Results.FilterBySelectedNames) {
                data = ParsingSession.ParseResults.RawCharacterStats
                    .Select(kvp => new DataItemDto {
                        Category = kvp.Key,
                        Value = kvp.Value.MessageCount,
                    });
            } else {
                data = Results.SelectedNames
                    .Select(name => new DataItemDto {
                        Category = name,
                        Value = Results.IsGroup(name)
                            ? Results.GroupsByName[name].Sum(n => ParsingSession.ParseResults.RawCharacterStats[n].MessageCount)
                            : ParsingSession.ParseResults.RawCharacterStats[name].MessageCount,
                    });
            }

            return StandardizeData(data.ToList(), displayType);
        }

        private List<DataItemDto> GetDiceByNameData(DataDisplayTypeEnum displayType) {
            IEnumerable<DataItemDto> data = new List<DataItemDto>();

            if (!Results.FilterBySelectedNames) {
                data = ParsingSession.ParseResults.RawCharacterStats
                    .Select(kvp => new DataItemDto {
                        Category = kvp.Key,
                        Value = kvp.Value.Rolls.Sum(r => r.DiceRolledCount),
                    });
            } else {
                data = Results.SelectedNames
                    .Select(name => new DataItemDto {
                        Category = name,
                        Value = Results.IsGroup(name)
                            ? Results.GroupsByName[name].Sum(n => ParsingSession.ParseResults.RawCharacterStats[n].Rolls.Sum(r => r.DiceRolledCount))
                            : ParsingSession.ParseResults.RawCharacterStats[name].Rolls.Sum(r => r.DiceRolledCount),
                    });
            }

            return StandardizeData(data.ToList(), displayType);
        }

        private List<DataItemDto> GetDiceByTypeData(DataDisplayTypeEnum displayType) {
            var rolls = Results.FilterBySelectedNames
                ? Results.AllSelectedCharacters.SelectMany(name => ParsingSession.ParseResults.RawCharacterStats[name].Rolls)
                : ParsingSession.ParseResults.RawCharacterStats.Values.SelectMany(i => i.Rolls);

            var data = rolls
                .ToLookup(r => r.RawDieType)
                .Select(die => new DataItemDto {
                    Category = die.Key,
                    Value = die.Sum(r => r.DiceRolledCount),
                })
                .ToList();

            return StandardizeData(data.ToList(), displayType);
        }

        private List<DataItemDto> GetRollerData(DataDisplayTypeEnum displayType) {
            var data = new List<DataItemDto>();
            var names = Results.FilterBySelectedNames ? Results.SelectedNames : ParsingSession.ParseResults.RawCharacterNames;

            foreach (string name in names) {
                var rollGroup = !Results.IsGroup(name)
                    ? ParsingSession.ParseResults.RawCharacterStats[name].Rolls
                    : Results.GroupsByName[name].SelectMany(n => ParsingSession.ParseResults.RawCharacterStats[n].Rolls);

                if (!rollGroup.Any()) {
                    continue;
                }

                var rollsByDieType = rollGroup.ToLookup(charRolls => (DieTypeEnum)DiceUtility.GetNumberOfFaces(charRolls.RawDieType));

                var observedScores = new List<double>();
                var expectedMeans = new List<double>();
                var observedCounts = new List<int>();
                var expectedVariances = new List<double>();

                foreach (var dieType in rollsByDieType) {
                    var rolls = dieType.SelectMany(r => r.Values).ToArray();

                    observedScores.Add(rolls.Sum());
                    expectedMeans.Add(((int)dieType.Key + 1) / 2d * rolls.Length);
                    observedCounts.Add(rolls.Length);
                    expectedVariances.Add(rolls.Length * (Math.Pow((int)dieType.Key, 2) - 1) / 12);
                }

                var observedCountsArray = observedCounts.ToArray();
                var expectedVariancesArray = expectedVariances.ToArray();

                double combinedObservedScore = observedScores.Sum();
                double combinedMean = expectedMeans.Sum();

                double pooledStd = Measures.PooledStandardDeviation(observedCountsArray, expectedVariancesArray);

                data.Add(new DataItemDto {
                    Category = name,
                    Value = (combinedObservedScore - combinedMean) / pooledStd,
                    SampleSize = observedCounts.Sum(),
                });
            }

            return StandardizeData(data, displayType);
        }

        private List<DataItemDto> StandardizeData(IList<DataItemDto> data, DataDisplayTypeEnum displayType) {
            return displayType == DataDisplayTypeEnum.Chart
                ? StandardizeDataForChart(data)
                : StandardizeDataForTable(data);
        }

        private List<DataItemDto> StandardizeDataForTable(IList<DataItemDto> data) {
            var orderedData = data
                .OrderByDescending(i => i.Value)
                .ThenByDescending(i => i.SampleSize)
                .ToList();

            for (int i = 0; i < orderedData.Count; i++) {
                orderedData[i].Number = i + 1;
            }

            return orderedData;
        }

        private List<DataItemDto> StandardizeDataForChart(IList<DataItemDto> data) {
            var orderedData = data
                .Where(d => d.Value > 0)
                .OrderByDescending(i => i.Value)
                .ToList();

            if (orderedData.Count <= 10) {
                return orderedData;
            }

            var groupedCategory = new DataItemDto {
                Category = $"Other ({orderedData.Count - 9})",
                Value = orderedData.Skip(7).Sum(data => data.Value),
            };

            return orderedData
                .Take(9)
                .Concat(new List<DataItemDto> { groupedCategory })
                .ToList();
        }
    }
}
