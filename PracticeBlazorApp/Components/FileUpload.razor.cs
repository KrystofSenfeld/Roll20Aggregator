using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Roll20Aggregator.Models;
using Roll20Aggregator.Services;

namespace Roll20Aggregator.Components {
    public partial class FileUpload {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] ParsingSession ParsingSession { get; set; }

        private readonly FileUploadModel fileUploadModel = new();

        private async void RunDemo() {
            Debug.WriteLine("Upload and parse complete; redirecting to results.");
            NavigationManager.NavigateTo("/results");
        }

        private async void UploadFileAndParse(InputFileChangeEventArgs e) {
            await ParsingSession.StartSession(e.File);
            Debug.WriteLine("Upload and parse complete; redirecting to results.");
            NavigationManager.NavigateTo("/results");
        }
    }
}
