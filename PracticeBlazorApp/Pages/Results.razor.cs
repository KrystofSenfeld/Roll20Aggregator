using Microsoft.AspNetCore.Components;
using Roll20Aggregator.Models;
using Roll20Aggregator.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Roll20Aggregator.Pages {
    [Route("/results")]
    public partial class Results : ComponentBase {
        [Inject] ParsingSession ParsingSession { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        public enum RollDisplayType {
            Count,
            Percent
        }

        public RollDisplayType DisplayType { get; set; } = RollDisplayType.Count;
        public ChiSquareTestResults TestResults => ParsingSession.CurrentGlobalStats.TestResults;
        public string TestConclusion {
            get {
                if (TestResults == null) {
                    return null;
                }

                if (TestResults.SampleSize < TestResults.MinimumRollsRequired) {
                    return "Not enough rolls to test randomness.";
                }

                return TestResults.Significant
                    ? "The rolls for this die deviate significantly from a random distribution. "
                    : "The rolls for this die follow a random distribution. ";
            }
        }
        public string TestResultColorClass {
            get {
                if (TestResults == null) {
                    return null;
                }

                if (TestResults.SampleSize < TestResults.MinimumRollsRequired) {
                    return "secondary";
                }

                return TestResults?.Significant ?? false ? "danger" : "success";
            }
        }

        public void Refresh() => StateHasChanged();

        public Task SwitchRollDisplayType(RollDisplayType type) {
            if (type != DisplayType) {
                DisplayType = type;
            }

            return Task.CompletedTask;
        }

        public decimal GroupAverage() {
            decimal runningTotalSum = 0;
            int runningTotalCount = 0;
            foreach (var kvp in ParsingSession.CurrentStatsByName) {
                runningTotalSum += kvp.Value.AverageRoll * kvp.Value.TotalRollsCount;
                runningTotalCount += kvp.Value.TotalRollsCount;
            }
            return runningTotalCount == 0 ? 0m : Math.Round(runningTotalSum / runningTotalCount, 2);
        }

        public decimal GroupPercent(string key) {
            decimal total = ParsingSession.CurrentStatsByName.Select(kvp => kvp.Value.RollsCount[key]).Sum();
            decimal dividend = ParsingSession.CurrentStatsByName.Select(kvp => kvp.Value.TotalRollsCount).Sum();

            return dividend == 0m ? 0m :
                Math.Round(total / dividend * 100m, 2);
        }

        protected override bool ShouldRender() {
            return ParsingSession.IsInitialized;
        }

        protected override void OnInitialized() {
            if (!ParsingSession.IsInitialized) {
                Console.WriteLine("No file was uploaded; redirecting to home screen.");
                NavigationManager.NavigateTo("/");
            }
        }
    }
}
