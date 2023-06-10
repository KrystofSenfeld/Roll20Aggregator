using System.Collections.Generic;

namespace Roll20Aggregator.Models {
    public class ParseResultsDto {
        public int ParsedMessagesCount { get; set; }

        public List<RollDto> AllRolls { get; set; }
        public List<string> AllCharacters { get; set; }
        public List<string> AllDieTypes { get; set; }

        public string PrimaryDieType { get; set; }
    }
}
