using Microsoft.AspNetCore.Components;
using Roll20AggregatorHosted.Models;
using Roll20AggregatorHosted.Services;

namespace Roll20AggregatorHosted.Pages.Results {
    public partial class ResultsLogTab {
        [Parameter] public Results Results { get; set; }
        [Inject] ParsingSession ParsingSession { get; set; }

        public List<RollDto> LogRolls { get; set; } = new();

        public void UpdateLogRolls() {
            LogRolls = GetLogRolls();
        }

        private List<RollDto> GetLogRolls() {
            var names = Results.AllSelectedCharacters.ToHashSet();

            return Results.FilterBySelectedNames
                ? ParsingSession.ParseResults.RawRolls.Where(r => names.Contains(r.RolledBy)).ToList()
                : ParsingSession.ParseResults.RawRolls;
        }
    }
}
