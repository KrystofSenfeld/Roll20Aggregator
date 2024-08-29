using Roll20AggregatorHosted.Models.Enums;
using Roll20AggregatorHosted.Utility;

namespace Roll20AggregatorHosted.Models {
    public class CharacterStatsDto {
        public CharacterStatsDto() {
            foreach (DieTypeEnum dieType in DiceUtility.ResultGroupsByDieType.Keys) {
                RollsByDieType[dieType.GetDescription()] = new List<RollDto>();
            }        
        }

        public int MessageCount { get; set; }
        public Dictionary<string, List<RollDto>> RollsByDieType { get; set; } = new();
        public List<RollDto> Rolls => RollsByDieType.Values.SelectMany(i => i).ToList();
    }
}