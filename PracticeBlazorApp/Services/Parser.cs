using HtmlAgilityPack;
using Roll20Aggregator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// TODO:
// - Remove Id from rolls and use allRolls.Count again for index?

namespace Roll20Aggregator.Services {
    public class Parser {
        private ChatLog chatLog;

        private HashSet<string> allCharacters = new();
        private HashSet<string> allDieTypes = new();

        private int rollIndex = 0;
        private List<Roll> allRolls = new();
        private HashSet<int> emoteIndices = new();
        private Dictionary<string, HashSet<string>> AvatarToCharacter = new();

        private string currentCharacter = null;
        private string currentAvatar = null;

        private readonly Regex authorQuery = new Regex(@"(\w)+");
        private readonly Regex dieTypeQuery = new Regex("Rolling .*[0-9]*(d[0-9]+)", RegexOptions.IgnoreCase);
        private readonly Regex rollValueQuery = new Regex(">([0-9]+)</");

        public async Task Parse(ChatLog chatLog) {
            this.chatLog = chatLog;
            await BufferedReadAndParse();
            TryResolveEmoteEntries();

            chatLog.AllRolls = allRolls.ToList();

            chatLog.AllCharacters = allCharacters.ToList()
                .OrderBy(c => c)
                .ToList();

            // Have the list of dice start with the largest first; eg d100
            chatLog.AllDieTypes = RollKeys.Keys.Keys.ToList()
                .Where(d => allDieTypes.Contains(d))
                .Reverse()
                .ToList();
        }

        private async Task BufferedReadAndParse() {
            using (StreamReader stream = new(chatLog.ChatLogFile.OpenReadStream(FileUploadModel.MaxFileSize))) {
                int bufferSize = 1024 * 10; // 10 KB
                char[] buffer = new char[bufferSize];

                StringBuilder bufferBuilder = new();
                List<string> messages = new();

                string messageTag = "<div class=\"message";
                string endTag = "<script";
                bool foundFirst = false;
                int foundIndex;

                while (await stream.ReadAsync(buffer, 0, bufferSize) > 0) {
                    bufferBuilder.Append(new string(buffer));
                    string adjustedBufferString = bufferBuilder.ToString();

                    // Search buffer for occurrences of message opening tags and add messages to list
                    while ((foundIndex = adjustedBufferString.IndexOf(messageTag)) >= 0) {
                        if (foundFirst) {
                            messages.Add(messageTag + adjustedBufferString[..foundIndex]);
                        } else {
                            foundFirst = true;
                        }
                        bufferBuilder.Remove(0, foundIndex + messageTag.Length);
                        adjustedBufferString = bufferBuilder.ToString();
                    }

                    // Check if we've reached the end of the document, and assume
                    // what's left is a message.
                    if (foundFirst && adjustedBufferString.Contains(endTag)) {
                        messages.Add(messageTag + adjustedBufferString + "></script>");
                        Console.WriteLine("Reached the end of file...");
                    }

                    foreach (string message in messages) {
                        HtmlDocument htmlDoc = new();
                        htmlDoc.LoadHtml(message);
                        ParseMessagesForRolls(htmlDoc);
                    }
                    messages.Clear();

                    // If there is no message, we don't want to keep building up the text
                    // and reading it all into memory. Stop parsing if no message is found
                    // after 50 KB of text.
                    if (bufferBuilder.Capacity > 1024 * 50) {
                        Console.WriteLine("Parser didn't find a valid message for 50 KB, so it stopped parsing.");
                        return;
                    }
                }
            }
        }

        private void ParseMessagesForRolls(HtmlDocument htmlDoc) {
             HtmlNode messageNode = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, \"message\")]");

            if (messageNode == null) {
                Console.WriteLine("Parser was expecting messages but got none.");
                return;
            }

