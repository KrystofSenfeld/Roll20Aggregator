using System;
using System.Collections.Generic;
using System.Linq;

namespace Roll20Aggregator.Models {
    public static class DiceResultGroups {
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
    }
}
