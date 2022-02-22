using System;
using System.Collections.Generic;

namespace Roll20Aggregator.Models {
    public class RollKeys {
        public static Dictionary<string, string[]> Keys = new() {
            { "d4", new[] { "1", "2", "3", "4" } },
            { "d6", new[] { "1", "2", "3", "4", "5", "6" } },
            { "d8", new[] { "1", "2", "3", "4", "5", "6", "7", "8" } },
            { "d10", new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" } },
            { "d12", new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" } },
            { "d20", new[] { "1", "1-5", "6-10", "11-15", "16-20", "20" } },
            { "d100", new[] { "1", "1-10", "11-20", "21-30", "31-40", "41-50", "51-60", "61-70", "71-80", "81-90", "91-100", "100" } },
        };

        public static string[] GetRollKeys(string dieType) {
            bool isValidType = Keys.TryGetValue(dieType, out string[] keys);
            return isValidType ? keys : Array.Empty<string>();
        }
    }
}
