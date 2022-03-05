using Microsoft.AspNetCore.Components;
using Roll20Aggregator.Services;

namespace Roll20Aggregator.Pages {
    [Route("/")]
    public partial class Index {
        [Inject] ParsingSession ParsingSession { get; set; }

        protected override void OnInitialized() {
            ParsingSession.IsInitialized = false;
            System.Console.WriteLine("Initializing home page");
        }
    }
}
