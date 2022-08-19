using DibbrBot;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using static System.DateTime;
using Newtonsoft.Json;
using System.Collections;
using Newtonsoft.Json.Linq;

public class DiscordV3
{
    public List<string> Channels;
    public string NotifyKeyword = "dabbr";
    public DiscordSocketClient _client;
    Dictionary<ulong, SocketMessage> Messages = new Dictionary<ulong, SocketMessage>();


    public class Room
    {
        public string Log = "";
        int Spoke;
        public DateTime LastNonBotMessage, LastBotMessage, LastBotQuery = DateTime.MinValue;
        public MessageHandler handler;
    }

    Dictionary<string, Room> Rooms = new Dictionary<string, Room>();

    public async Task Invite(string url)
    {
        var invite = "tmAdxdZj";
        if (url != null)
        {
            if (invite.IndexOf("/") != -1)
            {
                invite = url.Substring(url.LastIndexOf('/') + 1);
            }

            if (invite.Contains(" "))
                invite = invite.Substring(0, invite.IndexOf(" "));
        }
        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri("https://discord.com/api/v8/invites/" + invite);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("Authorization", Token);

        var postData = "";
        //    HttpResponseMessage response =
        //         await client.PostAsync(url, new StringContent($"{{\"new_member\": true}}", Encoding.UTF8, "application/json"));

        //    if (response.IsSuccessStatusCode) { 
        //       var res = await response.Content.ReadAsStringAsync();
        //   
        //  }
    }

    public string Token = "";
    public async Task Init(string token)
    {
        Token = token;
        var config = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            MessageCacheSize = 1000,
        };
        _client = new DiscordSocketClient(config);
        _client.Log += DoLog;
        _client.MessageReceived += OnMessage;
        _client.MessageDeleted += OnDelete;
        _client.MessageUpdated += OnEdit;

        // Some alternative options would be to keep your token in an Environment Variable or json config
        // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
        if (token == null)
            token = File.ReadAllText("token.txt");
        await _client.LoginAsync(TokenType.User, token);
        await _client.StartAsync();

        var dms = await _client.GetDMChannelsAsync(); ;
        var group = await _client.GetGroupChannelsAsync();
        var conns = await _client.GetConnectionsAsync();

        //  var ai = await _client.GetApplicationInfoAsync();
        // File.WriteAllText("dms.txt", dms.Concat((s)=>s.);
        File.WriteAllText("dm_channels.txt", group.ToString());
        File.WriteAllText("conns.txt", conns.ToString());
        //   File.WriteAllText("ai.txt", ai.ToString());

