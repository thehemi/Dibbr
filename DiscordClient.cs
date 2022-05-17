using DSharpPlus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
//using DSharpPlus.CommandsNext;

namespace DibbrBot
{

    /// <summary>
    /// Discord chat system
    /// TODO: This will replace the messy code below, like this
    /// var discord = new DiscordChat(true,true,"channelid");
    /// discord.Initialize(GPTHandleMessage);
    /// 
    /// </summary>
    public class DiscordChat : IChatSystem
    {
        public bool IsBot;
        public bool dm;
        public string channel;
        static HttpClient client; // Shared
        public string ChatLog = "";
        private readonly int MAX_BUFFER = 5000; // Chat buffer, don't change this, change the one in GPT3

        public override string GetChatLog()
        {
            return ChatLog;
        }

        public override void SetChatLog(string log)
        {
            ChatLog = log;
        }

        public DiscordChat(bool IsBot, bool isdm, string channel)
        {
            this.dm = isdm;
            this.channel = channel;
            
        }
        
        public override async Task Initialize(MessageRecievedCallback callback, string token = null)
        {
            if (client == null)
            {
                client = new HttpClient();
                // set the headers
                client.DefaultRequestHeaders.Add("Accept", "*/*");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US");
                client.DefaultRequestHeaders.Add("Authorization", token);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) discord/0.0.309 Chrome/83.0.4103.122 Electron/9.3.5 Safari/537.36");

                // SSL Certificate Bypass
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                // 2 secs for headers to take
                // await Task.Delay(1000);
            }
            //  await Task.Delay(1000);
            var lastMsgTime = DateTime.MinValue;
            new Thread(async delegate ()
            {
                string lastMsg = "";
                while (true)
                {
                    bool Contains(List<string> lines, string str)
                    {
                        foreach (var l in lines) if (l.Contains(str))
                                return true;
                        return false;
                    }

                    await Task.Delay(3000 + (int)(new Random().NextDouble() * 500));
                    // Read message
                    // TODO: Use getLatestMessages()
                    // var message = dm ? await API.getLatestdm(client, channel) : await API.getLatestMessage(client, channel);
                    var msgList = new List<string>();
                    var messages = dm?await API.getLatestDMs(client,channel):await API.getLatestMessages(client, channel);
                    if (messages == null)
                        continue;
                    // Skip messages already replied to
                    foreach(var msg in messages)
                    {
                        var id = msg["referenced_message"]?["id"]?.ToString();
                        // We're looking for a message that has not been replied to by us
                        if (id == null || msg["author"]?["username"].ToString() != Program.BotUsername)
                            continue;
                        foreach(var msg2 in messages)
                        {
                            if (msg2["id"].ToString() == id)
                            {
                                msg2["skip"] = "skip";
                            }
                        }
                    }
                    var log = ChatLog.TakeLastLines(messages.Count);
                    for (int i = 0; i < messages.Count; i++)
                    {
                        var message = messages[i];
                        if (message["skip"] != null)
                            continue;
                        var msgid = message["id"].ToString();
                        var msg = message["content"].ToString();
                        var auth = message["author"]["username"].ToString();

                        var c = auth + ": " + msg;

                        // FIXME: Will block repeating messages
                        if (Contains(log, c))
                            continue;

                        c += "\n\r";
                        ChatLog += c;

                        // Make bot recognize the user as itself
                        // if (auth == Program.BotUsername && !msg.ToLower().StartsWith(Program.BotName))
                        //      auth = Program.BotName;
                        auth = auth.Replace("????", "Q"); // Crap usernames

                        // Skip lines already parsed
                        // Skip lines sent by the bot
                        if (c == lastMsg || (auth == Program.BotUsername && lastMsg.Contains(msg)))
                            continue;
                        lastMsg = c;

                        // Skip our own replies
                        if ((auth == Program.BotUsername) && message["referenced_message"] != null)
                            continue;

                        Console.WriteLine(c);
                        var isReply = false;
                        // Treat replies to bot as bot messages
                        var replyUser = message["referenced_message"] != null ? message["referenced_message"]["author"]["username"] : null;
                        if (replyUser?.ToString() == Program.BotUsername && !msg.StartsWith(Program.BotName))
                            isReply = true;
       

                        if (dm)
                        {
                           // if(lastMsgTime == DateTime.MinValue)
                            // First message, we don't respond to it, could be old
                              //   continue;
                            
                            if (DateTime.Now < lastMsgTime.AddSeconds(10))
                                continue;
                        }

                       
                        if (dm && !msg.StartsWith(Program.BotName)) msg = Program.BotName + " " + msg;
                        // If you wanna log context, too
                        // if(lastMsgTime == DateTime.MinValue || DateTime.Now-lastMsgTime < DateTime.FromSeconds(15))
                        //  File.AppendAllText("chat_log_" + channel + ".txt", c);
                        string reply = null;
                        try
                        {
                            reply = await callback(msg, auth);
                        }
                        catch(Exception e)
                        {
                            await Task.Delay(1000);
                            try { reply = await callback(msg, auth); } catch (Exception) { }
                            if(reply==null)
                                 Console.WriteLine(e.Message);
                        }
                        if (reply != null && reply.Length > 0)
                        {
                            lastMsgTime = DateTime.Now;
                            var c2 = Program.BotName + ": " + reply + "\n\r";
                            ChatLog += c2;
                            lastMsg = c2;

                            var response = dm ? await API.send_dm(client, channel, reply,msgid) : await API.send_message(client, channel, reply, msgid);

                            // Write out our response, along with the question
                            File.AppendAllText("chat_log_" + channel + ".txt", c + c2);
                        }
                    }
                }

                Console.WriteLine("How did I get here?");
            }).Start();

        }

    }

    /// <summary>
    /// API Client based discord access. More features than DiscordV1, notably you can use with bots
    /// Doesn't support selfbots
    /// </summary>
    class DiscordChatV2 : IChatSystem, IDisposable
    {
        public string id = "";
        public string ChatLog = "";
        public override string GetChatLog() { return ChatLog; }
        private readonly int MAX_BUFFER = 1000; // Chat buffer to GPT3 (memory) THIS CAN GET EXPENSIVE

        private DiscordClient _discordClient;
        readonly bool disposed = false;
        MessageRecievedCallback Callback;

        public override void SetChatLog(string log)
        {
            ChatLog = log;
        }

        public async override Task Initialize(MessageRecievedCallback callback, string token)
        {
            id = token;
            Callback = callback;
            try
            {
                var clientConfig = new DiscordConfiguration
                {
                    //  MinimumLogLevel = LogLevel.Critical,
                    Token = token,
                    TokenType = TokenType.Bot,
                    //  UseInternalLogHandler = true
                };

                /* INSTANTIATE DISCORDCLIENT */
                _discordClient = new DiscordClient(clientConfig);
                _discordClient.MessageCreated += MessageCreated;

                await _discordClient.ConnectAsync();
                Console.WriteLine("V 2.0 Discord Bot Client Online");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                System.Threading.Thread.Sleep(5000);

            }
            await Task.Delay(-1);
        }

        string lastMsg = "";

        /// <summary>
        /// Me
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private Task<Task> MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            new Thread(async delegate ()
            {
                var c = e.Author.Username + ": " + e.Message.Content + "\n";
                ChatLog += c;
                if (ChatLog.Length > MAX_BUFFER)
                    ChatLog = ChatLog[^MAX_BUFFER..];

                var first = lastMsg == "";
                if (c == lastMsg)
                {
                    return;
                }
                lastMsg = c;
                if (first) return;

                Console.WriteLine(c);
                File.AppendAllText("chat_log_" + e.Channel.Name + ".txt", c);


                var str = await Callback(e.Message.Content, e.Author.Username);
                if (str != null)
                    await sender.SendMessageAsync(e.Channel, str);
            }).Start();
            return Task.FromResult(Task.CompletedTask);
        }


        /* DISPOSE OF MANAGED RESOURCES */
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
            {
                Initialize(null, null).Dispose();
            }

        }
    }

    public static class StringHelp
    {
        public static List<string> TakeLastLines(this string text, int count)
        {
            var lines = text.Split("\n\r");

            return new List<string>(lines);
        }
    }
}
