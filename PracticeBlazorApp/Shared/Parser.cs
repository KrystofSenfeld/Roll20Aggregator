using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using PracticeBlazorApp.Models;

namespace PracticeBlazorApp.Shared
{
    public class Parser
    {
        private ChatLog chatLog;

        private HashSet<string> allCharacters = new();
        private HashSet<string> allDieTypes = new();

        private string author;
        private string prevAuthor;
        private Regex dieTypeQuery = new Regex("Rolling .*[0-9]*(d[0-9]+)", RegexOptions.IgnoreCase);
        private Regex dieTypeShortQuery = new Regex("[0-9]*(d[0-9]+)\"");
        private Regex rollValueQuery = new Regex(">([0-9]+)</");

        public Parser(ChatLog chatLog)
        {
            this.chatLog = chatLog;
        }

        public async Task Parse()
        {
            await ReadFile();

            chatLog.AllCharacters = allCharacters.ToList()
                .OrderBy(c => c)
                .ToList();

            chatLog.AllDieTypes = RollStats.ValidDieTypes.ToList()
                .Where(d => allDieTypes.Contains(d))
                .ToList();
        }

        private async Task ReadFile()
        {
            using (StreamReader stream = new(chatLog.UploadedFile.OpenReadStream(FileUploadModel.MaxFileSize)))
            {
                int bufferSize = 1024 * 10; // 10 KB
                char[] buffer = new char[bufferSize];
                StringBuilder adjustedBuffer = new();

                string messageTag = "<div class=\"message";
                string endTag = "<script";
                bool foundFirst = false;
                int foundIndex;

                while (await stream.ReadAsync(buffer, 0, bufferSize) > 0)
                {
                    adjustedBuffer.Append(new string(buffer));
                    string adjustedBufferString = adjustedBuffer.ToString();

                    while ((foundIndex = adjustedBufferString.IndexOf(messageTag)) >= 0) {
                        Console.WriteLine(adjustedBuffer.Length);

                        if (foundFirst) {
                            HtmlDocument htmlDoc = new();
                            htmlDoc.LoadHtml(messageTag + adjustedBufferString.Substring(0, foundIndex));
                            ParseMessageForRolls(htmlDoc);
                        } else {
                            foundFirst = true;
                        }
                        adjustedBuffer.Remove(0, foundIndex + messageTag.Length);
                        adjustedBufferString = adjustedBuffer.ToString();
                    }

                    if (adjustedBufferString.Contains(endTag)) {
                        HtmlDocument htmlDoc = new();
                        htmlDoc.LoadHtml(messageTag + adjustedBufferString);
                        ParseMessageForRolls(htmlDoc);
                    } else if (!adjustedBufferString.Contains(messageTag.First())) {
                        adjustedBuffer.Clear();
                    }
                }
            }
        }

        private void ParseMessageForRolls(HtmlDocument htmlDoc)
        {
            var message = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, \"message\")]");

            if (message == null) {
                return;
            }

            var classes = message.GetClasses();
            GetAuthor(classes, message, ref author, ref prevAuthor);

            // Parsing roll block
            if (classes.Contains("rollresult"))
            {
                HtmlNode diceRollNode = message.SelectSingleNode(".//div[contains(@class, \"diceroll\")]");

                if (diceRollNode != null)
                {
                    string dieType = dieTypeShortQuery.Match(diceRollNode.OuterHtml).Groups[1].Value;
                    var rollTexts = message.SelectNodes(".//div[contains(@class, 'didroll')]");

                    foreach (var rollText in rollTexts)
                    {
                        int.TryParse(rollText.InnerText, out int rollValue);
                        RegisterRoll(author, rollValue, dieType);
                    }

                    return;
                }
            }

            ParseInlineRolls(message, author); // This is done for all messages since roll blocks can, rarely, contain inline rolls.

            //var messages = HtmlDoc.DocumentNode.SelectNodes("//div[contains(@class, \"message\")]");

            //foreach (var message in messages)
            //{
            //}
        }

        private void GetAuthor(IEnumerable<string> nodeClasses, HtmlNode node, ref string author, ref string prevAuthor)
        {
            Regex authorEmoteQuery = new Regex(@"\S*");

            if (nodeClasses.Contains("emote"))
            {
                author = authorEmoteQuery.Match(node.InnerText).Value;
            }
            else
            {
                string tempAuthor = node.SelectSingleNode(".//span[contains(@class, \"by\")]")?.InnerText;
                author = tempAuthor?.Remove(tempAuthor.Length - 1);
            }

            if (string.IsNullOrEmpty(author))
            {
                author = prevAuthor;
            }
            else
            {
                prevAuthor = author;
            }
        }

        private void ParseInlineRolls(HtmlNode message, string author)
        {
            var rollNodeList = message.SelectNodes(".//span[contains(@class, \"inlinerollresult\")]");

            if (rollNodeList == null)
            {
                return;
            }

            foreach (var rollNode in rollNodeList)
            {
                string[] rollNodeAttrTexts = new[] {
                        rollNode.GetAttributeValue("title", null),
                        rollNode.GetAttributeValue("original-title", null),
                    };

                foreach (string rollNodeAttrText in rollNodeAttrTexts)
                {
                    if (string.IsNullOrEmpty(rollNodeAttrText))
                    {
                        continue;
                    }

                    string dieType = dieTypeQuery.Match(rollNodeAttrText).Groups[1].Value;
                    var rollValues = rollValueQuery.Matches(rollNodeAttrText);

                    for (int i = 0; i < rollValues.Count; i++)
                    {
                        int.TryParse(rollValues[i].Groups[1].Value, out int rollValue);
                        RegisterRoll(author, rollValue, dieType);
                    }
                }
            }
        }

        private void RegisterRoll(string author, int rollValue, string dieType)
        {
            chatLog.AllRolls.Add(new Roll(author, rollValue, dieType));
            allCharacters.Add(author);
            allDieTypes.Add(dieType);
        }
    }
}
