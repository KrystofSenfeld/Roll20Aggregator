using Microsoft.AspNetCore.Components;
using Roll20Aggregator.Services;

namespace Roll20Aggregator.Pages {
    [Route("/log")]
    public partial class Log {
        [Inject] ParsingSession ParsingSession { get; set; }
    }
}
