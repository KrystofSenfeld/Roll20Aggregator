using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PracticeBlazorApp.Models
{
    public class FileSizeAttribute : ValidationAttribute
    {
        public int MaxFileSize { get; set; }

        public FileSizeAttribute(int maxFileSize)
        {
            MaxFileSize = maxFileSize;
        }

        public override bool IsValid(object value)
        {
            IBrowserFile file = value as IBrowserFile;
            if (file != null)
            {
                return file.Size < MaxFileSize;
            }

            return true;
        }
    }
}
