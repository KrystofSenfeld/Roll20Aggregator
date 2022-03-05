using Accord.Statistics.Testing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Roll20Aggregator.Models {
    public class RollStats {
        private List<double> observedRollValues = new();

        public int TotalRollsCount { get; private set; } = 0;
        public decimal AverageRoll { get; private set; } = 0;
        public static string[] RollCategories { get; private set; } = Array.Empty<string>();
        public Dictionary<string, int> RollsCount { get; private set; } = new();
        public Dictionary<string, decimal> RollsPercent { get; private set; } = new();
        public ChiSquareTestResults TestResults { get; set; }

        public RollStats() {}
        public RollStats(string dieType, List<Roll> rolls) {
            RollCategories = RollKeys.GetRollKeys(dieType);
            ParseRolls(dieType, rolls);
            CalculateSignificance(dieType);
        }

        public RollStats(string dieType, string character, List<Roll> rolls) {
            RollCategories = RollKeys.GetRollKeys(dieType);
            ParseRolls(dieType, character, rolls);
        }

        public static void SetRollKeys(string dieType) {
            RollCategories = RollKeys.GetRollKeys(dieType);
        }

        private void ParseRolls(string dieType, List<Roll> rolls) => ParseRolls(dieType, string.Empty, rolls, false);
        private void ParseRolls(string dieType, string character, List<Roll> rolls) => ParseRolls(dieType, character, rolls, true);
        private void ParseRolls(string dieType, string character, List<Roll> rolls, bool shouldFilter) {
            int runningTotal = 0;
            RollsCount = RollCategories.ToDictionary(keySelector: k => k, elementSelector: _ => 0);

            foreach (Roll roll in rolls) {
                if (shouldFilter && roll.RolledBy != character) {
                    continue;
                }

                if (roll.DieType != dieType) {
                    continue;
                }

                observedRollValues.Add(roll.Value);
                TotalRollsCount++;
                runningTotal += roll.Value;

                GetRollKeys(roll.Value).ForEach(k => RollsCount[k]++);
            }

            AverageRoll = TotalRollsCount == 0 ? 0m : Math.Round(runningTotal / (decimal)TotalRollsCount, 2);
            RollsPercent = RollCategories.ToDictionary(
                keySelector: k => k,
                elementSelector: k => TotalRollsCount == 0 ? 0m : Math.Round(RollsCount[k] / (decimal)TotalRollsCount * 100, 2)
            );
        }

        private List<string> GetRollKeys(int roll) {
            List<string> keys = new();
            bool hasBeenFound = false;

            foreach (var key in RollCategories) {
                if (IsInRange(roll, key)) {
                    keys.Add(key);
                    hasBeenFound = true;
                } else {
                    if (hasBeenFound) {
                        break;
                    }
                }
            }

            return keys;
        }

        private bool IsInRange(int roll, string rollKey) {
            int[] limits = rollKey.Split("-").Select(int.Parse).ToArray();
            if (limits.Length == 1) {
                return roll == limits[0];
            } else if (limits.Length == 2) {
                return roll >= limits[0] && roll <= limits[1];
            } else {
                return false;
            }
        }

        private void CalculateSignificance(string dieType) {
            double[] observed = observedRollValues.GroupBy(roll => roll).Select(group => (double)group.Count()).ToArray();
            observed = observed.Concat(Enumerable.Repeat(0d, GetDieFaces(dieType) - observed.Length)).ToArray();
            double[] expected = Enumerable.Repeat((double)observedRollValues.Count / GetDieFaces(dieType), observed.Length).ToArray();
            int degreesOfFreedom = GetDieFaces(dieType) - 1;

            var chi = new ChiSquareTest(expected, observed, degreesOfFreedom);
            TestResults = new ChiSquareTestResults(Math.Round(chi.Statistic, 4), chi.DegreesOfFreedom, observedRollValues.Count,
                Math.Round(chi.PValue, 4), chi.Significant, GetDieFaces(dieType) * 5);

            observedRollValues.Clear();
        }

        private int GetDieFaces(string dieType) => int.Parse(dieType[1..]);
    }
}
