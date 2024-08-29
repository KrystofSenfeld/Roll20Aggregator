using Roll20AggregatorHosted.Models.Enums;
using Roll20AggregatorHosted.Utility;

namespace Roll20AggregatorHosted.Models {
    public class DieStatsRowViewModel {
        public string Name { get; private set; }
        public int DiceCount { get; private set; }
        public decimal AverageResult { get; private set; }
        public Dictionary<string, int> ResultCountByGroup { get; private set; } = new();
        public Dictionary<string, decimal> ResultPercentByGroup { get; private set; } = new();

        public DieStatsRowViewModel(DieTypeEnum dieType, IList<RollDto> rolls, string name = null) {
            Name = name;

            ResultCountByGroup = DiceUtility.GetResultGroupsByDieType(dieType)
                .ToDictionary(key => key, element => 0);

            CategorizeRolls(rolls);
        }

        private void CategorizeRolls(IList<RollDto> rolls) {
            int rollValueTotal = 0;

            foreach (RollDto roll in rolls) {
                DiceCount += roll.Values.Count;
                rollValueTotal += roll.Values.Sum();

                DiceUtility.GetResultGroupsForRoll(roll).ForEach(key => ResultCountByGroup[key]++);
            }

            AverageResult = DiceCount == 0 ? 0m : Math.Round(rollValueTotal / (decimal)DiceCount, 2);

            ResultPercentByGroup = ResultCountByGroup.ToDictionary(
                key => key.Key,
                element => DiceCount == 0
                    ? 0m
                    : Math.Round(element.Value / (decimal)DiceCount * 100, 2));
        }
    }
}
