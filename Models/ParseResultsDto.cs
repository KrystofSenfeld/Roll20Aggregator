using Roll20AggregatorHosted.Models.Enums;
using Roll20AggregatorHosted.Utility;

namespace Roll20AggregatorHosted.Models {
    public class ParseResultsDto {
        public event EventHandler<EventArgs> ParseResultsChanged;

        public int ParsedMessageCount { get; set; }
        public int TotalMessageCount { get; set; }

        public string ParsedMessageCountDisplay => ParsedMessageCount.ToString("n0");
        public string TotalMessageCountDisplay => TotalMessageCount.ToString("n0");

        public Dictionary<string, CharacterStatsDto> RawCharacterStats { get; set; } = new();
        public List<RollDto> RawRolls { get; set; } = new();

        public HashSet<string> RawCharacterNames { get; private set; } = new();
        public HashSet<string> ValidCharacterNames { get; private set; } = new();

        public List<RollDto> AllValidRolls { get; private set; } = new();
        public Dictionary<string, List<RollDto>> ValidRollsByCharacter { get; private set; } = new();
        public Dictionary<DieTypeEnum, List<RollDto>> ValidRollsByDieType { get; private set; } = new();
        public List<DieTypeEnum> UsedDieTypes { get; private set; } = new();

        public void ResultsChanged() => ParseResultsChanged?.Invoke(this, EventArgs.Empty);

        public void FinishParsing() {
            RawCharacterNames = RawCharacterStats.Keys
                .OrderBy(name => name)
                .ToHashSet();

            RawRolls = RawCharacterStats.Values
                .SelectMany(i => i.Rolls)
                .OrderBy(r => r.MessageId)
                .ToList();

            AllValidRolls = RawCharacterStats.Values
                .SelectMany(kvp => kvp.RollsByDieType)
                .Where(kvp => DiceUtility.ValidDieTypeDescriptions.Contains(kvp.Key))
                .SelectMany(kvp => kvp.Value)
                .OrderBy(r => r.MessageId)
                .ToList();

            ValidRollsByDieType = AllValidRolls
                .GroupBy(r => r.RawDieType)
                .ToDictionary(group => (DieTypeEnum)DiceUtility.GetNumberOfFaces(group.Key), group => group.ToList());

            UsedDieTypes = ValidRollsByDieType.Keys
                .OrderBy(die => die)
                .ToList();

            ValidRollsByCharacter = AllValidRolls
                .GroupBy(r => r.RolledBy)
                .ToDictionary(group => group.Key, group => group.ToList());

            ValidCharacterNames = ValidRollsByCharacter.Keys
                .OrderBy(name => name)
                .ToHashSet();
        }
    }
}
