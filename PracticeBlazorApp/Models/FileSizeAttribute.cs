using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace Roll20Aggregator.Models {
    public class FileSizeAttribute : ValidationAttribute {
        public int MaxFileSize { get; set; }

        public FileSizeAttribute(int maxFileSize) {
            MaxFileSize = maxFileSize;
        }

        public override bool IsValid(object value) {
            IBrowserFile file = value as IBrowserFile;
            if (file != null) {
                return file.Size < MaxFileSize;
            }

            return true;
        }
    }
}
