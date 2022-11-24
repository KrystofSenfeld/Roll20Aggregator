using System;
using System.Collections.Generic;
using System.Linq;

namespace Roll20Aggregator.Utility {
    public static class DiceUtility {
        public static int GetNumberOfFaces(string dieType) => int.Parse(dieType[1..]);

        public static Dictionary<string, string[]> ResultGroupsByDieType = new() {
            { "d4", Enumerable.Range(1, 4).Select(n => n.ToString()).ToArray() },
            { "d6", Enumerable.Range(1, 6).Select(n => n.ToString()).ToArray() },
            { "d8", Enumerable.Range(1, 8).Select(n => n.ToString()).ToArray() },
            { "d10", Enumerable.Range(1, 10).Select(n => n.ToString()).ToArray() },
            { "d12", Enumerable.Range(1, 12).Select(n => n.ToString()).ToArray() },
            { "d20", new[] { "1", "1-5", "6-10", "11-15", "16-20", "20" } },
            { "d100", new[] { "1", "1-10", "11-20", "21-30", "31-40", "41-50", "51-60", "61-70", "71-80", "81-90", "91-100", "100" } },
        };

        public static string[] GetResultGroups(string dieType) {
            bool isValidType = ResultGroupsByDieType.TryGetValue(dieType, out string[] resultGroups);
            return isValidType ? resultGroups : Array.Empty<string>();
        }

        public static List<string> GetResultGroupsByRoll(int roll, string[] resultGroups) {
            List<string> keys = new();
            bool hasBeenFound = false;

            foreach (string key in resultGroups) {
                if (IsRollInGroup(roll, key)) {
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

        public static bool IsRollInGroup(int roll, string rollKey) {
            int[] limits = rollKey
                .Split("-")
                .Select(rk => int.Parse(rk))
                .ToArray();

            if (limits.Length == 1) {
                return roll == limits[0];
            } else if (limits.Length == 2) {
                return roll >= limits[0] && roll <= limits[1];
            }

            return false;
        }
    }
}
