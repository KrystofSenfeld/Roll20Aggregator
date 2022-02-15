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
        private HashSet<string> allCharacters = new();
        private HashSet<string> allDieTypes = new();
        private Regex dieTypeQuery = new Regex("Rolling .*[0-9]*(d[0-9]+)", RegexOptions.IgnoreCase);
        private Regex dieTypeShortQuery = new Regex("[0-9]*(d[0-9]+)\"");
        private Regex rollValueQuery = new Regex(">([0-9]+)</");

        public ChatLog(string html) {
            HtmlDoc.LoadHtml(html);
            GetAllRolls();

            AllCharacters = allCharacters.ToList()
                .OrderBy(c => c)
                .ToList();

            AllDieTypes = RollStats.ValidDieTypes.ToList()
                .Where(d => allDieTypes.Contains(d))
                .ToList();
        }

        public HtmlDocument HtmlDoc { get; set; } = new();
        public List<Roll> AllRolls { get; set; } = new();
        public List<string> AllCharacters { get; set; } = new();
        public List<string> AllDieTypes { get; set; } = new();

        private void GetAllRolls() {
            string author = null;
            string prevAuthor = null;

            var messages = HtmlDoc.DocumentNode.SelectNodes("//div[contains(@class, \"message\")]");

            foreach (var message in messages) {
                var classes = message.GetClasses();
                GetAuthor(classes, message, ref author, ref prevAuthor);

                // Parsing roll block
                if (classes.Contains("rollresult")) {
                    HtmlNode diceRollNode = message.SelectSingleNode(".//div[contains(@class, \"diceroll\")]");

                    if (diceRollNode != null) {
                        string dieType = dieTypeShortQuery.Match(diceRollNode.OuterHtml).Groups[1].Value;
                        var rollTexts = message.SelectNodes(".//div[contains(@class, 'didroll')]");

                        foreach (var rollText in rollTexts) {
                            int.TryParse(rollText.InnerText, out int rollValue);
                            RegisterRoll(author, rollValue, dieType);
                        }

                        continue;
                    }
                }

                ParseInlineRolls(message, author); // This is done for all messages since roll blocks can, rarely, contain inline rolls.
            }
        }

        private void GetAuthor(IEnumerable<string> nodeClasses, HtmlNode node, ref string author, ref string prevAuthor) {
            Regex authorQuery = new Regex(@"\S*");

            if (nodeClasses.Contains("emote")) {
                author = authorQuery.Match(node.InnerText).Value;
            } else {
                string tempAuthor = node.SelectSingleNode(".//span[contains(@class, \"by\")]")?.InnerText;
                author = tempAuthor?.Remove(tempAuthor.Length - 1);
            }

            if (string.IsNullOrEmpty(author)) {
                author = prevAuthor;
            } else {
                prevAuthor = author;
            }
        }

        private void ParseInlineRolls(HtmlNode message, string author) {
            var rollNodeList = message.SelectNodes(".//span[contains(@class, \"inlinerollresult\")]");

            if (rollNodeList == null) {
                return;
            }

            foreach (var rollNode in rollNodeList) {
                string[] rollNodeAttrTexts = new[] {
                        rollNode.GetAttributeValue("title", null),
                        rollNode.GetAttributeValue("original-title", null),
                    };

                foreach (string rollNodeAttrText in rollNodeAttrTexts) {
                    if (string.IsNullOrEmpty(rollNodeAttrText)) {
                        continue;
                    }

                    string dieType = dieTypeQuery.Match(rollNodeAttrText).Groups[1].Value;
                    var rollValues = rollValueQuery.Matches(rollNodeAttrText);

                    for (int i = 0; i < rollValues.Count; i++) {
                        int.TryParse(rollValues[i].Groups[1].Value, out int rollValue);
                        RegisterRoll(author, rollValue, dieType);
                    }
                }
            }
        }

        private void RegisterRoll(string author, int rollValue, string dieType) {
            AllRolls.Add(new Roll(author, rollValue, dieType));
            allCharacters.Add(author);
            allDieTypes.Add(dieType);
        }
    }
}