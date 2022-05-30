using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;

//using DSharpPlus.CommandsNext;

namespace DibbrBot;

/// <summary>
///     Discord chat system
///     TODO: This will replace the messy code below, like this
///     var discord = new DiscordChat(true,true,"channelid");
///     discord.Initialize(GPTHandleMessage);
/// </summary>
public class DiscordChat : ChatSystem
{
    public HttpClient _client;

    public string BotUsername, BotName;

    //private string ChatLog = "";
    // private readonly int MAX_BUFFER = 5000; // Chat buffer, don't change this, change the one in GPT3
    public List<Channel> Channels = new();
    public bool IsBot;

    public DiscordChat(bool isBot, string BotName, string BotUsername)
    {
        this.BotUsername = this.BotName = BotName;
        if (BotUsername != null) BotUsername = BotUsername;
    }


    public override void Typing(bool start)
    {
        // API.Typing(client, channel, start);
    }

    public void AddChannel(string channel, bool dm, MessageHandler handler, bool sayHi = false)
    {
        var c = new Channel {Id = channel, Dm = dm};
        c.Handler = handler;

        handler.BotName = BotName;
        Channels.Add(c);
        if (c.Dm) handler.ChattyMode = false;

        // Bot is not allowed here
        if (c.Id == "937151566266384394" || c.Id == "572387680235683861")
        {
            //H  handler.muted = true;
            handler.ChattyMode = false;
        }
        else { handler.SayHelo = sayHi; }

        /// dibbr pro, better memory
        if (c.Id == "979230826346709032") handler.MessageHistory *= 3;
    }

    public override async Task Initialize(MessageRecievedCallback callback, string token = null)
    {
        if (_client == null)
        {
            _client = new();
            // set the headers
            _client.DefaultRequestHeaders.Add("Accept", "*/*");
            _client.DefaultRequestHeaders.Add("Accept-Language", "en-US");
            _client.DefaultRequestHeaders.Add("Authorization", token);
            _client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) discord/0.0.309 Chrome/83.0.4103.122 Electron/9.3.5 Safari/537.36");

            // SSL Certificate Bypass
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            // 2 secs for headers to take
            //  await Task.Delay(1000);
        }

