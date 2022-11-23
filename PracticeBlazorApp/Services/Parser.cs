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

        private readonly Regex authorQuery = new(@"(\w)+");
        private readonly Regex dieTypeQuery = new("Rolling .*[0-9]*(d[0-9]+)", RegexOptions.IgnoreCase);
        private readonly Regex rollValueQuery = new(">([0-9]+)</");

        private readonly List<RollDto> rolls = new();
        private readonly HashSet<int> emoteMessageIndices = new();

        private readonly HashSet<string> characterNames = new();
        private readonly Dictionary<string, HashSet<string>> characterNameByAvatar = new();
        private readonly HashSet<string> dieTypes = new();

        // private int rollIndex = 0;
        private string currentCharacter = null;
        private string currentAvatar = null;


        public async Task Parse(ChatLog chatLog) {
            this.chatLog = chatLog;

            await ReadAndParse();

            // await BufferedReadAndParse();

            TryResolveEmoteEntries();

            Console.WriteLine(string.Join(", ", rolls.Where(r => r.DieType == "d100" && r.RolledBy == "Torian York").Select(r => r.Value.ToString())));

            chatLog.AllRolls = rolls.ToList();

            chatLog.AllCharacters = characterNames.ToList()
                .OrderBy(name => name)
                .ToList();

            chatLog.AllDieTypes = RollKeys.Keys.Keys.ToList()
                .Where(dieType => dieTypes.Contains(dieType))
                .Reverse()
                .ToList();
        }

        private async Task ReadAndParse() {
            using StreamReader stream = new (chatLog.ChatLogFile.OpenReadStream(FileUploadModel.MaxFileSize));

            HtmlDocument htmlDoc = new();
            htmlDoc.LoadHtml(await stream.ReadToEndAsync());

            ParseMessagesForRolls(htmlDoc);
        }

        private async Task BufferedReadAndParse() {
            using StreamReader stream = new(chatLog.ChatLogFile.OpenReadStream(FileUploadModel.MaxFileSize));

            // Chat logs can be massive, so we don't want to read everything into memory at once.
            int bufferSize = 1024 * 100; // 100 KB
            char[] bufferArray = new char[bufferSize];

            StringBuilder bufferText = new();
            List<string> messages = new();

            string messageTag = "<div class=\"message";
            string endTag = "<script";

            bool foundMessageTag = false;
            int messageTagIndex;

            while (await stream.ReadAsync(bufferArray, 0, bufferSize) > 0) {
                bufferText.Append(new string(bufferArray));
                string bufferTextString = bufferText.ToString();

                // Search buffer for occurrences of message opening tags and add messages to list.
                while ((messageTagIndex = bufferTextString.IndexOf(messageTag)) >= 0) {

                    // The very first time we find a message tag, we don't know where the message ends.
                    // Thus it's only once we get the second message tag that we can start adding text
                    // to the message list.
                    if (foundMessageTag) {
                        messages.Add(messageTag + bufferTextString[..messageTagIndex]);
                    } else {
                        foundMessageTag = true;
                    }

                    // Remove found message from our buffer so that we can search for the next one.
                    bufferText.Remove(0, messageTagIndex + messageTag.Length);
                    bufferTextString = bufferText.ToString();
                }

                // Check if we've reached the end of the document. If we have, the text remaining
                // before the ending tag belongs to the last message.
                if (bufferTextString.Contains(endTag) && foundMessageTag) {
                    messages.Add(messageTag + bufferTextString + "></script>");
                    Console.WriteLine("Reached the end of file...");
                }

                // Create an HTML document out of each message and parse it.
                foreach (string message in messages) {
                    HtmlDocument htmlDoc = new();
                    htmlDoc.LoadHtml(message);
                    ParseMessagesForRolls(htmlDoc);
                }

                messages.Clear();

                // If we keep not finding a message, we don't want to keep building up the buffer.
                // Stop parsing if no message is found after 100 KB of text.
                if (bufferText.Capacity > 1024 * 100) {
                    Console.WriteLine("Parser didn't find a valid message for 50 KB, so it stopped parsing.");
                    return;
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
                try {
                    HashSet<string> classes = messageNode.GetClasses().ToHashSet();

                    // Skip unwanted message types.
                    if (classes.Contains("desc") || classes.Contains("private") || classes.Contains("whisper")) {
                        messageNode = messageNode.NextSibling;
                        continue;
                    }

                    GetAndLogCharacter(messageNode, classes);

                    // Only messages with the rollresult class contain roll blocks.
                    if (classes.Contains("rollresult")) {
                        ParseRollBlock(messageNode);
                    }

                    // Inline roll parsing must be done for all messages since roll blocks can, rarely, contain inline rolls.
                    ParseInlineRolls(messageNode, classes);

                    messageNode = messageNode.NextSibling;
                } catch (Exception ex) {
                    Console.WriteLine($"There was a problem parsing the following message:\n{messageNode.InnerHtml}\n\n" + ex);
                }
            }
        }

        private void ParseRollBlock(HtmlNode messageNode) {
            HtmlNode rollResultNode = messageNode.SelectSingleNode(".//div[contains(@class, \"diceroll\")]");

            if (rollResultNode == null) {
                return;
            }

            string dieType = rollResultNode.GetClasses().ToArray()[1].ToLowerInvariant();

            List<HtmlNode> rollNodes = messageNode.SelectNodes(".//div[contains(@class, 'didroll')]").ToList();
            rollNodes.ForEach(r => RegisterRoll(currentCharacter, int.Parse(r.InnerText), dieType, false));
        }

        private void ParseInlineRolls(HtmlNode messageNode, IEnumerable<string> classes) {
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
            // If this is an emote message, it has no written character name. The avatar must be used
            // to attempt to identify the character.
            if (classes.Contains("emote")) {
                GetAvatar(messageNode);
                return;
            }

            HtmlNode authorNode = messageNode.SelectSingleNode(".//span[contains(@class, \"by\")]");

            if (authorNode != null) {
                currentCharacter = authorNode.InnerText[0..^1];
                characterNames.Add(currentCharacter);
                GetAvatar(messageNode);
                LinkAvatarToCharacter(currentAvatar, currentCharacter);
            }
        }

        private void GetAvatar(HtmlNode messageNode) {
            HtmlNode avatarNode = messageNode.SelectSingleNode(".//div[contains(@class, \"avatar\")]")?.FirstChild;
            currentAvatar = avatarNode?.GetAttributeValue("src", null) ?? "blankAvatar";
        }

        private void RegisterRoll(string author, int rollValue, string dieType, bool isEmote) {
            rolls.Add(new RollDto {
                RolledBy = author, 
                Avatar = currentAvatar,
                Value = rollValue,
                DieType = dieType,
            });

            dieTypes.Add(dieType);
            
            if (isEmote) {
                emoteMessageIndices.Add(rolls.Count - 1);
            }
        }

        private void LinkAvatarToCharacter(string avatar, string character) {
            avatar ??= "blankAvatar";

            if (characterNameByAvatar.ContainsKey(avatar)) {
                characterNameByAvatar[avatar].Add(character);
            } else {
                characterNameByAvatar[avatar] = new() { character };
            }
        }

        private void TryResolveEmoteEntries() {
            Console.WriteLine($"Attempting to resolve {emoteMessageIndices.Count} emote rolls...");
            int unresolvedEntries = 0;

            foreach (int index in emoteMessageIndices) {
                RollDto roll = rolls[index];

                // Check the AvatarToCharacter dictionary for this avatar. Having no avatar counts as having a blank avatar.
                bool foundAvatar = characterNameByAvatar.TryGetValue(roll.Avatar, out HashSet<string> characterSet);

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
                    // for the longest possible character name from those associated with this avatar. This includes characters that don't have avatars.
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

                characterNames.Add(newCharacter);
                roll.RolledBy = newCharacter;
                unresolvedEntries++;
            }

            Console.WriteLine($"{unresolvedEntries} emote rolls could not be linked to existing characters.");

            foreach(var kvp in characterNameByAvatar) {
                Console.WriteLine(string.Join(", ", kvp.Value));
            }
        }

        private static string SearchForLongestMatch(string searchText, IEnumerable<string> searchTerms) {
            searchTerms = searchTerms
                .Where(t => t.Length <= searchText.Length)
                .OrderByDescending(t => t.Length);

            foreach (string searchTerm in searchTerms) {
                if (searchText[..searchTerm.Length] == searchTerm) {
                    return searchTerm;
                }
            }

            return null;
        }
    }
}