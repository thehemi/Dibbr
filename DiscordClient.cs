using DSharpPlus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System.Configuration;
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
        static HttpClient client; // Shared
                                  //private string ChatLog = "";
                                  // private readonly int MAX_BUFFER = 5000; // Chat buffer, don't change this, change the one in GPT3
        public List<Channel> channels = new List<Channel>();



        public override void Typing(bool start)
        {
            // API.Typing(client, channel, start);
        }

        public void AddChannel(string channel, bool dm, MessageHandler handler)
        {

            var c = new Channel { id = channel, dm = dm };
            c.handler = handler;
            channels.Add(c);
            if (c.dm)
                handler.chattyMode = false;
            // Bot is not allowed here
            if (c.id == "937151566266384394")
            {
                //H  handler.muted = true;
                handler.chattyMode = false;
            }
            if (c.id == "572387680235683861")
                handler.chattyMode = false;
            /// dibbr pro, better memory
            if (c.id == "979230826346709032")
            {
                handler.MESSAGE_HISTORY *= 3;
            }
        }

        public override string GetChatLog(int messages = 10)
        {
            return "";//String.Join(Program.NewLogLine, ChatLog.TakeLastLines(messages));
        }

        public override void SetChatLog(string log)
        {
            //ChatLog = log;
        }

        public DiscordChat(bool IsBot)
        {


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
                await Task.Delay(1000);
            }

            new Thread(async delegate ()
            {
                while (true)
                {
                    //
                    // Parse new DMs - a hack because I dunno how to get the discord user id
                    //
                    var hack = ConfigurationManager.AppSettings["hack"];
                    if (hack != null && hack == "true")
                    {
                        var dms = await API.getNewDMs(client, "884911258413989978"); // <--- Replace with discordapi.getuserid()
                        foreach (var dm in dms)
                        {
                            var id = dm["recipients"][0]["id"].ToString();
                            var found = channels.Where(c => c.id == id).Count();
                            if (found == 0 && id != "948792559017283659") // Don't add the discord bot
                                AddChannel(id, true, new MessageHandler(this, channels[0].handler.gpt3));
                        }
                    }

                    foreach (var channel in channels)
                    {
                        await channel.Update();
                        await Task.Delay(300);
                    }
                }
            }).Start();
        }

        public class Channel
        {
            public string Log = "";
            public bool dm;
            public string id;
            public string name;
            public string lastMsg = "";

            public MessageHandler handler;

          
            
            public bool first = true;

            public async Task Update()
            {
                var channel = id;
                //  await Task.Delay(1000);

                bool Contains(List<string> messages, string str)
                {
                    str = str.Replace("\n", "").Replace("\r", "").Trim();
                    foreach (var l in messages)
                        if (l.Replace("\n", "").Replace("\r", "").Contains(str))
                            return true;
                    return false;
                }

                // Read message
                // TODO: Use getLatestMessages()
                // var message = dm ? await API.getLatestdm(client, channel) : await API.getLatestMessage(client, channel);
                var msgList = new List<string>();
                var messages = dm ? await API.getLatestDMs(client, channel) : await API.getLatestMessages(client, channel, numMessages: lastMsg == "" ? 10 : 5);
                if (messages == null)
                    return;
                // Skip messages already replied to
                var repliedTo = false;
                foreach (var msg in messages)
                {

                    if (dm && msg.author.username == Program.BotUsername)
                    {
                        repliedTo = true; // We've already replied to this message, and all other messages are older
                    }

                    if (repliedTo)
                    {
                        msg.flags = 69; // Set flags, but don't remove message, as we might want to add to log
                        continue;
                    }
                    var id = msg.referenced_message?.id;
                    // If this is a bot reply message, mark the referenced message as skipped
                    if (msg.author.username == Program.BotUsername && id != null)
                    {
                        /// Find referenced message
                        foreach (var msg2 in messages)
                        {
                            if (msg2.id == id)
                            {
                                msg2.flags = 69; // skip
                                msg.flags = 69; // skip this too
                            }
                        }
                    }
                }

                var log = Log.TakeLastLines(messages.Count * 2); // assume extra replies
                for (int i = messages.Count - 1; i >= 0; i--)
                {
                    var message = messages[i];
                    var msgid = message.id;
                    var msg = message.content;
                    var auth = message.author.username;

                    // So GPT-3 doesn't get confused, in case these differ
                    if (auth == Program.BotUsername)
                        auth = Program.BotName;

                    var c = auth + ": " + msg + Program.NewLogLine;

                    // FIXME: Will block repeating messages
                    if (Contains(log, c))
                        continue;

                    Log += c;

                    if (message.flags == 69 || c == lastMsg || (auth.ToLower() == Program.BotName && !msg.ToLower().StartsWith(Program.BotName)))
                        continue;

                    auth = auth.Replace("????", "Q"); // Crap usernames


                    // FIXME: This is a hack that'll probably break on other timezones. mostly for DM bugs
                    if (message.timestamp.AddSeconds(60).AddHours(2) < DateTime.Now)
                    {
                        Console.WriteLine("Skipped old message: " + msg);
                        continue;
                    }

                    lastMsg = c;

                    var isForBot = false;

                    // Is for bot if it's a reply to the bot
                    isForBot |= (message.referenced_message?.author.username == Program.BotName);

        
                    isForBot |= dm;

                    // If you wanna log context, too
                    // if(lastMsgTime == DateTime.MinValue || DateTime.Now-lastMsgTime < DateTime.FromSeconds(15))
                    //  File.AppendAllText("chat_log_" + channel + ".txt", c);
                    string reply = null;
                    bool isReply1 = true;
                    try
                    {
                        handler.Log = Log;

                        if (isForBot)
                            API.Typing(client, channel, true);

                        (isReply1, reply) = await handler.OnMessage(msg, auth, isForBot);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        await Task.Delay(1000);
                    }
                    if (reply == null || reply.Length == 0)
                        continue;

                    var c2 = Program.BotName + ": " + reply + Program.NewLogLine;
                    Log += c2;
                    lastMsg = c2;

                    var response = dm ? await API.send_dm(client, channel, reply, msgid) : await API.send_message(client, channel, reply, isReply1 ? msgid : null);

                    // Write out our response, along with the question
                    File.AppendAllText("chat_log_" + channel + ".txt", c + c2);

                }
                
                await handler.OnUpdate(async (s) => {  // FIXME: Need generic handler for all client types
                   var x = dm ? await API.send_dm(client, channel, s, null) : await API.send_message(client, channel, s, null);
                   
                });
            }
            //0 + (int)(new Random().NextDouble() * 500));
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
        public override string GetChatLog(int messages) { return ChatLog; }
        private readonly int MAX_BUFFER = 1000; // Chat buffer to GPT3 (memory) THIS CAN GET EXPENSIVE

        private DiscordClient _discordClient;
        readonly bool disposed = false;
        MessageRecievedCallback Callback;

        public override void SetChatLog(string log)
        {
            ChatLog = log;
        }

        public override void Typing(bool start)
        {
            //  API.Typing(client, channel, start);
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


                var (reply, str) = await Callback(e.Message.Content, e.Author.Username);
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

        public static string Remove(this string str, string s)
        {
            return str.Replace(s, "").Trim();
        }
        public static List<string> TakeLastLines(this string text, int count)
        {
            var lines = text.Split(Program.NewLogLine).ToArray();
            if (lines.Last().Trim() == "")
                lines = lines[..(lines.Length - 1)];

            if (lines.Length > count)
                return lines[^count..].ToList();
            return lines.ToList();
        }
    }
}