        if (File.Exists("BlackBoard.txt") && BlackBoard == null)
            BlackBoard = JsonConvert.DeserializeObject<Dictionary<string, string>>
                (File.ReadAllText("Blackboard.txt"));
    }

    // Store anything!!
    static Dictionary<string, string> BlackBoard = null;


    static Dictionary<string, int> Content = new Dictionary<string, int>();
    //-----------------------------------------------------------------------------------
    /// Below is just functions
    //-----------------------------------------------------------------------------------

    public async Task OnEdit(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
    {
        if (arg2 != null && arg2.Content != null)
            await OnMessage(arg2, true);

        SocketMessage arg;
        if (Messages.TryGetValue(arg1.Id, out arg))
            Log($"logs/{arg.Id}_{arg2.Channel.Name}_Edits.log", $"{DateTime.Now}#{arg2.Author.Username}:{arg.Content} :--> {arg2.Content}\n");
        else
            Log($"logs/{arg2.Id}_{arg2.Channel.Name}_Edits.log", $"{DateTime.Now}#{arg1.Id}\n");
    }


    public async Task OnDelete(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg2)
    {
        SocketMessage arg;
        if (Messages.TryGetValue(arg1.Id, out arg))
            Log($"logs/{arg.Id}_{arg.Channel.Name}_Deletes.log", $"{DateTime.Now}#{arg.Author.Username}:{arg.Content}\n");
        else
            Log($"logs/{arg2.Id}_{arg2.Name}_Deletes.log", $"{DateTime.Now}#{arg1.Id}\n");


    }
    Dictionary<string, string> MessageAuthors = new Dictionary<string, string>();
    Dictionary<ulong, string> Message = new Dictionary<ulong, string>();
    int d = 0;
    //Dictionary<ulong, int> Spoke = new Dictionary<ulong, int>();
    Dictionary<ulong, Discord.Rest.RestUserMessage> LastMessage = new Dictionary<ulong, Discord.Rest.RestUserMessage>();

    public async Task OnMessage(SocketMessage arg) => OnMessage(arg, false);

    public async Task OnMessage(SocketMessage arg, bool edit = false)
    {
        var botName = arg.Discord.CurrentUser.Username;
        var isForBot = ForBot();
        if (edit && !isForBot)
            return;


        #region Spam message filter
        var hash = arg.Content.Sub(0, 50) + arg.Channel.Name + arg.Author.Username;
        var dup = Content.Inc(hash);
        if (dup > 3)
        {
            var val = arg.Content;
            return;
        }
        #endregion


        #region KeyValues
        // Check for writing to key value store
        var regex = new Regex(botName + " (?<key>[^:]+) is(?<value>[^,]+)");
        var x = regex.Match(arg.Content);

        if (!arg.Content.HasAny("what", "where", "when", "who", $"{botName}:") && x.Success/* && !x.Groups["key"].Value.Trim().Contains(" ")*/)
        {
            var key = x.Groups["key"].Value;
            var val = x.Groups["value"].Value.Trim();
            if (val == "what") val = "?";
            var txt = "";
            // Old val
            if (BlackBoard.TryGetValue(key, out var v2))
            {
                if (val == "?" || val.Trim().Length == 0)
                    txt += $"{key} is {v2}";
                else if (v2 != val)
                    txt += $"{key} was {v2}. ";
            }

            //  Setting a new val
            if (val.Length > 0 && val != "?")
            {

                BlackBoard[key] = val;
                txt += $"{key} is {val}";
            }

            if (txt.Length > 0)
            {
                File.WriteAllText("BlackBoard.Txt", JsonConvert.SerializeObject(BlackBoard));
                arg.Channel.SendMessageAsync(txt);
                return;
            }

        }
        #endregion

        #region APIKeys
        if (arg.Content.Contains("sk-"))
        {
            var key = arg.Content.Substring(arg.Content.IndexOf("sk-"));
            var idx = key.IndexOf(" ");
            if (idx == -1) idx = key.Length;
            key = key.Substring(0, idx);
            Program.Set("OpenAI_OLD", Gpt3._token);
            Program.Set("OpenAI", key);

            await arg.Channel.SendMessageAsync("Thank you for the key, " + arg.Author.Username);

        }
        #endregion

        #region Invites
        if (arg.Content.Contains("https://discord.gg/") || arg.Content.Contains("https://discord.com/"))
        {
            //   Console.Beep(5000, 4000);
            var key = arg.Content.Substring(arg.Content.LastIndexOf("/") + 1, 7);
            Invite(key);
            Program.Set(key, "invite");//arg.Discord.AddGuild(E)
        }
        #endregion // invites

        new Thread(async delegate ()
        {
            if (!(arg is SocketUserMessage message)) return;// Task.CompletedTask;
            if (message.Source != MessageSource.User) return;// Task.CompletedTask;


            var room = Rooms.GetValueOrDefault(arg.Channel.Name, new Room()
            {
                handler = new MessageHandler(null, null) { Channel = arg.Channel.Id.ToString(), BotName = botName }
            });



            string content = message.Content;
            bool isBot = arg.Author.Username == arg.Discord.CurrentUser.Username;

            if (arg.Discord.CurrentUser.Username == botName)
                isForBot |= message.Channel.Name.StartsWith("@");


            #region RoomData
            var newIsForBot = false;
            // If a message was sent very close to the bot replying, assume it's for the bot
            if (!isForBot && message.Reference == null && !isBot)
            {
                var timeSinceBot = (Now - room.LastBotMessage).TotalSeconds;
                var timeSinceUser = (Now - room.LastNonBotMessage).TotalSeconds;
                if (timeSinceBot > 3)
                    if (timeSinceBot < 8 && timeSinceBot < timeSinceUser * 1.5f)
                        newIsForBot = true;
                //     newIsForBot = true;

            }
            // Now updates
            if (isBot)
                room.LastBotMessage = DateTime.Now;
            else if (!isForBot)
                room.LastNonBotMessage = DateTime.Now;
            else if (isForBot)
                room.LastBotQuery = DateTime.Now;
            if (newIsForBot)
                isForBot = newIsForBot;
            #endregion



            // Always add to log
            var q = $"{arg.Author.Username}: {content}" + Program.NewLogLine;
            await Program.Log("chat_log_" + arg.Channel.Name + ".txt", $"{q}");
            room.Log += q;

            if (!isForBot)
                return;

            if (message.Reference != null)
            {
                if (Message.TryGetValue(message.Reference.MessageId.Value, out string context))
                {

                    if (context != null && context != "")
                    {
                        var c = MessageAuthors[message.Reference.MessageId.ToString()] + ": " + context + Program.NewLogLine;
                        room.Log += c;
                        Console.WriteLine($"Context is {c}");
                    }
                }
            }


            //using (arg.Channel.EnterTypingState())
            {
                bool isReply = true;

                // arg.Discord.UserIsTyping += Discord_UserIsTyping;
                if (arg.Channel.Name.Contains("neo") || arg.Channel.Id == 1008443933006770186)// || (arg.Channel.Id == 1009524782229901444 && !content.Contains("story")))
                    content = "$$ " + content;

                if (content.Contains(botName + " edit", StringComparison.OrdinalIgnoreCase))
                {
                    edit = true;
                    content = content.Replace(" edit ", " ");
                    if (content.Contains("delete"))
                    {
                        if (LastMessage.TryGetValue(message.Channel.Id, out var restMsg))
                            restMsg.DeleteAsync().Wait();
                        return;
                    }
                }
                if (content == "dibbr")
                {
                    content = "dibbr, What are your thoughts on this?";
                }
                string str = "";
                bool bReply = false;
                var typing = arg.Channel.EnterTypingState(new RequestOptions() { Timeout = 1, RetryMode = RetryMode.AlwaysFail });

                room.handler.Log = room.Log;
                (bReply, str) = await room.handler.OnMessage(content, arg.Author.Username, true, true);// _callback(content, arg.Author.Username, isReply);
                room.Log = room.handler.Log;


                // Only let bot call self once, to avoid loop but still be funny
                if (q.Contains("dibbr") && isBot)
                    str = str.Replace("dibbr", "dihbbr");

                var a = $"{room.handler.BotName}): {str ?? ""}{Program.NewLogLine}";
                typing.Dispose();
                //  typing.Dispose();
                if (str is null or "") return;



                await SendMessage(str, edit);
            }
            // var guild = _client.GetGuild((message.Channel as SocketGuildChannel).Guild.Id);
            // await message.Channel.SendMessageAsync($"{arg.Content} world!");
            async Task SendMessage(string str, bool edit)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Sending: {str}");

                var msgs = new List<string>();
                while (str.Length > 2000)
                {
                    msgs.Add(str.Substring(0, 2000));
                    str = str.Substring(2000);
                }

                msgs.Add(str);

                foreach (var msg in msgs)
                {
                    var m = msg;
                    try
                    {
                        /*  DiscordEmbedBuilder embed = null;
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
                          }*/

                        // DiscordMessageBuilder d;d.
                        if (edit && LastMessage.TryGetValue(arg.Channel.Id, out var restMessage))
                        {
                            object modify = null;
                            await restMessage.ModifyAsync(msg => msg.Content = m + " [edited]");
                        }
                        else
                            LastMessage[message.Channel.Id] = await message.Channel.SendMessageAsync($"{m}");

                    }
                    catch (Exception e) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(e.Message); }

                    await Task.Delay(1);
                }
            }

        }).Start();

        #region Helpers
        bool ForBot()
        {
            if (!(arg is SocketUserMessage message))
                return false;
            MessageAuthors[arg.Id.ToString()] = arg.Author.Username;
            Message[arg.Id] = arg.Content;
            var id = message.Reference != null ? message.Reference.MessageId.ToString() : "null";
            var mentionsAnyone = MessageAuthors.TryGetValue(id, out string name);
            // var mentions = name == botName;
            if (name == botName)
                return true;

            if (arg.Content.Contains(botName))
            {
                var s = arg.Content.After(botName);
                var idx = s.IndexOf(" ");
                if (idx == -1) idx = s.Length;
                var word = s.Substring(0, idx);
                //This is a hacky way to catch people talking about dibbr
                // dibbr will be useful - won't trigger
                // dibbr will there be an update - will trigger
                if (word.ToLower().HasAny("will", "has", "needs", "should", "can't", "doesn't", "didn't", "won't", "is"))
                {
                    var next = s.After(word).Trim();
                    if (!next.ToLower().HasAny("you", "there"))
                    {
                        return false;
                    }

                }
                return true;
            }
            return false;


        }

        #endregion




        var guild = _client.GetGuild((arg.Channel as SocketGuildChannel)?.Guild?.Id ?? 0);
        var bg = ConsoleColor.Black;
        var fg = ConsoleColor.Gray;
        // If you want to log and notify something, do it here
        if (arg.Content.ToLower().Contains(NotifyKeyword))
        {
            bg = ConsoleColor.DarkBlue;
            fg = ConsoleColor.Red;
            new Thread(delegate ()
            {
                Console.Beep(369, 50);
            }).Start();
        }
        if (arg.Content.Contains("@everyone"))

            Console.WriteLine(guild?.Name);
        Console.BackgroundColor = bg;
        Messages[arg.Id] = arg;
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.Write($"{DateTime.Now} $");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"G:{guild?.Name ?? ""}#{arg.Channel.Name}@{arg.Author.Username}: ");
        Console.ForegroundColor = fg;
        Console.Write($"{arg.Content}\n");
        Console.ForegroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.Black;
        Log($"logs/{arg.Channel.Id}_{arg.Channel.Name}.log", $"{DateTime.Now}#{arg.Author.Username}:{arg.Content}\n");
        Log($"logs/{MakeValidFileName(arg.Author.Username)}#{arg.Author.Discriminator}.log", $"{DateTime.Now}#{arg.Id}_{arg.Channel.Name}:{arg.Content}\n");

        return;
    }

    private Task Discord_UserIsTyping(SocketUser arg1, ISocketMessageChannel arg2)
    {
        // arg2.
        // throw new NotImplementedException();
        return Task.CompletedTask;
    }

    void Log(string file, string txt)
    {
        file = MakeValidFileName(file);
        File.AppendAllText(file, txt);
    }

    Task DoLog(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    string MakeValidFileName(string name)
    {
        string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

        return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
    }
}

