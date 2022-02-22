using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Roll20Aggregator.Models;
using Roll20Aggregator.Services;

namespace Roll20Aggregator.Components {
    public partial class FileUpload {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] ParsingSession ParsingSession { get; set; }

        private readonly FileUploadModel fileUploadModel = new();
        private bool isLoading;

        private async void UploadFileAndParse(InputFileChangeEventArgs e) {
            isLoading = true;

            await ParsingSession.GetChatLogFromFile(e.File);
            Parser parser = new();
            await parser.Parse(ParsingSession.ChatLog);

            isLoading = false;

            NavigationManager.NavigateTo("/results");
        }
    }
}
