using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PracticeBlazorApp.Shared {
    public class ChatLog {
        public ChatLog(string html) {
            HtmlDoc.LoadHtml(html);
            GetAllRolls();
            AllCharacters = AllRolls.Select(r => r.RolledBy).Distinct().ToList();
        }

        public HtmlDocument HtmlDoc { get; set; } = new();
        public List<string> AllCharacters { get; set; } = new();
        public List<Roll> AllRolls { get; set; } = new();

        private void GetAllRolls() {
            string author, prevAuthor = null;
            Regex messageQuery = new Regex("<div class=\"message(.*?)</div>(?=<div class=\"message)");
            Regex dieTypeQuery = new Regex("Rolling [0-9]*(d[0-9]+)", RegexOptions.IgnoreCase);
            Regex rollValueQuery = new Regex(">([0-9]+)</");

            var messages = HtmlDoc.DocumentNode.SelectNodes("//div[contains(@class, \"message\")]");

            foreach (var message in messages)
            {
                var classes = message.GetClasses();

                author = ParseAuthor(classes, message);
                if (string.IsNullOrEmpty(author))
                {
                    author = prevAuthor;
                }
                else
                {
                    prevAuthor = author;
                }
                Console.WriteLine(author);

                if (classes.Contains("rollresult"))
                {
                    string dieType = dieTypeQuery.Match(message.SelectSingleNode(".//div[contains(@class, \"formula\")]").InnerText).Groups[0]?.Value;
                    var rollTexts = message.SelectNodes(".//div[contains(@class, 'didroll')]");

                    foreach (var rollText in rollTexts)
                    {
                        int.TryParse(rollText.InnerText, out int rollValue);
                        AllRolls.Add(new Roll(author, rollValue, dieType));
                    }

                    continue;
                }
                else
                {
                    var rollList = message.SelectNodes(".//span[contains(@class, \"inlinerollresult\")]");

                    if (rollList == null) {
                        continue;
                    }

                    foreach (var roll in rollList)
                    {
                        string[] rollText = new[] {
                            roll.GetAttributeValue("title", null),
                            roll.GetAttributeValue("original-title", null),
                        };

                        foreach (string text in rollText)
                        {
                            if (!string.IsNullOrEmpty(text))
                            {
                                var dieType = dieTypeQuery.Match(text);
                                var rollValues = rollValueQuery.Matches(text);

                                for (int i = 0; i < rollValues.Count; i++)
                                {
                                    int.TryParse(rollValues[i].Groups[0].Value, out int rollValue);
                                    AllRolls.Add(new Roll(author, rollValue, dieType.Value));
                                }
                            }
                        }
                    }
                }
            }
        }

        private string ParseAuthor(IEnumerable<string> nodeClasses, HtmlNode node)
        {
            Regex authorQuery = new Regex(@"\S*");

            if (nodeClasses.Contains("emote"))
            {
                return authorQuery.Match(node.InnerText).Value;
            }
            else
            {
                string tempAuthor = node.SelectSingleNode(".//span[contains(@class, \"by\")]")?.InnerText;
                return tempAuthor?.Remove(tempAuthor.Length - 1);
            }
        }
    }
}
