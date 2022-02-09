using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PracticeBlazorApp.Shared {
    public class ChatLog {
        public ChatLog(string html) {
            HtmlDoc.LoadHtml(html);

            AllCharacters = new() { "Adam", "Bob", "Eve" };

            GetAllRolls();
        }

        public HtmlDocument HtmlDoc { get; set; } = new();
        public List<string> AllCharacters { get; set; } = new();
        public List<Roll> AllRolls { get; set; } = new();

        private void GetAllRolls() {
            AllRolls.Add(new Roll("Adam", 20, "d100"));
            AllRolls.Add(new Roll("Adam", 40, "d100"));
            AllRolls.Add(new Roll("Adam", 11, "d20"));
            AllRolls.Add(new Roll("Eve", 30, "d100"));
            AllRolls.Add(new Roll("Eve", 15, "d20"));
            AllRolls.Add(new Roll("Eve", 11, "d100"));
            AllRolls.Add(new Roll("Bob", 20, "d20"));
            AllRolls.Add(new Roll("Bob", 1, "d20"));
            AllRolls.Add(new Roll("Bob", 73, "d100"));
        }
    }
}
