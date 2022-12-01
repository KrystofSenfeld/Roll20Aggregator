using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using HtmlAgilityPack;
using Roll20Aggregator.Models;
using Roll20Aggregator.Utility;

namespace Roll20Aggregator.Services {
    public class FileParser {
        private readonly IDocument htmlDocument;

        private readonly Regex authorQuery = new(@"(\w)+");
        private readonly Regex dieTypeQuery = new("Rolling .*[0-9]*(d[0-9]+)", RegexOptions.IgnoreCase);
        private readonly Regex rollValueQuery = new(">([0-9]+)</");

        private readonly List<RollDto> rolls = new();
        private readonly HashSet<int> emoteMessageIndices = new();

        private readonly HashSet<string> characterNames = new();
        private readonly Dictionary<string, HashSet<string>> characterNameByAvatar = new();
        private readonly HashSet<string> dieTypes = new();

        private int parsedMessagesCount = 0;

        private string currentCharacter = null;
        private string currentAvatar = null;

        public FileParser(IDocument htmlDocument) {
            this.htmlDocument = htmlDocument;
        }


        public ParseResultsDto Parse() {
            if (htmlDocument == null) {
                throw new ArgumentNullException();
            }

            ParseMessagesForRollsAngleSharp();
            TryResolveEmoteEntries();

            // Console.WriteLine(string.Join(", ", rolls.Where(r => r.DieType == "d100" && r.RolledBy == "Torian York").Select(r => r.Value.ToString())));

            return new ParseResultsDto {
                ParsedMessagesCount = parsedMessagesCount,

                AllRolls = rolls.ToList(),

                AllCharacters = characterNames
                    .OrderBy(name => name)
                    .ToList(),

                AllDieTypes = DiceUtility.ResultGroupsByDieType.Keys
                    .Where(dieType => dieTypes.Contains(dieType))
                    .Reverse()
                    .ToList(),

                PrimaryDieType = rolls
                    .GroupBy(roll => roll.DieType)
                    .OrderByDescending(group => group.Count())
                    .Select(group => group.Key)
                    .FirstOrDefault(),
            };
        }

        private void ParseMessagesForRollsAngleSharp() {
            var messageNodes = htmlDocument.QuerySelectorAll("div.content > div.message");

            if (!messageNodes.Any()) {
                Console.WriteLine("Parser was expecting messages but got none.");
                return;
            }

            parsedMessagesCount += messageNodes.Length;

            Console.WriteLine($"message count: {parsedMessagesCount}");

            foreach (var messageNode in messageNodes) {
                HashSet<string> classes = messageNode.ClassList.ToHashSet();

                // Skip unwanted message types.
                if (classes.Contains("desc") || classes.Contains("private") || classes.Contains("whisper")) {
                    continue;
                }

                GetAndLogCharacter(messageNode, classes);

                // Only messages with the rollresult class contain roll blocks.
                if (classes.Contains("rollresult")) {
                    ParseRollBlock(messageNode);
                }

                // Inline roll parsing must be done for all messages since roll blocks can, rarely, contain inline rolls.
                ParseInlineRolls(messageNode, classes);
            }
        }

        private void ParseMessagesForRolls() {
            /*HtmlNode messageNode = null; //  htmlDocument.DocumentNode.SelectSingleNode("//div[contains(@class, \"message\")]");

            if (messageNode == null) {
                Console.WriteLine("Parser was expecting messages but got none.");
                return;
            }

            while (messageNode != null) {
                try {
                    parsedMessagesCount++;

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
            }*/
        }

        private void ParseRollBlock(IElement messageNode) {
            IElement rollResultNode = messageNode.QuerySelector("div .diceroll");

            if (rollResultNode == null) {
                return;
            }

            string dieType = rollResultNode.ClassList[1].ToLowerInvariant();

            List<IElement> rollNodes = messageNode.QuerySelectorAll("div .didroll").ToList();
            rollNodes.ForEach(r => RegisterRoll(currentCharacter, int.Parse(r.TextContent), dieType, false));
        }

        private void ParseInlineRolls(IElement messageNode, IEnumerable<string> classes) {
            var rollNodes = messageNode.QuerySelectorAll("span .inlinerollresult");

            if (rollNodes == null) {
                return;
            }

            foreach (var rollNode in rollNodes) {
                string rollNodeAttr = rollNode.GetAttribute("title") ?? rollNode.GetAttribute("original-title");

                Console.WriteLine(rollNodeAttr);

                if (string.IsNullOrEmpty(rollNodeAttr)) {
                    continue;
                }

                string dieType = dieTypeQuery.Match(rollNodeAttr).Groups[1].Value;
                List<Match> rollValues = rollValueQuery.Matches(rollNodeAttr).ToList();

                if (classes.Contains("emote")) {
                    // Emote message author is temporarily set to the emote message itself, which will later be
                    // resolved based on confirmed characters and avatars.
                    rollValues.ForEach(r => RegisterRoll(messageNode.TextContent, int.Parse(r.Groups[1].Value), dieType, true));
                } else {
                    rollValues.ForEach(r => RegisterRoll(currentCharacter, int.Parse(r.Groups[1].Value), dieType, false));
                }
            }
        }

        private void GetAndLogCharacter(IElement messageNode, HashSet<string> classes) {
            // If this is an emote message, it has no written character name. The avatar must be used
            // to attempt to identify the character.
            if (classes.Contains("emote")) {
                GetAvatar(messageNode);
                return;
            }

            IElement authorNode = messageNode.QuerySelector("span .by");

            if (authorNode != null) {
                currentCharacter = authorNode.TextContent[0..^1];
                characterNames.Add(currentCharacter);
                GetAvatar(messageNode);
                LinkAvatarToCharacter(currentAvatar, currentCharacter);
            }
        }

        private void GetAvatar(IElement messageNode) {
            var avatarNode = messageNode.QuerySelector("div .avatar")?.FirstElementChild;
            currentAvatar = avatarNode?.GetAttribute("src") ?? "blankAvatar";
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