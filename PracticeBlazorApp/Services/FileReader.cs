using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Roll20Aggregator.Models;

namespace Roll20Aggregator.Services {
    public class FileReader {
        private ChatLog chatLog;

        public FileReader(ChatLog chatLog) {
            this.chatLog = chatLog;
        }

        public async Task<HtmlDocument> ReadDemoAsync(string demoFileText) {
            HtmlDocument htmlDoc = new();
            htmlDoc.LoadHtml(demoFileText);

            return htmlDoc;
        }

        public async Task<HtmlDocument> ReadAsync() {
            if (chatLog == null) {
                return null;
            }

            using StreamReader stream = new (chatLog.ChatLogFile.OpenReadStream(FileUploadModel.MaxFileSize));

            HtmlDocument htmlDoc = new();
            htmlDoc.LoadHtml(await stream.ReadToEndAsync());

            return htmlDoc;
        }

        public async Task BufferedReadAsync() {
            if (chatLog == null) {
                return;
            }

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
    }
}