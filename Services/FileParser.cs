using AngleSharp.Dom;
using Roll20AggregatorHosted.Models;
using Roll20AggregatorHosted.Models.Enums;
using Roll20AggregatorHosted.Utility;
using System.Text.RegularExpressions;

namespace Roll20AggregatorHosted.Services {
    public class FileParser {
        private readonly IDocument htmlDocument;
        private ParsingSession parsingSession;
        private ParseResultsDto parseResults;

        private const string BLANK_AVATAR = "blankAvatar";
        private readonly Regex dieTypeQuery = new("([0-9]*)(d[0-9]+)", RegexOptions.IgnoreCase);
        private readonly Regex rollValueQuery = new("(?:>|&gt;)([0-9]+)(?:<|&lt;)(?:/span)", RegexOptions.IgnoreCase);

        private IList<IElement> messageNodes;
        private List<RollDto> emoteMessages = new();
        private Dictionary<string, HashSet<string>> characterNameByAvatar = new();

        private string currentCharacter = null;
        private string currentAvatar = null;

        public FileParser(IDocument htmlDocument, ParsingSession parsingSession) {
            this.htmlDocument = htmlDocument;
            this.parsingSession = parsingSession;
            this.parseResults = parsingSession.ParseResults;
        }

        public async Task Parse() {
            if (htmlDocument == null) {
                throw new ArgumentNullException();
            }

            messageNodes = htmlDocument.QuerySelectorAll(".message")
                .Where(i => !i.ClassList.Contains("desc"))
                .Where(i => !i.ClassList.Contains("private"))
                .Where(i => !i.ClassList.Contains("whisper"))
                .ToList();

            parseResults.TotalMessageCount = messageNodes.Count;
            parseResults.ResultsChanged();

            await ParseMessages();

            parsingSession.Status = SessionStatusEnum.ResolvingEmoteMessages;
            ResolveEmoteMessages();

            parsingSession.Status = SessionStatusEnum.Finalizing;
            parseResults.FinishParsing();
        }

        private async Task ParseMessages() {
            foreach (var messageNode in messageNodes) {
                try {
                    parseResults.ParsedMessageCount++;
                    if (parseResults.ParsedMessageCount % Math.Max(1, parseResults.TotalMessageCount / 250) == 0) {
                        await Task.Delay(1);
                        parseResults.ResultsChanged();
                    }

                    bool isEmote = messageNode.ClassList.Contains("emote");
                    GetAndLogCharacter(messageNode, isEmote);

                    // Only messages with the rollresult class contain roll blocks.
                    if (messageNode.ClassList.Contains("rollresult")) {
                        ParseRollBlock(messageNode);
                    }

                    // Inline roll parsing must be done for all messages since roll blocks can, rarely, also contain inline rolls.
                    bool wasInlineRollFound = ParseInlineRolls(messageNode, isEmote);

                    if (isEmote && !wasInlineRollFound) {
                        RegisterEmoteMessage(messageNode.TextContent);
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"\nCould not parse the following message:\n{messageNode.InnerHtml}\n\n" + ex);
                }
            }

            parseResults.ResultsChanged();
        }

        private void ParseRollBlock(IElement messageNode) {
            var rollResultNodes = messageNode.QuerySelectorAll("div.diceroll");

            if (rollResultNodes == null) {
                return;
            }

            var rollDict = new Dictionary<string, List<int>>();

            foreach (var rollResultNode in rollResultNodes) {
                var nodeClasses = rollResultNode.ClassList;

                if (nodeClasses.Length < 2) {
                    continue;
                }

                string dieType = nodeClasses[1].ToLowerInvariant();
                var rollNode = rollResultNode.QuerySelector("div.didroll");

                if (!int.TryParse(rollNode.TextContent, out int result)) {
                    continue;
                }

                if (result <= 0 || result > DiceUtility.GetNumberOfFaces(dieType)) {
                    continue;
                }

                if (!rollDict.ContainsKey(dieType)) {
                    rollDict.Add(dieType, new List<int> { result });
                } else {
                    rollDict[dieType].Add(result);
                }
            }

            foreach (string key in rollDict.Keys) {
                RegisterRoll(currentCharacter, rollDict[key], key, false);
            }
        }

        private bool ParseInlineRolls(IElement messageNode, bool isEmote) {
            bool wasRollFound = false;

            var rollNodes = messageNode.QuerySelectorAll("span.inlinerollresult");

            if (rollNodes == null) {
                return false;
            }

            foreach (var rollNode in rollNodes) {
                string rollNodeAttr = rollNode.GetAttribute("title") ?? rollNode.GetAttribute("original-title");

                if (string.IsNullOrEmpty(rollNodeAttr)) {
                    continue;
                }

                var dieTypeMatches = dieTypeQuery.Matches(rollNodeAttr).ToList();
                var rollValueMatches = rollValueQuery.Matches(rollNodeAttr).ToList();

                // This can happen if square brackets are used to highlight a static number; the inlinerollresult span will
                // exist with a title attribute but there will be no valid die type.
                if (!dieTypeMatches.Any() || !rollValueMatches.Any()) {
                    continue;
                }

                int index = 0;

                foreach (var dieTypeMatch in dieTypeMatches) {
                    string dieQuantityString = dieTypeMatch.Groups[1].Value;

                    int dieQuantity = string.IsNullOrEmpty(dieQuantityString) ? 1 : int.Parse(dieQuantityString);
                    string dieType = dieTypeMatch.Groups[2].Value;

                    List<int> rollValues = new List<int>();
                    for (int i = 0; i < dieQuantity; i++) {
                        if (!int.TryParse(rollValueMatches[index++].Groups[1].Value, out int result)) {
                            continue;
                        }

                        if (result > 0 && result <= DiceUtility.GetNumberOfFaces(dieType)) {
                            rollValues.Add(result);
                        }
                    }

                    // At this stage there is no way to determine the message author.
                    // We must track emote messages to resolve them later.
                    RegisterRoll(isEmote ? messageNode.TextContent : currentCharacter, rollValues, dieType, isEmote);
                    wasRollFound = true;
                }
            }

            return wasRollFound;
        }

