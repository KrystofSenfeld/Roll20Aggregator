using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Roll20AggregatorHosted.Services;

namespace Roll20AggregatorHosted.Pages {
    [Route("/")]
    public partial class Home : IDisposable {
        [Inject] ParsingSession ParsingSession { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        private IBrowserFile file;

        public string FileExtension => Path.GetExtension(file?.Name);
        public string FileSize { get; set; } = "1,645";
        public bool HasError { get; set; }

        public async void UploadFileAndParse(InputFileChangeEventArgs e) {
            FileSize = $"{e.File.Size / 1024:n0}";
            await StartAggregator(e.File);
        }

        public async Task StartAggregator(IBrowserFile file = null) {
            ParsingSession.Initialize();

            ParsingSession.SessionStatusChanged += ResultsChanged;
            ParsingSession.ParseResults.ParseResultsChanged += ResultsChanged;

            bool response = await ParsingSession.StartSession(file);

            if (response) {
                NavigationManager.NavigateTo("/results");
            } else {
                HasError = true;
                StateHasChanged();
            }
        }

        public void Dispose() {
            ParsingSession.SessionStatusChanged -= ResultsChanged;
            ParsingSession.ParseResults.ParseResultsChanged -= ResultsChanged;

            GC.SuppressFinalize(this);
        }

        private void ResultsChanged(object sender, EventArgs e) {
            InvokeAsync(StateHasChanged);
        }
    }
}
