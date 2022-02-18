using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PracticeBlazorApp.Models;

namespace PracticeBlazorApp.Shared {
    public partial class FileUpload {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] ParsingSession ParsingSession { get; set; }

        private FileUploadModel fileUploadModel = new();
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
