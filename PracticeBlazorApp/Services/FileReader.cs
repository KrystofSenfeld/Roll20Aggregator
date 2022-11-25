using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Forms;

namespace Roll20Aggregator.Services {
    public static class FileReader {
        public static readonly long MaxFileSize = 1024 * 1024 * 10; // 10 MB

        public static async Task<HtmlDocument> ReadAsync(string chatLog) {
            HtmlDocument htmlDoc = new();
            htmlDoc.LoadHtml(chatLog);

            await Task.CompletedTask;
            return htmlDoc;
        }

        // This is simple, but not ideal. We're loading everything into memory at once, which
        // could be a problem for large files.
        public static async Task<HtmlDocument> ReadAsync(IBrowserFile chatLog) {
            if (chatLog == null) {
                return null;
            }

            using StreamReader stream = new(chatLog.OpenReadStream(MaxFileSize));
            string chatLogText = await stream.ReadToEndAsync();

            HtmlDocument htmlDoc = new();
            htmlDoc.LoadHtml(chatLogText);

            return htmlDoc;
        }

        // TODO: Buffering file read would be preferred, but it presents some complications. We
        // need to ensure that each segment we buffer is valid HTML for the parser.
        public static async Task BufferedReadAsync(IBrowserFile chatLog) {
            if (chatLog == null) {
                return;
            }

            using StreamReader stream = new(chatLog.OpenReadStream(MaxFileSize));

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