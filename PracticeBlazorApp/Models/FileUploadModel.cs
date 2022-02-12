using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using System.IO;

namespace PracticeBlazorApp.Models
{
    public class FileUploadModel
    {
        public const int MaxFileSize = 1024 * 1024 * 50; // 50 MB

        [Required]
        [FileExtensions]
        [FileSize(MaxFileSize)]
        public IBrowserFile UploadedFile { get; set; }
    }
}