        new Thread(async delegate()
        {
            while (true)
            {
                //
                // Parse new DMs - a hack because I dunno how to get the discord user id
                //
                var hack = ConfigurationManager.AppSettings["hack"];
                if (hack is "true")
                {
                    var dms = await Api.GetNewDMs(_client,
                        "884911258413989978"); // <--- Replace with discordapi.getuserid()
                    foreach (var dm in dms)
                    {
                        var id = dm["recipients"][0]["id"].ToString();
                        var found = Channels.Where(c => c.Id == id).Count();
                        if (found == 0 && id != "948792559017283659") // Don't add the discord bot
                            AddChannel(id, true, new(this, Channels[0].Handler.Gpt3));
                    }
                }

                try
                {
                    foreach (var channel in Channels)
                    {
                        await channel.Update(this);
                        await Task.Delay(100);
                    }
                }
                catch (System.InvalidOperationException e)
                {
                    // We added a channel, ignore
                    continue;
                }
            }
        }).Start();
    }

    public class Channel
    {
        public bool Dm;


        public bool First = true;
        public MessageHandler Handler;
        public string Id;
        public string Log = "";
        public string Name;

        public async Task Update(DiscordChat client)
        {
            var channel = Id;
            //  await Task.Delay(1000);

            bool Contains(List<string> messages, string str)
            {
                str = str.Remove(Program.NewLogLine).Trim();
                foreach (var l in messages)
                {
                    if (l.Remove(Program.NewLogLine).Contains(str)) return true;
                }

                return false;
            }

            // Read message
            // TODO: Use getLatestMessages()
            // var message = dm ? await API.getLatestdm(client, channel) : await API.getLatestMessage(client, channel);
            new List<string>();
            var messages = Dm ? await Api.GetLatestDMs(client._client, channel)
                : await Api.GetLatestMessages(client._client, channel, numMessages: 8);
            if (messages == null) return;

            // Skip messages already replied to
            for (var i = 0; i < messages.Count; i++)
            {
                var msg = messages[i];

                // Most recent DM is from bot, so all other messages are replied to
                if (Dm && msg.Author.Username == client.BotUsername)
                {
                    // Set flags to ignore all, but don't remove message, as we might want to add to log
                    for (var k = i; k < messages.Count; k++) messages[k].Flags = 69;

                    break;
                }

                var id = msg.ReferencedMessage?.Id;

                if (id != null && msg.ReferencedMessage.Author.ToString() != client.BotName)
                {
                    // Ignore, was for someone else
                    msg.Flags = 69;
                }

                // If this is not a bot reply message, ignore
                if (msg.Author.Username != client.BotUsername || id == null) continue;

                // Bot reply, so flag the replied message as replied to
                foreach (var msg2 in from msg2 in messages where msg2.Id == id select msg2)
                {
                    msg2.Flags = 69; // skip
                    msg.Flags = 69; // skip this too
                }
            }

            var log = Log.TakeLastLines(messages.Count * 2); // assume extra replies
            for (var i = messages.Count - 1; i >= 0; i--)
            {
                var message = messages[i];
                var msgid = message.Id;
                var msg = message.Content;
                var auth = message.Author.Username.Replace("????", "Q");
                string editMsg = null;

                if (msg.StartsWith("!!")) editMsg = msg.After("!! ");

                // So GPT-3 doesn't get confused, in case these differ
                if (auth == client.BotUsername) auth = client.BotName;

                var c = auth + ": " + msg + Program.NewLogLine;

                // FIXME: Will block repeating messages
                bool AddToLog(string msg)
                {
                    if (Contains(log, msg)) return true;
                    Log += msg;
                    return false;
                } // true if already added to log


                if (AddToLog(c) || message.Flags == 69) continue;

                // Skip older messages, or if bot restarts he may spam
                if (message.Timestamp.AddSeconds(1600) < DateTime.Now)
                {
                    Console.WriteLine("Skipped old message: " + msg);
                    continue;
                }

                var isForBot = false;

                // Is for bot if it's an edit message
                isForBot |= editMsg != null && auth == client.BotUsername;

                // dibbr demo channel
                isForBot |= channel == "972018566834565124";

                // Is for bot if it's a reply to the bot
                isForBot |= message.ReferencedMessage?.Author.Username == client.BotName;
                // or DM
                isForBot |= Dm;

                // If you wanna log context, too
                // if(lastMsgTime == DateTime.MinValue || DateTime.Now-lastMsgTime < DateTime.FromSeconds(15))
                //  File.AppendAllText("chat_log_" + channel + ".txt", c);

                if (isForBot) Api.Typing(client._client, channel, true);

                Handler.Log = Log;
                var (isReply1, reply) = await Handler.OnMessage(msg, auth, isForBot);


                if (reply is not {Length: not 0}) continue;

                var c2 = client.BotName + ": " + reply + Program.NewLogLine;
                Log += c2;

                _ = Dm ? await Api.send_dm(client._client, channel, reply, msgid)
                    : await Api.send_message(client._client, channel, reply, isReply1 ? msgid : null, editMsg);

                // Write out our response, along with the question
                File.AppendAllText("chat_log_" + channel + ".txt", c + c2);
            }

            await Handler.OnUpdate(async s =>
            {
                // FIXME: Need generic handler for all client types
                _ = Dm ? await Api.send_dm(client._client, channel, s, null)
                    : await Api.send_message(client._client, channel, s, null);
            });
        }
    }
}

/// <summary>
///     API Client based discord access. More features than DiscordV1, notably you can use with bots
///     Doesn't support selfbots
/// </summary>
class DiscordChatV2 : ChatSystem, IDisposable
{
    readonly bool _disposed = false;
    readonly int _maxBuffer = 1000; // Chat buffer to GPT3 (memory) THIS CAN GET EXPENSIVE

    DiscordClient _discordClient;
    MessageRecievedCallback _callback;
    public string ChatLog = "";
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
                Token = token, TokenType = TokenType.Bot
                //  UseInternalLogHandler = true
            };

            /* INSTANTIATE DISCORDCLIENT */
            _discordClient = new(clientConfig);
            _discordClient.MessageCreated += MessageCreated;

            await _discordClient.ConnectAsync();
            Console.WriteLine("V 2.0 Discord Bot Client Online");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Thread.Sleep(5000);
        }

        await Task.Delay(-1);
    }

    /// <summary>
    ///     Me
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    Task<Task> MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
    {
        new Thread(async delegate()
        {
            var c = e.Author.Username + ": " + e.Message.Content + "\n";
            ChatLog += c;
            if (ChatLog.Length > _maxBuffer) ChatLog = ChatLog[^_maxBuffer..];

            var first = _lastMsg == "";
            if (c == _lastMsg) return;

            _lastMsg = c;
            if (first) return;

            Console.WriteLine(c);
            File.AppendAllText("chat_log_" + e.Channel.Name + ".txt", c);


            var (_, str) = await _callback(e.Message.Content, e.Author.Username);
            if (str != null) await sender.SendMessageAsync(e.Channel, str);
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
        var lines = text.Split(Program.NewLogLine).ToArray();
        if (lines.Last().Trim() == "") lines = lines[..(lines.Length - 1)];

        if (lines.Length > count) return lines[^count..].ToList();

        return lines.ToList();
    }
}