            while (messageNode != null) {
                HashSet<string> classes = messageNode.GetClasses().ToHashSet();

                if (classes.Contains("desc") || classes.Contains("private") || classes.Contains("whisper")) {
                    messageNode = messageNode.NextSibling;
                    continue;
                }

                GetAndLogCharacter(messageNode, classes);

                if (classes.Contains("rollresult")) {
                    try {
                        ParseForRollBlock(messageNode);
                    } catch (Exception) {
                        Console.WriteLine($"There was a problem parsing for roll block in the message:\n{messageNode.InnerHtml}");
                    }
                }

                // Inline roll parsing is done for all messages since roll blocks can, rarely, contain inline rolls.
                try {
                    ParseForInlineRolls(messageNode, classes);
                } catch (Exception) {
                    Console.WriteLine($"There was a problem parsing for inline rolls in the message:\n{messageNode.InnerHtml}");
                }

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
                    // Emote message author is temporarily set to the emote message itself, which will later be
                    // resolved based on confirmed characters and avatars.
                    rollValues.ForEach(r => RegisterRoll(messageNode.InnerText, int.Parse(r.Groups[1].Value), dieType, true));
                } else {
                    rollValues.ForEach(r => RegisterRoll(currentCharacter, int.Parse(r.Groups[1].Value), dieType, false));
                }
            }
        }

        private void GetAndLogCharacter(HtmlNode messageNode, HashSet<string> classes) {
            if (classes.Contains("emote")) {
                GetAvatar(messageNode);
                return;
            }

            HtmlNode authorNode = messageNode.SelectSingleNode(".//span[contains(@class, \"by\")]");

            if (authorNode != null) {
                currentCharacter = authorNode.InnerText[0..^1];
                allCharacters.Add(currentCharacter);
                GetAvatar(messageNode);
                LinkAvatarToCharacter(currentAvatar, currentCharacter);
            }
        }

        private void GetAvatar(HtmlNode messageNode) {
            HtmlNode avatarNode = messageNode.SelectSingleNode(".//div[contains(@class, \"avatar\")]")?.FirstChild;
            currentAvatar = avatarNode?.GetAttributeValue("src", null) ?? "blankAvatar";
        }

        private void RegisterRoll(string author, int rollValue, string dieType, bool isEmote) {
            allRolls.Add(new Roll(rollIndex, author, currentAvatar, rollValue, dieType));
            allDieTypes.Add(dieType);
            
            if (isEmote) {
                emoteIndices.Add(rollIndex);
            }

            rollIndex++;
        }

        private void LinkAvatarToCharacter(string avatar, string character) {
            avatar ??= "blankAvatar";

            if (AvatarToCharacter.ContainsKey(avatar)) {
                AvatarToCharacter[avatar].Add(character);
            } else {
                AvatarToCharacter[avatar] = new() { character };
            }
        }

        private void TryResolveEmoteEntries() {
            Console.WriteLine($"Attempting to resolve {emoteIndices.Count} emote rolls...");
            int unresolvedEntries = 0;

            foreach (int index in emoteIndices) {
                Roll roll = allRolls.Where(r => r.Id == index).SingleOrDefault();

                // Check the AvatarToCharacter dictionary for this avatar. Having no avatar counts as having a blank avatar.
                bool foundAvatar = AvatarToCharacter.TryGetValue(roll?.Avatar, out HashSet<string> characterSet);

                if (foundAvatar) {
                    // If there is only one character for this avatar, use that character. This is the ideal case.
                    if (characterSet.Count == 1) {
#if DEBUG
                        Console.WriteLine($"Identified '{roll.RolledBy}' as '{characterSet.First()}' by unique avatar.");
#endif
                        roll.RolledBy = characterSet.First();
                        continue;
                    }

                    // If there is more than one character, it means multiple characters used the same avatar. Search the emote message
                    // for the longest possible character from those associated with this avatar. This includes characters that don't have avatars.
                    string characterResult = SearchForLongestMatch(roll.RolledBy, characterSet);

                    if (characterResult != null) {
#if DEBUG
                        Console.WriteLine($"Identified '{roll.RolledBy}' as '{characterResult}'.");
#endif
                        roll.RolledBy = characterResult;
                        continue;
                    }
                }

                // If an avatar wasn't found, it means that this character has only typed emote messages, and it is programmatically
                // impossible to determine what their name is. Default to the first word of the emote message.
                string newCharacter = authorQuery.Match(roll.RolledBy.Trim()).Value;
#if DEBUG
                Console.WriteLine($"Could not identify '{roll.RolledBy}'; using '{newCharacter}'");
#endif

                allCharacters.Add(newCharacter);
                roll.RolledBy = newCharacter;
                unresolvedEntries++;
            }

            Console.WriteLine($"{unresolvedEntries} emote rolls could not be linked to existing characters.");

            foreach(var kvp in AvatarToCharacter) {
                Console.WriteLine(string.Join(", ", kvp.Value));
            }

            // Console.WriteLine(string.Join(", ", allCharacters));
        }

        private string SearchForLongestMatch(string searchText, IEnumerable<string> searchTerms) {
            searchTerms = searchTerms.OrderByDescending(t => t.Length);
            foreach (string searchTerm in searchTerms) {
                if (searchText[..searchTerm.Length] == searchTerm) {
                    return searchTerm;
                }
            }

            return null;
        }
    }
}