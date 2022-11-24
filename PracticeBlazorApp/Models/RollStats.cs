using Accord.Statistics.Testing;
using Roll20Aggregator.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roll20Aggregator.Models
{
    public class RollStats {
        private List<double> observedRollValues = new(); // used for statistical analysis

        public int TotalRollsCount { get; private set; } = 0;
        public decimal AverageRoll { get; private set; } = 0;
        public static string[] ResultGroups { get; private set; } = Array.Empty<string>();
        public Dictionary<string, int> RollsCount { get; private set; } = new();
        public Dictionary<string, decimal> RollsPercent { get; private set; } = new();
        public ChiSquareTestResults TestResults { get; set; }

        public RollStats() {}
        public RollStats(string dieType, List<RollDto> rolls) {
            ResultGroups = DiceUtility.GetResultGroups(dieType);
            ParseRolls(dieType, rolls);
            CalculateSignificance(dieType);
        }

        public RollStats(string dieType, string character, List<RollDto> rolls) {
            ResultGroups = DiceUtility.GetResultGroups(dieType);
            ParseRolls(dieType, character, rolls);
        }

        private void ParseRolls(string dieType, List<RollDto> rolls) => ParseRolls(dieType, string.Empty, rolls, false);

        private void ParseRolls(string dieType, string character, List<RollDto> rolls) =>
            ParseRolls(dieType, character, rolls, true);

        private void ParseRolls(string dieType, string character, List<RollDto> rolls, bool shouldFilter) {
            int runningTotal = 0;
            RollsCount = ResultGroups.ToDictionary(keySelector: k => k, elementSelector: _ => 0);

            foreach (RollDto roll in rolls) {
                if (shouldFilter && roll.RolledBy != character) {
                    continue;
                }

                if (roll.DieType != dieType) {
                    continue;
                }

                observedRollValues.Add(roll.Value);
                TotalRollsCount++;
                runningTotal += roll.Value;

                DiceUtility.GetResultGroupsByRoll(roll.Value, ResultGroups)
                    .ForEach(k => RollsCount[k]++);
            }

            AverageRoll = TotalRollsCount == 0 ? 0m : Math.Round(runningTotal / (decimal)TotalRollsCount, 2);
            RollsPercent = ResultGroups.ToDictionary(
                keySelector: k => k,
                elementSelector: k => TotalRollsCount == 0 
                    ? 0m
                    : Math.Round(RollsCount[k] / (decimal)TotalRollsCount * 100, 2)
            );
        }

        private void CalculateSignificance(string dieType) {
            double[] observed = observedRollValues
                .GroupBy(roll => roll)
                .Select(group => (double)group.Count())
                .ToArray();

            observed = observed
                .Concat(Enumerable.Repeat(0d, DiceUtility.GetNumberOfFaces(dieType) - observed.Length))
                .ToArray();

            double[] expected = Enumerable
                .Repeat((double)observedRollValues.Count / DiceUtility.GetNumberOfFaces(dieType), observed.Length)
                .ToArray();

            int degreesOfFreedom = DiceUtility.GetNumberOfFaces(dieType) - 1;

            var chi = new ChiSquareTest(expected, observed, degreesOfFreedom);
            TestResults = new ChiSquareTestResults(Math.Round(chi.Statistic, 4), chi.DegreesOfFreedom, observedRollValues.Count,
                Math.Round(chi.PValue, 4), chi.Significant, DiceUtility.GetNumberOfFaces(dieType) * 5);

            observedRollValues.Clear();
        }
    }
}
