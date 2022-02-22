using Microsoft.AspNetCore.Components;
using Roll20Aggregator.Models;
using Roll20Aggregator.Services;
using System.Threading.Tasks;

namespace Roll20Aggregator.Pages {
    [Route("/results")]
    public partial class Results {
        [Inject] ParsingSession ParsingSession { get; set; }

        public decimal GroupAverage() {
            decimal runningTotalSum = 0;
            int runningTotalCount = 0;
            foreach (var kvp in ParsingSession.CurrentStats) {
                runningTotalSum += kvp.Value.AverageRoll * kvp.Value.TotalRollsCount;
                runningTotalCount += kvp.Value.TotalRollsCount;
            }
            return runningTotalCount == 0 ? 0m : runningTotalSum / runningTotalCount;
        }

        async void DieSelected(ChangeEventArgs e) {
            string newDie = e.Value.ToString();

            if (string.IsNullOrEmpty(newDie)) {
                return;
            }

            ParsingSession.CurrentDieType = newDie;
            RollStats.SetRollKeys(newDie);
            await ParsingSession.SetStatsDict();
        }

        async void CharacterSelected(ChangeEventArgs e) {
            string newCharacter = e.Value.ToString();

            if (string.IsNullOrEmpty(newCharacter)) {
                return;
            }

            if (!ParsingSession.CurrentCharacters.Contains(newCharacter)) {
                await AddCharacter(newCharacter);
            }
        }

        async Task AddCharacter(string character) {
            ParsingSession.CurrentCharacters.Add(character);
            await ParsingSession.AddToStatsDict(character);
        }

        async Task RemoveCharacter(string character) {
            ParsingSession.CurrentCharacters.Remove(character);
            await ParsingSession.RemoveFromStatsDict(character);
        }
    }
}