        private void GetAndLogCharacter(IElement messageNode, bool isEmote) {
            GetAvatar(messageNode);

            if (isEmote) {
                return;
            }

            var authorNode = messageNode.QuerySelector("span.by");

            if (authorNode != null) {
                currentCharacter = authorNode.TextContent[0..^1];
            }

            LogCharacterMessage(currentCharacter);
            LinkAvatarToCharacter(currentAvatar, currentCharacter);
        }

        private void LogCharacterMessage(string name) {
            if (parseResults.RawCharacterStats.TryGetValue(name, out CharacterStatsDto stats)) {
                stats.MessageCount++;
            } else {
                parseResults.RawCharacterStats.Add(name, new CharacterStatsDto {
                    MessageCount = 1,
                });
            }
        }

        private void GetAvatar(IElement messageNode) {
            var avatarNode = messageNode.QuerySelector("div.avatar > img");
            currentAvatar = avatarNode?.GetAttribute("src") ?? BLANK_AVATAR;
        }

        private void RegisterRoll(string author, List<int> rollValues, string dieType, bool isEmote) {
            if (!rollValues.Any()) {
                return;
            }

            var roll = new RollDto {
                MessageId = parseResults.ParsedMessageCount,
                RolledBy = author,
                Avatar = currentAvatar,
                Values = rollValues,
                RawDieType = dieType,
            };


            if (!isEmote) {
                AddRollToDictionary(roll);
            } else {
                emoteMessages.Add(roll);
            }
        }

        private void RegisterEmoteMessage(string author) {
            emoteMessages.Add(new RollDto {
                MessageId = parseResults.ParsedMessageCount,
                RolledBy = author,
                Avatar = currentAvatar,
                RawDieType = null,
            });
        }

        private void AddRollToDictionary(RollDto roll) {
            if (!parseResults.RawCharacterStats.ContainsKey(roll.RolledBy)) {
                parseResults.RawCharacterStats.Add(roll.RolledBy, new CharacterStatsDto());
            }

            if (!parseResults.RawCharacterStats[roll.RolledBy].RollsByDieType.ContainsKey(roll.RawDieType)) {
                parseResults.RawCharacterStats[roll.RolledBy].RollsByDieType.Add(roll.RawDieType, new List<RollDto>());
            }

            parseResults.RawCharacterStats[roll.RolledBy].RollsByDieType[roll.RawDieType].Add(roll);
        }

        private void LinkAvatarToCharacter(string avatar, string characterName) {
            avatar ??= BLANK_AVATAR;

            if (!characterNameByAvatar.ContainsKey(avatar)) {
                characterNameByAvatar.Add(avatar, new HashSet<string>());
            }

            characterNameByAvatar[avatar].Add(characterName);
        }

        private void ResolveEmoteMessages() {
            int unresolvedMessages = 0;
            long? lastConsumedMessageId = null;

            foreach (var message in emoteMessages) {
                // Having no avatar counts as having an avatar with the BLANK_AVATAR string value.
                bool foundAvatar = characterNameByAvatar.TryGetValue(message.Avatar, out HashSet<string> knownCharacterNames);

                if (foundAvatar) {
                    // If there is only one character for this avatar, use that character. This is the ideal case.
                    // If there is more than one character, it means multiple characters used the same avatar or
                    // one character switched avatars.
                    message.RolledBy = knownCharacterNames.Count == 1
                        ? knownCharacterNames.Single()
                        : SearchMessageForLongestNameMatch(message.RolledBy, knownCharacterNames);
                } else {
                    message.RolledBy = SearchMessageForLongestNameMatch(message.RolledBy);
                }

                if (string.IsNullOrEmpty(message.RolledBy)) {
                    unresolvedMessages++;
                    lastConsumedMessageId = message.MessageId;
                    continue;
                }

                if (message.RawDieType != null) {
                    AddRollToDictionary(message);
                }

                // Prevent multiple rolls in the same message from counting as mutliple messages.
                if (message.MessageId != lastConsumedMessageId) {
                    LogCharacterMessage(message.RolledBy);
                }

                lastConsumedMessageId = message.MessageId;
            }

            if (unresolvedMessages > 0) {
                Console.WriteLine($"{unresolvedMessages} emote messages could not be linked to existing characters.");
            }
        }

        private string SearchMessageForLongestNameMatch(string message, IEnumerable<string> names = null) {
            names ??= parseResults.RawCharacterStats.Keys;

            names = names
                .Where(name => name.Length <= message.Length)
                .OrderByDescending(name => name.Length);

            foreach (string name in names) {
                if (message[..name.Length] == name) {
                    return name;
                }
            }

            return null;
        }
    }
}