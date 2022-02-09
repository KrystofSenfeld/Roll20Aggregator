using System;
using System.Collections.Generic;
using System.Linq;

namespace PracticeBlazorApp {
    public class RollStatsCharacter {

        public int TotalRollsCount { get; private set; } = 0;
        public decimal AverageRoll { get; private set; } = 0;
        public static string[] RollKeys { get; private set; } = Array.Empty<string>();
        public Dictionary<string, int> RollsCount { get; private set; } = new();
        public Dictionary<string, decimal> RollsPercent { get; private set; } = new();

        public RollStatsCharacter(string dieType, List<Roll> rolls) {
            SetRollKeys(dieType);
            ParseRolls(dieType, rolls);
        }

        public RollStatsCharacter(string dieType, string character, List<Roll> rolls) {
            SetRollKeys(dieType);
            ParseRolls(dieType, character, rolls);
        }

        public static void SetRollKeys(string dieType) {
            RollKeys =
                dieType == "d20" ?
                    RollKeys = new string[] { "1", "1-5", "6-10", "11-15", "16-20", "20" }
                : dieType == "d100" ?
                    RollKeys = new string[] { "1", "1-10", "11-20", "21-30", "31-40", "41-50", "51-60", "61-70", "71-80", "81-90", "91-100", "100" }
                : RollKeys = Array.Empty<string>();
        }

        private void ParseRolls(string dieType, List<Roll> rolls) {
            int runningTotal = 0;
            RollsCount = RollKeys.ToDictionary(keySelector: k => k, elementSelector: _ => 0);

            foreach (Roll roll in rolls) {
                if (roll.DieType != dieType) {
                    continue;
                }

                TotalRollsCount++;
                runningTotal += roll.Value;

                GetRollKeys(roll.Value).ForEach(k => RollsCount[k]++);
            }

            AverageRoll = TotalRollsCount == 0 ? 0m : runningTotal / (decimal)TotalRollsCount;
            RollsPercent = RollKeys.ToDictionary(
                keySelector: k => k,
                elementSelector: k => TotalRollsCount == 0 ? 0m : RollsCount[k] / (decimal)TotalRollsCount
            );
        }

        private void ParseRolls(string dieType, string character, List<Roll> rolls) {
            int runningTotal = 0;
            RollsCount = RollKeys.ToDictionary(keySelector: k => k, elementSelector: _ => 0);

            foreach (Roll roll in rolls) {
                if (roll.RolledBy != character) {
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
            RollsPercent = RollKeys.ToDictionary(
                keySelector: k => k,
                elementSelector: k => TotalRollsCount == 0 ? 0m : RollsCount[k] / (decimal)TotalRollsCount
            );
        }

        private List<string> GetRollKeys(int roll) {
            List<string> keys = new();
            bool hasBeenFound = false;

            foreach (var key in RollKeys) {
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
