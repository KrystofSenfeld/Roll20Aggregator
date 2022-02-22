using HtmlAgilityPack;
using Roll20Aggregator.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Roll20Aggregator.Services {
    public class Parser {
        private HashSet<string> allCharacters = new();
        private HashSet<string> allDieTypes = new();

        private Roll[] allRolls = Array.Empty<Roll>();
        private HashSet<int> emoteIndices = new();
        private Dictionary<string, string[]> AvatarToCharacter = new();

        private Regex authorQuery = new Regex(@"\S*");
        private Regex dieTypeQuery = new Regex("Rolling .*[0-9]*(d[0-9]+)", RegexOptions.IgnoreCase);
        private Regex rollValueQuery = new Regex(">([0-9]+)</");

        public async Task Parse(ChatLog chatLog) {
            GetAllRolls(chatLog.HtmlDoc);
            TryResolveEmoteEntries();

            chatLog.AllRolls = allRolls.ToList();

            chatLog.AllCharacters = allCharacters.ToList()
                .OrderBy(c => c)
                .ToList();

            chatLog.AllDieTypes = RollKeys.Keys.Keys.ToList()
                .Where(d => allDieTypes.Contains(d))
                .ToList();

            await Task.CompletedTask;
        }

        private void GetAllRolls(HtmlDocument htmlDoc) {
            HtmlNode contentNode = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, \"content\")]");
            HtmlNode messageNode = contentNode?.FirstChild;

            if (messageNode == null) {
                // Handle invalid html - no messages!
            }

            while (messageNode != null) {
                IEnumerable<string> classes = messageNode.GetClasses();

                if (classes.Contains("desc")) {
                    messageNode = messageNode.NextSibling;
                    continue;
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

            string character = GetCharacterAndAvatar(messageNode);
            string dieType = rollResultNode.GetClasses().ToArray()[1].ToLowerInvariant();

            List<HtmlNode> rollNodes = messageNode.SelectNodes(".//div[contains(@class, 'didroll')]").ToList();
            rollNodes.ForEach(r => RegisterRoll(character, int.Parse(r.InnerText), dieType, false));
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
                    string character = messageNode.InnerText;
                    emoteIndices.Add(allRolls.Length);
                    rollValues.ForEach(r => RegisterRoll(character, int.Parse(r.Groups[1].Value), dieType, true));
                } else {
                    string character = GetCharacterAndAvatar(messageNode);
                    rollValues.ForEach(r => RegisterRoll(character, int.Parse(r.Groups[1].Value), dieType, false));
                }
            }
        }

        private string GetCharacterAndAvatar(HtmlNode messageNode) {
            HtmlNode authorNode = null;
            while (authorNode == null) {
                authorNode = messageNode.SelectSingleNode(".//span[contains(@class, \"by\")]");
                if (authorNode != null) {
                    HtmlNode avatarNode = messageNode.SelectSingleNode(".//div[contains(@class, \"avatar\")]").FirstChild;
                    string avatar = avatarNode != null ? avatarNode.GetAttributeValue("img", string.Empty) : string.Empty;
                    string character = authorNode.InnerText[0..^1];
                    AddToAvatarToCharacter(avatar, character);

                    return character;
                }

                messageNode = messageNode.PreviousSibling;
            }

            return null;
        }

        private void RegisterRoll(string author, int rollValue, string dieType, bool isEmote) {
            allRolls.Append(new Roll(author, rollValue, dieType));
            allDieTypes.Add(dieType);

            if (!isEmote) {
                allCharacters.Add(author);
            }
        }

        private void AddToAvatarToCharacter(string avatar, string character) {
            avatar ??= string.Empty;

            if (AvatarToCharacter.ContainsKey(avatar)) {
                AvatarToCharacter[avatar].Append(character);
            } else {
                AvatarToCharacter[avatar] = new string[] { character };
            }
        }

        private int TryResolveEmoteEntries() {
            int unresolvedEntries = 0;

            foreach (int index in emoteIndices) {
                // Check the AvatarToCharacter dictionary for this avatar. Having no avatar counts as having a blank avatar.
                bool foundAvatar = AvatarToCharacter.TryGetValue(allRolls[index].RolledBy, out string[] characterList);

                if (foundAvatar) {
                    // If there is only one author for this avatar, use that author. This is the simplest case.
                    if (characterList.Length == 1) {
                        Debug.WriteLine($"Changing {allRolls[index].RolledBy} to {characterList[0]}");
                        allRolls[index].RolledBy = characterList[0];
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
