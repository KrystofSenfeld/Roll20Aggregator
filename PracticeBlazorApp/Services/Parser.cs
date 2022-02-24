using HtmlAgilityPack;
using Roll20Aggregator.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Roll20Aggregator.Services {
    public class Parser {
        private ChatLog chatLog;

        private HashSet<string> allCharacters = new();
        private HashSet<string> allDieTypes = new();

        private List<Roll> allRolls = new();
        private HashSet<int> emoteIndices = new();
        private Dictionary<string, List<string>> AvatarToCharacter = new();

        private string currentCharacter = string.Empty;
        private Regex authorQuery = new Regex(@"\S*");
        private Regex dieTypeQuery = new Regex("Rolling .*[0-9]*(d[0-9]+)", RegexOptions.IgnoreCase);
        private Regex rollValueQuery = new Regex(">([0-9]+)</");

        public async Task Parse(ChatLog chatLog) {
            this.chatLog = chatLog;
            await BufferedReadAndParse();

            Console.WriteLine($"{emoteIndices.Count} emote rolls have been recorded:\n{string.Join(" ", emoteIndices)}.");

            Console.WriteLine(TryResolveEmoteEntries());

            chatLog.AllRolls = allRolls.ToList();

            chatLog.AllCharacters = allCharacters.ToList()
                .OrderBy(c => c)
                .ToList();

            chatLog.AllDieTypes = RollKeys.Keys.Keys.ToList()
                .Where(d => allDieTypes.Contains(d))
                .ToList();

            await Task.CompletedTask;
        }

        private async Task BufferedReadAndParse() {
            using (StreamReader stream = new(chatLog.ChatLogFile.OpenReadStream(FileUploadModel.MaxFileSize))) {
                int bufferSize = 1024 * 10; // 1 KB
                char[] buffer = new char[bufferSize];
                StringBuilder adjustedBuffer = new();
                List<string> messagesFromBuffer = new();

                string messageTag = "<div class=\"message";
                string endTag = "<script";
                bool foundFirst = false;
                int foundIndex;

                while (await stream.ReadAsync(buffer, 0, bufferSize) > 0) {
                    adjustedBuffer.Append(new string(buffer));
                    string adjustedBufferString = adjustedBuffer.ToString();

                    // Search buffer for occurrences of message opening tags and add messages to list
                    while ((foundIndex = adjustedBufferString.IndexOf(messageTag)) >= 0) {
                        if (foundFirst) {
                            messagesFromBuffer.Add(messageTag + adjustedBufferString.Substring(0, foundIndex));
                        } else {
                            foundFirst = true;
                        }
                        adjustedBuffer.Remove(0, foundIndex + messageTag.Length);
                        adjustedBufferString = adjustedBuffer.ToString();
                    }

                    // Handle what's left in the buffer after all messages have been found.
                    // If there is a <, don't clear the buffer, as maybe the buffer split a message or end tag.
                    if (adjustedBufferString.Contains(endTag)) {
                        messagesFromBuffer.Add(messageTag + adjustedBufferString + "></script>");
                    } else if (!adjustedBufferString.Contains(messageTag.First())) {
                        adjustedBuffer.Clear();
                    }

                    foreach (string message in messagesFromBuffer) {
                        Console.WriteLine("----------------<>---------------");
                        Console.WriteLine(message);
                        HtmlDocument htmlDoc = new();
                        htmlDoc.LoadHtml(message);
                        ParseMessagesForRolls(htmlDoc);
                    }
                    Console.WriteLine(messagesFromBuffer.Count);
                    messagesFromBuffer.Clear();
                }
            }
        }

        private void ParseMessagesForRolls(HtmlDocument htmlDoc) {
             HtmlNode messageNode = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, \"message\")]");

            if (messageNode == null) {
                Console.WriteLine("Parser was expecting messages but got none. Sorry!");
                return;
            }

            while (messageNode != null) {
                HashSet<string> classes = messageNode.GetClasses().ToHashSet();

                if (classes.Contains("desc")) {
                    messageNode = messageNode.NextSibling;
                    continue;
                }

                if (!classes.Contains("emote")) {
                    currentCharacter = GetAndLogCharacter(messageNode);
                }

                if (classes.Contains("rollresult")) {
                    ParseForRollBlock(messageNode);
                }

                // This is done for all messages since roll blocks can, rarely, contain inline rolls.
                ParseForInlineRolls(messageNode, classes);

                messageNode = messageNode.NextSibling;
            }
        }

        private void ParseForRollBlock(HtmlNode messageNode) {
            HtmlNode rollResultNode = messageNode.SelectSingleNode(".//div[contains(@class, \"diceroll\")]");

            if (rollResultNode == null) {
                return;
            }

            string dieType = rollResultNode.GetClasses().ToArray()[1].ToLowerInvariant();

            List<HtmlNode> rollNodes = messageNode.SelectNodes(".//div[contains(@class, 'didroll')]").ToList();
            rollNodes.ForEach(r => RegisterRoll(currentCharacter, int.Parse(r.InnerText), dieType, false));
        }

        private void ParseForInlineRolls(HtmlNode messageNode, IEnumerable<string> classes) {
            var rollNodes = messageNode.SelectNodes(".//span[contains(@class, \"inlinerollresult\")]");

            if (rollNodes == null) {
                return;
            }

            foreach (var rollNode in rollNodes) {
                string rollNodeAttr = rollNode.GetAttributeValue("title", null) ?? rollNode.GetAttributeValue("original-title", null);

                if (string.IsNullOrEmpty(rollNodeAttr)) {
                    continue;
                }

                string dieType = dieTypeQuery.Match(rollNodeAttr).Groups[1].Value;
                List<Match> rollValues = rollValueQuery.Matches(rollNodeAttr).ToList();

                if (classes.Contains("emote")) {
                    rollValues.ForEach(r => {
                        emoteIndices.Add(allRolls.Count); // Mark the next roll to be added as from an emote message
                        RegisterRoll(messageNode.InnerText, int.Parse(r.Groups[1].Value), dieType, true);
                    });
                } else {
                    rollValues.ForEach(r => RegisterRoll(currentCharacter, int.Parse(r.Groups[1].Value), dieType, false));
                }
            }
        }

        private string GetAndLogCharacter(HtmlNode messageNode) {
            HtmlNode authorNode = messageNode.SelectSingleNode(".//span[contains(@class, \"by\")]");
            string character = authorNode?.InnerText[0..^1] ?? currentCharacter;

            HtmlNode avatarNode = messageNode.SelectSingleNode(".//div[contains(@class, \"avatar\")]")?.FirstChild;
            string avatar = avatarNode?.GetAttributeValue("img", null);

            LinkAvatarToCharacter(avatar, character);
            return character;
        }

        private void RegisterRoll(string author, int rollValue, string dieType, bool isEmote) {
            allRolls.Add(new Roll(author, rollValue, dieType));
            allDieTypes.Add(dieType);

            if (!isEmote) {
                allCharacters.Add(author);
            }
        }

        private void LinkAvatarToCharacter(string avatar, string character) {
            avatar ??= "blankAvatar";

            if (AvatarToCharacter.ContainsKey(avatar)) {
                AvatarToCharacter[avatar].Add(character);
            } else {
                AvatarToCharacter[avatar] = new() { character };
            }
        }

        private int TryResolveEmoteEntries() {
            int unresolvedEntries = 0;

            foreach (int index in emoteIndices) {
                // Check the AvatarToCharacter dictionary for this avatar. Having no avatar counts as having a blank avatar.
                bool foundAvatar = AvatarToCharacter.TryGetValue(allRolls[index].RolledBy, out List<string> characterList);

                if (foundAvatar) {
                    // If there is only one author for this avatar, use that author. This is the simplest case.
                    if (characterList.Count == 1) {
                        Debug.WriteLine($"Changing {allRolls[index].RolledBy} to {characterList.First()}");
                        allRolls[index].RolledBy = characterList.First();
                        continue;
                    }

                    // If there is more than one author, it means multiple characters used the same avatar, or a single character
                    // used the avatar and changed their name. Search for the longest possible author match inside the emote message
                    // from the dictionary entry for this avatar.
                    string characterResult = SearchForLongestMatch(allRolls[index].RolledBy, characterList);
                    Debug.WriteLine($"Changing {allRolls[index].RolledBy} to {characterResult}");
                    allRolls[index].RolledBy = characterResult;
                } else {
                    // If there is no avatar, it means that this character has only typed emote messages, and it is programmatically
                    // impossible to determine what their name is. Default to the first word of the emote message.
                    // Return the number of these cases.
                    string newCharacter = authorQuery.Match(allRolls[index].RolledBy).Value;
                    allCharacters.Add(newCharacter);

                    Debug.WriteLine($"Changing {allRolls[index].RolledBy} to {newCharacter}");
                    allRolls[index].RolledBy = newCharacter;
                    unresolvedEntries++;
                }
            }

            return unresolvedEntries;
        }

        private string SearchForLongestMatch(string searchText, IEnumerable<string> searchTerms) {
            foreach (string searchTerm in searchTerms) {
                if (searchText[..searchTerm.Length] == searchTerm) {
                    return searchTerm;
                }
            }

            return null;
        }
    }
}