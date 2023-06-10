using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Roll20Aggregator.Services;

namespace Roll20Aggregator.Pages {
    [Route("/")]
    public partial class Home {
        [Inject] ParsingSession ParsingSession { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        protected override void OnInitialized() {
            ParsingSession.IsInitialized = false;
        }

        public async Task StartAggregator(IBrowserFile file = null) {
            bool response = await ParsingSession.StartSession(file);

            if (response) {
                NavigationManager.NavigateTo("/results");
            }
        }

        private async void UploadFileAndParse(InputFileChangeEventArgs e) {
            await StartAggregator(e.File);
        }
    }
}
