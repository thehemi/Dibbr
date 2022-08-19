using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using ServiceStack;
using ServiceStack.Host;
using ServiceStack.Text;

//using DSharpPlus.CommandsNext;

namespace DibbrBot;


/// <summary>
///     API Client based discord access. More features than DiscordV1, notably you can use with bots
///     Doesn't support selfbots
/// </summary>
class DiscordChatV2 : ChatSystem, IDisposable
{
    readonly bool _disposed = false;
    readonly int _maxBuffer = 1000; // Chat buffer to GPT3 (memory) THIS CAN GET EXPENSIVE
    public MessageHandler handler;
    DiscordClient _discordClient;
    MessageRecievedCallback _callback;
    public Dictionary<string, string> ChatLog = new Dictionary<string, string>();
    public string Id = "";

    string _lastMsg = "";


    /* DISPOSE OF MANAGED RESOURCES */
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public override void Typing(bool start)
    {
        //  API.Typing(client, channel, start);
    }

    public override async Task Initialize(MessageRecievedCallback callback, string token)
    {
        bool bot = false;
        if (token.StartsWith("BOT"))
        {
            token = token.After("BOT ");
            bot = true;
        }

        Id = token;
        _callback = callback;
        try
        {
            var clientConfig = new DiscordConfiguration
            {
                //  MinimumLogLevel = LogLevel.Critical,
                Token = token,
                TokenType = TokenType.Bot
                //  UseInternalLogHandler = true
            };

            /* INSTANTIATE DISCORDCLIENT */
            _discordClient = new(clientConfig);
            _discordClient.MessageCreated += MessageCreated;

            await _discordClient.ConnectAsync();
            handler.BotName = _discordClient.CurrentUser.Username.ToLower();
            Console.WriteLine("V 2.0 Discord Bot Client Online");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Thread.Sleep(5000);
        }

        await Task.Delay(-1);
    }

    DateTime lastMsg = DateTime.MinValue;

    /// <summary>
    ///     Me
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    async Task<Task> MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
    {
        new Thread(async delegate ()
        {
            var content = Regex.Replace(e.Message.Content, "<.*?>|&.*?;", string.Empty);
            // Mentions should convert to text string that is recognized by handler
            if (e.MentionedRoles.Any(u => u.Name.ToLower() == handler.BotName.ToLower()) ||
                e.MentionedUsers.Any(u => u.Username.ToLower() == handler.BotName.ToLower()))
            {
                if (!content.StartsWith(handler.BotName)) content = handler.BotName + ", " + content + "\n";
            }

            var c = e.Author.Username + ": " + content;
            // HACK
            if (e.Channel == null) return; // Task.FromResult(Task.CompletedTask);
            var name = e.Channel.Name ?? e.Author.Username;


            // if (!name.Contains("dibbr")) return;

            var first = _lastMsg == "";
            if (c == _lastMsg) return; // Task.FromResult(Task.CompletedTask);

            if (!content.ToLower().Contains("misa") && !content.ToLower().Contains("miku") &&
                !content.ToLower().Contains(handler.BotName.ToLower()))
                return; // Task.FromResult(Task.CompletedTask);
            _lastMsg = c;
            // if (first) return;

            if (true) await Task.Delay(1);

            Console.WriteLine(name + " " + c);
            bool isReply = true;

            if (!ChatLog.ContainsKey(name)) ChatLog.Add(name, "");
            var oldLog = ChatLog[name];
            ChatLog[name] += $"Q (from {e.Author.Username}): {content}" + Program.NewLogLine;

            //      if (ChatLog[name].Length > _maxBuffer) ChatLog[name] = ChatLog[name][^_maxBuffer..];

            handler.Log = ChatLog[name];
            var (bReply, str) = await _callback(content, e.Author.Username, isReply);
            ChatLog[name] = handler.Log;

            // Reasons to not keep this Q/A pair
            if (str is null or "" || ChatLog[name].Contains(str))
            {
                ChatLog[name] = oldLog;
            } // Task.FromResult(Task.CompletedTask);
            else { ChatLog[name] += $"A (from {handler.BotName}): {str}{Program.NewLogLine}"; }

            if (str is null or "") return;

            var msgs = new List<string>();
            if (str.Length > 2000)
            {
                msgs.Add(str[..1999]);
                msgs.Add(str[1999..]);
            }
            else
                msgs.Add(str);

            await Program.Log("chat_log_" + name + ".txt", c + $"\n{handler.BotName}: " + str + "\n");
            foreach (var msg in msgs)
            {
                var m = msg;
                try
                {
                    DiscordEmbedBuilder embed = null;
                    if (msg.Contains("[Spoken"))
                    {
                        var m2 = msg.Substring(msg.IndexOf("http"));
                        var m3 = m2.Substring(0, m2.IndexOf(")")).Replace("\\", "");
                        //m3 = m3.Replace("")
                        embed = new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.SpringGreen,
                            Description = "Click here to listen instead",
                            Title = "Spoken Audio",
                            Url = m3
                        };
                        m = msg.Substring(msg.IndexOf("mp3") + 4);
                        // await sender.SendMessageAsync(e.Channel, m, embed);
                    }

                    // DiscordMessageBuilder d;d.

                    await sender.SendMessageAsync(e.Channel, m, embed);
                    lastMsg = DateTime.Now;
                }
                catch (Exception e) { Console.WriteLine(e.Message); }

                await Task.Delay(1);
            }
        }).Start();
        return Task.FromResult(Task.CompletedTask);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing) Initialize(null, null).Dispose();
    }
}

public static class StringHelp
{
    public static int Inc<T>(this IDictionary<T, int> input, T key)
    {
        if (input.TryGetValue(key, out var val))
        {
            input[key] = val + 1;
            return val + 1;
        }
        else
        {
            input.Add(key, 1);
            return 1;
        }

    }
    public static string Sub(this string input, int start, int end)
    {
        if (end > input.Length) end = input.Length;
        var result = input.Substring(start, end);
        return result;
    }
    public static string Rem(this string input, string search, string replacement = "")
    {
        var result = Regex.Replace(input, Regex.Escape(search), replacement.Replace("$", "$$"),
            RegexOptions.IgnoreCase);
        return result;
    }

    public static string Remove(this string str, string s) =>
        Regex.Replace(str, Regex.Escape(s), "", RegexOptions.IgnoreCase);

    public static List<string> TakeLastLines(this string text, int count)
    {
        var lines = Enumerable.ToArray(text.Split(Program.NewLogLine));
        if (lines.Last().Trim() == "") lines = lines[..^1];

        var newLines = new List<string>();
        for (var index = 0; index < lines.Length; index++)
        {
            var l = lines[index];
            if (l.Length < 4) continue;
            lines[index] = "@@" + l;
            newLines.Add(lines[index]);
        }

        if (newLines.Count > count) return newLines.ToArray()[^count..].ToList();

        return lines.ToList();
    }
}