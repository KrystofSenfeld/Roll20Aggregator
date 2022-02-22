using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace Roll20Aggregator.Models {
    public class FileUploadModel {
        public const int MaxFileSize = 1024 * 1024 * 50; // 50 MB

        [Required]
        [FileExtensions]
        [FileSize(MaxFileSize)]
        public IBrowserFile UploadedFile { get; set; }
    }
}
