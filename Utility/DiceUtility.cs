using Roll20AggregatorHosted.Models;
using Roll20AggregatorHosted.Models.Enums;

namespace Roll20AggregatorHosted.Utility {
    public static class DiceUtility {
        public static int GetNumberOfFaces(string dieType) {
            int.TryParse(dieType[1..], out int faces);
            return faces;
        }

        public static Dictionary<DieTypeEnum, List<string>> ResultGroupsByDieType => new() {
            { DieTypeEnum.D2, Enumerable.Range(1, 2).Select(n => n.ToString()).ToList() },
            { DieTypeEnum.D4, Enumerable.Range(1, 4).Select(n => n.ToString()).ToList() },
            { DieTypeEnum.D6, Enumerable.Range(1, 6).Select(n => n.ToString()).ToList() },
            { DieTypeEnum.D8, Enumerable.Range(1, 8).Select(n => n.ToString()).ToList() },
            { DieTypeEnum.D10, Enumerable.Range(1, 10).Select(n => n.ToString()).ToList() },
            { DieTypeEnum.D12, Enumerable.Range(1, 12).Select(n => n.ToString()).ToList() },
            { DieTypeEnum.D20, new() { "1", "1-5", "6-10", "11-15", "16-20", "20", } },
            { DieTypeEnum.D100, new() {
                "1", "1-10", "11-20", "21-30", "31-40", "41-50", "51-60", "61-70", "71-80", "81-90", "91-100", "100",
            }},
        };

        public static HashSet<DieTypeEnum> ValidDieTypes => ResultGroupsByDieType.Keys.ToHashSet();

        public static HashSet<string> ValidDieTypeDescriptions => ResultGroupsByDieType.Keys
            .Select(d => d.GetDescription())
            .ToHashSet();

        public static List<string> GetResultGroupsByDieType(DieTypeEnum dieType) {
            bool isValidType = ResultGroupsByDieType.TryGetValue(dieType, out List<string> resultGroups);
            return isValidType ? resultGroups : new List<string>();
        }

        public static List<string> GetResultGroupsForRoll(RollDto roll) {
            List<string> resultGroups = new();

            if (roll.RawDieType == null) {
                return resultGroups;
            }

            foreach (int rollValue in roll.Values) {
                resultGroups.AddRange(GetResultGroupForRoll((DieTypeEnum)DiceUtility.GetNumberOfFaces(roll.RawDieType), rollValue));
            }

            return resultGroups;
        }

        public static List<string> GetResultGroupForRoll(DieTypeEnum dieType, int rollValue) {
            var resultGroups = new List<string>();

            if (dieType == DieTypeEnum.None) {
                return resultGroups;
            }

            if (dieType == DieTypeEnum.D20) {
                if (rollValue == 1 || rollValue == 20) {
                    resultGroups.Add(rollValue.ToString());
                }

                int index = (int)Math.Ceiling(rollValue / 5m);
                resultGroups.Add(ResultGroupsByDieType[dieType][index]);
            } else if (dieType == DieTypeEnum.D100) {
                if (rollValue == 1 || rollValue == 100) {
                    resultGroups.Add(rollValue.ToString());
                }

                int index = (int)Math.Ceiling(rollValue / 10m);
                resultGroups.Add(ResultGroupsByDieType[dieType][index]);
            } else {
                resultGroups.Add(rollValue.ToString());
            }

            return resultGroups;
        }
    }
}
