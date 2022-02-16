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

        private Dictionary<string, HashSet<string>> AvatarToCharacter = new();
        private List<Roll> EmoteRolls = new();

        private Regex dieTypeQuery = new Regex("Rolling .*[0-9]*(d[0-9]+)", RegexOptions.IgnoreCase);
        private Regex dieTypeShortQuery = new Regex("[0-9]*(d[0-9]+)\"", RegexOptions.IgnoreCase);
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
            HtmlNode contentNode = HtmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, \"content\")]");
            HtmlNode messageNode = contentNode?.FirstChild;

            if (messageNode == null) {
                // Handle invalid html - no messages!
            }

            while (messageNode != null) {
                IEnumerable<string> classes = messageNode.GetClasses();
                
                // Skip desc messages?

                if (classes.Contains("rollresult")) {
                    ParseForRollBlock(messageNode);
                }

                // This is done for all messages since roll blocks can, rarely, contain inline rolls.
                ParseForInlineRolls(messageNode, classes);

                messageNode = messageNode.NextSibling;
            }

            TryResolveEmoteEntries();
        }

        private void ParseForRollBlock(HtmlNode messageNode) {
            HtmlNode rollResultNode = messageNode.SelectSingleNode(".//div[contains(@class, \"diceroll\")]");

            if (rollResultNode == null) {
                return;
            }

            string character = GetCharacterAndAvatar(messageNode);
            string dieType = rollResultNode.GetClasses().ToArray()[1].ToLowerInvariant();

            List<HtmlNode> rollNodes = messageNode.SelectNodes(".//div[contains(@class, 'didroll')]").ToList();
            rollNodes.ForEach(r => RegisterRoll(character, int.Parse(r.InnerText), dieType));
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
                    rollValues.ForEach(r => EmoteRolls.Add(new Roll(character, int.Parse(r.Groups[1].Value), dieType)));   
                } else {
                    string character = GetCharacterAndAvatar(messageNode);
                    rollValues.ForEach(r => RegisterRoll(character, int.Parse(r.Groups[1].Value), dieType));
                }
            }
        }

        private string GetCharacterAndAvatar(HtmlNode messageNode) {
            Regex authorQuery = new Regex(@"\S*");

            HtmlNode authorNode = null;
            while (authorNode == null) {
                authorNode = messageNode.SelectSingleNode(".//span[contains(@class, \"by\")]");
                if (authorNode != null) {
                    HtmlNode avatarNode = messageNode.SelectSingleNode(".//div[contains(@class, \"avatar\")]");
                    string avatar = avatarNode.InnerHtml;
                    string character = authorNode.InnerText[0..^1];
                    AvatarToCharacter.Add(avatar, character);

                    return character;
                }

                messageNode = messageNode.PreviousSibling;
            }

            return null;
        }

        private void RegisterRoll(string author, int rollValue, string dieType) {
            AllRolls.Add(new Roll(author, rollValue, dieType));
            allCharacters.Add(author);
            allDieTypes.Add(dieType);
        }

        private int TryResolveEmoteEntries() {
            int unresolvedEntires = 0;

            foreach (var emoteRoll in EmoteRolls) {
                // Check the AvatarToCharacter dictionary for this avatar.
                AvatarToCharacter.TryGetValue(emoteRoll.RolledBy, out HashSet<string> characterList);

                // If there is only one author for this avatar, use that author. This is the simplest case.
                if (characterList.Count == 1) {

                    continue;
                }

                // If there is more than one author, it means multiple characters used the same avatar, or a single character
                // used the avatar and changed their name. Search for the longest possible author match inside the emote message
                // from the dictionary entry for this avatar.
                if (characterList.Count > 1) {

                    continue;
                }

                // If there is no avatar, search for the longest possible author match inside the emote message from the
                // list of confirmed authors.
                string characterResult = SearhEmoteMessageForLongestMatch();
                if (!string.IsNullOrEmpty(characterResult)) {

                    continue;
                }

                // If there is still no match, default to the first word of the emote message. Return the number of these cases.

                unresolvedEntires++;
            }

            return unresolvedEntires;
        }

        private string SearhEmoteMessageForLongestMatch() {


            return null;
        }
    }
}