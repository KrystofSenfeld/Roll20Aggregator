using System;
using System.Collections.Generic;
using System.Linq;

namespace Roll20Aggregator.Models {
    public class RollStats {
        public int TotalRollsCount { get; private set; } = 0;
        public decimal AverageRoll { get; private set; } = 0;
        public static string[] RollCategories { get; private set; } = Array.Empty<string>();
        public Dictionary<string, int> RollsCount { get; private set; } = new();
        public Dictionary<string, decimal> RollsPercent { get; private set; } = new();

        public RollStats() { }

        public RollStats(string dieType, List<Roll> rolls) {
            RollCategories = RollKeys.GetRollKeys(dieType);
            ParseRolls(dieType, rolls);
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

                TotalRollsCount++;
                runningTotal += roll.Value;

                GetRollKeys(roll.Value).ForEach(k => RollsCount[k]++);
            }

            AverageRoll = TotalRollsCount == 0 ? 0m : runningTotal / (decimal)TotalRollsCount;
            RollsPercent = RollCategories.ToDictionary(
                keySelector: k => k,
                elementSelector: k => TotalRollsCount == 0 ? 0m : RollsCount[k] / (decimal)TotalRollsCount
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
    }
}
