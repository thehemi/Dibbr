using DSharpPlus;
using System;
using System.IO;
using System.Net.Http;
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
        private readonly int MAX_BUFFER = 600; // Chat buffer to GPT3 (memory) THIS CAN GET EXPENSIVE

        public override string GetChatLog()
        {
            return ChatLog;
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
            
            new Thread(async delegate ()
            {
                string lastMsg = "";
                while (true)
                {
                    // Read message
                    // TODO: Use getLatestMessages()
                    var message = dm ? await API.getLatestdm(client, channel) : await API.getLatestMessage(client, channel);
                    await Task.Delay(500 +(int) (new Random().NextDouble()*500));
                    if (message == null)
                    {
                        dm = false;
                        message = await API.getLatestMessage(client, channel);
                        if (message == null)
                            continue;
                    }
                    var msgid = message["id"].ToString();
                    var msg = message["content"].ToString();
                    var auth = message["author"]["username"].ToString();
                    // Make bot recognize the user as itself
                    // if (auth == Program.BotUsername && !msg.ToLower().StartsWith(Program.BotName))
                    //      auth = Program.BotName;
                    auth = auth.Replace("?????", "Q"); // Crap usernames
                    var c = auth + ": " + msg + "\n";

                    // Skip lines already parsed
                    // Funky check is because log might be BotName or BotUsername
                    if (c == lastMsg || (auth == Program.BotUsername && lastMsg.Contains(msg)))
                        continue;

                    bool first = lastMsg == "";
                    ChatLog += c;
                    lastMsg = c;

                    if (first && dm)
                    {
                        // First message, we don't respond to it, could be old
                        continue;
                    }

                    if (ChatLog.Length > MAX_BUFFER)
                        ChatLog = ChatLog.Substring(ChatLog.Length - MAX_BUFFER);

                    Console.WriteLine(c);
                    File.AppendAllText("chat_log_" + channel + ".txt", c);

                    var reply = await callback(msg, auth);

                    if (reply != null)
                    {
                        c = Program.BotName + ": " + reply + "\n";
                        ChatLog += c;
                        lastMsg = c;
                        var response = dm ? await API.send_dm(client, channel, reply) : await API.send_message(client, channel, reply, msgid);
                        int i = 0;
                        // while (++i<1 && (response == null || !response.IsSuccessStatusCode))
                        //    response = dm ? await API.send_dm(client, channel, reply) : await API.send_message(client, channel, reply, msgid);
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
        public string ChatLog = "";
        public override string GetChatLog() { return ChatLog; }
        private readonly int MAX_BUFFER = 1000; // Chat buffer to GPT3 (memory) THIS CAN GET EXPENSIVE

        private DiscordClient _discordClient;
        readonly bool disposed = false;
        MessageRecievedCallback Callback;


        public async override Task Initialize(MessageRecievedCallback callback, string token)
        {
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
                Environment.Exit(0);
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
                    ChatLog = ChatLog.Substring(ChatLog.Length - MAX_BUFFER);

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
                  Initialize(null,null).Dispose();
            }

        }
    }
}
