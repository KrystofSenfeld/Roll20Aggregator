using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PracticeBlazorApp.Shared {
    public class ChatLog
    {
        public ChatLog(IBrowserFile file)
        {
            UploadedFile = file;
            Parser = new Parser(this);

            //HtmlDoc.LoadHtml(html);
            //GetAllRolls();
        }

        public IBrowserFile UploadedFile { get; set; }
        //public HtmlDocument HtmlDoc { get; set; } = new();
        public Parser Parser { get; set; }
        public List<Roll> AllRolls { get; set; } = new();
        public List<string> AllCharacters { get; set; } = new();
        public List<string> AllDieTypes { get; set; } = new();

    }
}
