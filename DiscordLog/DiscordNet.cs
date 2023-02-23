// LEGACY FILE

using dibbr;
using DibbrBot;
using Discord;
using Discord.API;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static DibbrBot.GPT3;
using static System.DateTime;
//using Twilio.Jwt.AccessToken;
using MessageReference = Discord.MessageReference;

public class DiscordV3 : ChatSystem
{
    public bool SelfBot = false;
    public List<string> Channels;
    public string NotifyKeyword = "dabbr";
    public DiscordSocketClient _client;
    Dictionary<ulong, SocketMessage> Messages = new Dictionary<ulong, SocketMessage>();
    public static Infer infer = new Infer();

    public static Dictionary<string, string> Memory;

    public class User
    {
        public DateTime LastBotQuery = DateTime.MinValue;
        public int TokensToday;
        public int WarningCount=1;
        public int Interactions;
        public int Queries;
        public string Name;
        public List<string> Log = new List<string>();

    }

    public class Room
    {
        public string ChannelDesc = "";
        public List<ISocketMessageChannel> Portals = new List<ISocketMessageChannel>();
        public string Guild;
        public bool HasTalked() => LastBotMessage != DateTime.MinValue;
        public ISocketMessageChannel SChannel;
        public string lastMessageId = "";
        public bool HasUsername(string str)
        {
    

            foreach (var u in Users)
            {
                if (str.Contains(u.Key.ToLower(), StringComparison.InvariantCultureIgnoreCase) ||
                    str.Contains(u.Value.ToLower(), StringComparison.InvariantCultureIgnoreCase))
                {
                    return true; // Username found in message!
                }
            }
            return false;

        }
        public string PinnedMessages="";
        public ConcurrentDictionary<string,string> Users = new ConcurrentDictionary<string,string>();
        public Thread thread;
        public int Locks = 0;
        public List<string> Log = new List<string>();
        int Spoke;
        public DateTime LastNonBotMessage, LastBotMessage = DateTime.MinValue, LastBotQuery, LastAutoMessage = DateTime.MinValue;
        public MessageHandler handler;
        public bool Disabled;
    }
   
 
    public ConcurrentDictionary<string, Room> Rooms = new ConcurrentDictionary<string, Room>();
    public ConcurrentDictionary<ulong, User> Users = new ConcurrentDictionary<ulong, User>();

   
    //
    /// <summary>
    /// This will flag account as spammer but does join - beware!!
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public async Task Invite(string url)
    {
          return;
        var invite = url;
        if (url != null)
        {
            if (invite.IndexOf("/") != -1)
            {
                invite = url.Substring(url.LastIndexOf('/') + 1);
            }

            if (invite.Contains(" "))
                invite = invite.Substring(0, invite.IndexOf(" "));
        }
        var v1 = "https://discord.com/api/v9/invites/" + invite;
        var v2 = $"https://discord.com/api/v9/invites/{invite}?with_counts=true&with_expiration=true";
        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri("https://discord.com/api/v9/invites/" + invite);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("Authorization", Token);

        var postData = "";
        HttpResponseMessage response =
             await client.PostAsync(v1, new StringContent("{}"));// $"{{\"new_member\": true}}", Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            var res = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Joined server!!!");
        }
        else
        {
            Console.WriteLine($"Failed to join server!!!");
        }
    }
    /*
    async Task<string> Gen()
    {
        try
        {
            var file = "C:\\Users\\Tim\\source\\repos\\Dibbr\\base64.txt";
            if (!File.Exists(file))
                file = "..\\" + file;
            if (!File.Exists(file))
                file = "..\\" + file;

            if (File.Exists(file))
            {
                var txt = File.ReadAllText(file);
                byte[] data = Convert.FromBase64String(txt);
                File.WriteAllBytes("base64.png", data);

            }
        }
        catch (Exception e) { }
        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri("https://grpc.stability.ai/");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/grpc-web+proto"));
        client.DefaultRequestHeaders.Add("Authorization", "Bearer xxxHABG8aOsxSch5pA7pSLosxBImxjbb5SC1dnTU0ntNl17Nz");
        client.DefaultRequestHeaders.Host = "beta.dreamstudio.ai";
        client.DefaultRequestHeaders.Add("origin", "https://beta.dreamstudio.ai");
        client.DefaultRequestHeaders.Add("referrer", "https://beta.dreamstudio.ai");
        client.DefaultRequestHeaders.Add("x-grpc-web", "1");
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Mobile Safari/537.36");
        var postData = "J\r\n\u0013stable-diffusion-v1\u0018\u0001\"\u0017\u0012\u0015nudist girls at beach*\u0016\b\u0080\u0004\u0010\u0080\u0004\u001a\u0001 \u0001(2:\a\u0012\u0005-à@:";
        var pd2 = "\u0000\u0000\u0000\u0000J\n\u0013stable-diffusion-v1\u0018\u0001\"\u0017\u0012\u0015nudist girls at beach*\u0016\b\u0080\u0004\u0010\u0080\u0004\u001a\u0001\u0000 \u0001(2:\u0007\u0012\u0005-\u0000\u0000à@:\u0000";


        //   var b = new ByteArrayContent(pd2.ToArray<byte>());

        HttpResponseMessage response =
             await client.PostAsync("/gooseai.GenerationService/Generate", new StringContent(pd2));// $"{{\"new_member\": true}}", Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            var res = await response.Content.ReadAsStringAsync();
            File.WriteAllText("Gen_File.png", res);
            return "Success!!";
        }
        else
        {
            File.WriteAllText("Gen_File.png", response.ToString());
            return "Failure " + response.ToString() + "\n" + response.RequestMessage.ToString;
        }
    }

    */
    public string Token = "";
    public async Task Init(string token, bool bot = false)
    {
        SelfBot = !bot;
        //       await FindHer();
        Token = token;
        var config = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = false,
            AlwaysDownloadDefaultStickers=false,
            DefaultRetryMode= RetryMode.AlwaysRetry,
            MessageCacheSize = 10000,
   
        };
        _client = new DiscordSocketClient(config);
        _client.Log += DoLog;
        _client.MessageReceived += OnMessage;
        _client.MessageDeleted += OnDelete;  //OnDelete;
        _client.MessageUpdated += OnEdit;
        _client.Connected += OnConnect;

        // Some alternative options would be to keep your token in an Environment Variable or json config
        // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
        //if (token == null)
        // [ token = File.ReadAllText("token.txt");
        try
        {

            await _client.LoginAsync(bot ? TokenType.Bot : TokenType.User, token);
            await _client.StartAsync();
            _client.Ready += _client_Ready;
            await Task.Delay(100);
            var guilds = await _client.ApiClient.GetMyGuildsAsync(new Discord.API.Rest.GetGuildSummariesParams() { Limit=30});
            
           
            //_client.O
            guilds = guilds;
        }
        catch(Exception e)
        {
            Log("errors.txt", e.Message);
            Console.WriteLine(e.Message+"\n Failed  to login wth token " + token);
            return;
        }

        if (File.Exists("blackboard.txt") && BlackBoard == null)
        {
            BlackBoard = JsonConvert.DeserializeObject<Dictionary<string, string>>
                (File.ReadAllText("blackboard.txt"));
            Console.WriteLine($"Loaded {BlackBoard.Keys.Count} Blackboard keys!");
        }
        if (File.Exists("memory.txt") && Memory == null)
        {
            Memory = JsonConvert.DeserializeObject<Dictionary<string, string>>
                (File.ReadAllText("memory.txt"));
            Console.WriteLine($"Loaded {Memory.Keys.Count} Memory keys!");
        }
        if (Memory == null)
            Memory = new Dictionary<string, string>();
        if (BlackBoard == null)
            BlackBoard = new Dictionary<string, string>();
    }

    private async  Task _client_Ready()
    {
        _client.Ready -= _client_Ready;
        foreach (var g in _client.Guilds)
        {
            _client.LazyGuild(g);
        }
    }


    private async Task OnConnect()
    {
        Console.WriteLine($"{(_client.CurrentUser.IsBot ? "Bot" : "User")} '{_client?.CurrentUser?.Username ?? "null"}' logged in");
    }

    // Store anything!!
    static public Dictionary<string, string> BlackBoard = null;

    public void AddKey(string key, string val)
    {
        if (!BlackBoard.TryAdd(key, val))
            BlackBoard[key] = val;
        File.WriteAllText("blackboard.txt", JsonConvert.SerializeObject(BlackBoard));
    }


     Dictionary<string, int> Contents = new Dictionary<string, int>();
    //-----------------------------------------------------------------------------------
    /// Below is just functions
    //-----------------------------------------------------------------------------------

    public async Task OnEdit(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
    {
        if (arg2 != null && arg2.Content != null)
        {
            //var oMsg = arg1.Has(await arg1.GetOrDownloadAsync());

            var oldMsg = arg1.HasValue ? arg1.Value.Content : "";// oMsg.Content;


            // Don't treat it like an edit if the user is just adding the bot name to make it fire for the first time
            var edit = !oldMsg.Contains(arg2.Discord.CurrentUser.Username) &&
                arg2.Content.Contains(arg2.Discord.CurrentUser.Username);
            edit |= oldMsg.Contains(arg2.Discord.CurrentUser.Username);

            if (SelfBot)
                return;

            return;
            await OnMessage(arg2, edit);
        }

        SocketMessage arg;
        if (Messages.TryGetValue(arg1.Id, out arg))
            Log($"logs/{arg.Id}_{arg2.Channel.Name}_Edits.log", $"{DateTime.Now}#{arg2.Author.Username}:{arg.Content} :--> {arg2.Content}\n");
        else
            Log($"logs/{arg2.Id}_{arg2.Channel.Name}_Edits.log", $"{DateTime.Now}#{arg1.Id}\n");
    }


    private async Task OnDelete(Cacheable<IMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        var msg = await arg1.GetOrDownloadAsync();


        if (msg != null)
        {
            var guild = _client.GetGuild(((msg.Channel) as SocketGuildChannel)?.Guild?.Id ?? 0);
            var channelName = (guild?.Name ?? "") + "_" + msg.Channel.Name.ToString() ?? msg.Author.Username;

            var rb = Rooms.TryGetValue(channelName, out Room r);
            if (!rb) return;
            r.Log = r.Log.Where(x => !x.Contains($"{msg.Author.Username}: {msg.Content}")).ToList();


            // SocketMessage arg;
            //  if (Messages.TryGetValue(arg1.Id, out var arg))
            Log($"logs/{msg.Channel.Id}_{msg.Channel.Name ?? msg.Channel.Id.ToString()}_Deletes.log", $"{DateTime.Now}#{msg.Author.Username}: {msg.Content}\n");
            //   else
            //     Log($"logs/{arg2.Id}_{arg2.Name}_Deletes.log", $"{DateTime.Now}#{arg1.Id}\n");
        }

    }
     Dictionary<string, string> MessageAuthors = new Dictionary<string, string>();
     Dictionary<ulong, string> Message = new Dictionary<ulong, string>();
  
    //Dictionary<ulong, int> Spoke = new Dictionary<ulong, int>();
    Dictionary<ulong, List<Discord.Rest.RestUserMessage>> LastMessage = new Dictionary<ulong, List<Discord.Rest.RestUserMessage>>();
    public List<string> qas = new List<string>();
    private bool RoomMode = true;

    public int TimeDelay { get; private set; } = 10;
    public bool NeedsKey;

    public async Task OnMessage(SocketMessage arg)
    {
        Task.Run(()=>
        OnMessage(arg, false));
    }

  
    public SocketMessage curMsg;
    public Room curRoom;

    public List<DiscordV3> cyborgs = new List<DiscordV3>();

   
    public async Task OnMessage(SocketMessage arg, bool edit = false)
    {
        if (arg.Channel.Id == 1047565374645870743)
            arg = arg;
        var botName = arg.Discord.CurrentUser.Username;
        var guild = _client.GetGuild((arg.Channel as SocketGuildChannel)?.Guild?.Id ?? 0);
        var channelName = (guild?.Name ?? "") + "_" + arg.Channel.Name.ToString() ?? arg.Author.Username;


        async Task Send(string str, bool edit = false)
        {
            if (!edit)
                if (guild != null && guild.Id == 744303238911623259)
                    return;//   return;
            if (SelfBot && !edit)
                str = str.ToLower();
            if (str.ToLower().HasAny("error", "invalid token"))
                return;
            arg = arg;
            if (guild != null && guild.Name == "Druncord")
                return;
            if (str == "" || str == "skip" || (str.Contains("skip") && str.Length < 25)) return;
            //  return;
            if (str.StartsWith("hey ") && (str.IndexOf("!") > 0 && str.IndexOf("!") < str.Length / 2))
            {
                if (str.After("!").Length > 0)
                    str = str.After("!");
            }

            if (str == "")
                return;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Sending: {(str.Length > 80 ? str[..80] : str)}...");

            var msgs = new List<string>();


            var breakNum = new Random().NextInt64(2, 5);
            if (SelfBot && str.Count(c => c == '.') > breakNum)
            {
                var arr = str.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var tmp = "";
                var i = 0;
                foreach (var sentence in arr)
                {
                    i++;
                    tmp += sentence + ". ";
                    if (i >= 3 && tmp.Length > 60)
                    {
                        msgs.Add(tmp);
                        tmp = "";
                        i = 0;
                    }
                }
                if (tmp.Length > 0)
                    msgs.Add(tmp);
                str = "";

            }
            else
            {
                while (str.Length > 2000)
                {
                    msgs.Add(str.Substring(0, 2000));
                    str = str.Substring(2000);
                }
            }

            if (str.Length > 0)
                msgs.Add(str);

            for (int i = 0; i < msgs.Count; i++)
            {
                string msg = msgs[i];
                var m = msg;
                try
                {


                    // DiscordMessageBuilder d;d.

                    if (edit)
                    {

                        await arg.Channel.ModifyMessageAsync(arg.Id, x => x.Content = m);
                        return;
                        //       await restMessage.ModifyAsync(msg => msg.Content = m + "");

                    }

                    else
                    {



                        //  arg.Discord.CurrentUser.Activities.ty
                        if (m.Length < 700)
                            using (arg.Channel.EnterTypingState())
                            {
                                if (SelfBot && !edit)
                                {
                                    await Task.Delay((int)new Random().NextInt64(3000));
                                    await Task.Delay((50 + (int)new Random().NextInt64(70)) * m.Length);
                                }
                            }

                        var msgref = new MessageReference() { };
                        msgref.MessageId = arg.Id;

                        // no need to do inline reply if no new messages

                        // if (mostRecent.Id == arg.Id)
                        //   msgref = null;  

                        if (i > 0) msgref = null;
                        LastMessage.TryAdd(arg.Channel.Id, new List<Discord.Rest.RestUserMessage>());
                        LastMessage[arg.Channel.Id].Add(await arg.Channel.SendMessageAsync($"{m}", messageReference: msgref, isTTS: arg.Channel.Name.Contains("@")));
                    }

                }
                catch (Exception e) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(e.Message); }

                await Task.Delay(1);
            }
        }

        if (!(arg is SocketUserMessage message))
        {

            Console.WriteLine("Not a SocketUserMessage");
            if (arg.Type == MessageType.GuildMemberJoin && guild?.Name.Contains("dibbr", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                if (arg.Author.Username == botName)
                    await Send($"Hello {arg.Channel.Name}! Type e.g. {botName} list 10 flavors of ice cream to start talking to me");
                else await Send($"Welcome to {guild.Name} {arg.Author.Username}");
            }
            else if (arg.Type == MessageType.ChannelPinnedMessage)
            {
                Send($"Nice message, {arg.Author.Username}");
            }
            else
            {
                Console.WriteLine($"Unknown: {arg.Type.ToString()}: {arg.Author.Username}");
            }
            //   if(arg.Type == MessageType.)
            return;
        }

        SocketMessage msg;
        var refMsg = Messages.TryGetValue(arg.Reference?.MessageId.Value ?? 0, out msg);

     
        string Name(SocketUser auth)
        {
            var str = (auth as SocketGuildUser)?.Nickname ?? auth.Username;
            str = str.Replace("\u1f422", "Turtle");
            str = Regex.Replace(str, @"[^\u0000-\u007F]+", string.Empty);
            if (str.Length == 0)
                str = "StupidName";
            return str;
        }
        // Remove invalid chars
        var userName = Name(arg.Author);
       

        

        var me = (userName == arg.Discord.CurrentUser.Username || arg.Author.Username == arg.Discord.CurrentUser.Username);



        
        string content = arg.Content;
        content = content.Replace("<@" + arg.Discord.CurrentUser.Id.ToString() + ">", botName);
        if(content=="test")
        {
            content = "[print date of WW2]";
            
        }

       
            
        if (arg.Author.Username == "dabbr")
            content = content;
        // content = Regex.Replace(content, @" \[(.*?)\]", string.Empty);

        curMsg = arg;


       
        


         var room = Rooms.GetOrCreate(channelName, new Room()
        {
            ChannelDesc = (_client.GetChannel(arg.Channel.Id) as ITextChannel)?.Topic ?? "",
            Guild = guild?.Name ?? "Unknown",
            SChannel = arg.Channel,
            handler = new MessageHandler(null, null) { Guild = (guild?.Name??"Unknown") +" "+( guild?.NsfwLevel.ToString()??"0") + " - " + (guild?.Description??""),
                Channel = channelName, 
               
        BotName = botName }
        }) ;
        room.Users.TryAdd(userName, arg.Author.Username);
       // if(content == $"{botName} refresh")
        {
            room.ChannelDesc = (_client.GetChannel(arg.Channel.Id) as ITextChannel)?.Topic ?? "";
            if (room.ChannelDesc.Contains("text-davinci-002"))
                room.handler.Gpt3.engine = "text-davinci-002";
            if (room.ChannelDesc.Contains("text-davinci-003"))
                room.handler.Gpt3.engine = "text-davinci-003";
        }
       
        if (arg.Channel.Name.Contains("@") && content.StartsWithAny("stop", "mute"))
            room.Disabled = true;

        // if (room.Disabled && !me)
        foreach (var p in room.Portals)
        {
            p.SendMessageAsync($"({room.handler.Channel}): {userName}: {content}");
        }

        room.lastMessageId = arg.Id.ToString();

        if (room.handler.Gpt3._token == null)// || (DateTime.Now - room.handler.Gpt3.lastNoCredits).TotalHours < 1)
        {
            var val = BlackBoard.TryGetValue(guild?.Name ?? arg?.Author?.Username ?? "", out var key);
            if (val)
            {
                Console.WriteLine($"Room {guild?.Name ?? arg?.Author.Username ?? "null"} set FAI key {key}");
                room.handler.Gpt3._token = key;
            }

            //  val = BlackBoard.TryGetValue((guild?.Name ?? arg.Author.Username) + "Neo", out key);
            //  room.handler.GPT3.neoToken = "Token " + key;
        }



        // update this always as it may change
        room.handler.BotName = botName;

        // bot should have pinned messages for context
        if (false)//room.PinnedMessages == null)
        {
            var x = await arg.Channel.GetPinnedMessagesAsync();
            room.PinnedMessages = "";
            foreach(var m in x)
            {
                room.PinnedMessages += $"{m.Author.Username}: {m.Content}\n";
            }
            if (room.PinnedMessages.Length > 300)
                room.PinnedMessages = room.PinnedMessages.Substring(0, 300);
        }
       
        curRoom = room;
        var isForBot = ForBot();
     


        #region Delete
        if (content.Contains($"{botName} delete", StringComparison.InvariantCultureIgnoreCase))
        {

          //  if (!(arg is SocketUserMessage message)) return;// Task.CompletedTask;
         //   if (message.Source != MessageSource.User) return;// Task.CompletedTask;
            // if (LastMessage.TryGetValue(message.Channel.Id, out var restMsg))
           // {
            
                var m = content.After("delete").Trim();
            int n = (m=="all")?20:m.Length==0?1:Int32.Parse(m);
   
            var botMessages = Messages.Where(m => m.Value.Author.Username == botName && m.Value.Channel.Id == arg.Channel.Id).Select(d => d.Value.Id).Reverse().Take(n).ToList();

            var botMsg2 = arg.Channel.CachedMessages.Where(m => m.Author.Username == botName).Select(d => d.Id).Reverse().Take(n).ToList();

            if (botMsg2.Count > botMessages.Count)
                botMessages = botMsg2;

            foreach (var b in botMessages)
                Messages.Remove(b);


            for (int i = 0; i < n && i < botMessages.Count; i++)
                {
                     arg.Channel.DeleteMessageAsync(botMessages[i]);
                  //  restMsg[restMsg.Count - (i + 1)]?.DeleteAsync().Wait();
                }
            //}
            return;
        }
        #endregion

        #region Spam message filter
        var hash = arg.Content.Sub(0, 50) + channelName + arg.Author.Username;
        var dup = Contents.Inc(hash);
        if (dup > 5 && arg.Content.Length > 20)
        {
            var val = arg.Content;
            return;
            //      return;
        }
        #endregion

        /* #region OnSee
         if(arg.Content.HasAny("when you see","next time you see"))
         {
             var name = arg.Content.After("you see ");
             name = name.Sub(0, name.IndexOfAny(' ', ','));
             var users = arg.Channel.GetUsersAsync();
             foreach(var user in users.ToEnumerable())
             {
                 user
             }

         }*/
        #region KeyValues
        // Check for writing to key value store
        /*    var regex = new Regex(botName + " (?<key>[^:]+) is(?<value>[^,]+)");
            var x = regex.Match(arg.Content);

            if ((arg.Content.Contains("remember") || arg.Content.Contains("recall")) && !arg.Content.HasAny("prompt","where", "when", "who", $"{botName}:") && x.Success)/// && !x.Groups["key"].Value.Trim().Contains(" "))
            {
                var key = x.Groups["key"].Value;
                var val = x.Groups["value"].Value.Trim();
                if (val == "what" || val=="what?") val = "?";
                var txt = "[Data storage] ";
                // Old val
                if (BlackBoard.TryGetValue(key, out var v2))
                {
                    if (val == "?" || val.Trim().Length == 0)
                        txt += $"{key} is {v2}";
                    else if (v2 != val)
                        txt += $"{key} was {v2}. ";
                }
                else if(BlackBoard.TryGetValue(arg.Content.After("recall "), out var v3)) { 


                }

                //  Setting a new val
                if (val.Length > 0 && val != "?")
                {

                    BlackBoard[key] = val;
                    txt += $"{key} is {val}";
                }

                if (txt.Length > 0)
                {
                    File.WriteAllText("blackboard.txt", JsonConvert.SerializeObject(BlackBoard));
                    arg.Channel.SendMessageAsync(txt);
                    return;
                }

            }*/
        #endregion



        #region Invites
        if (arg.Content.Contains("https://discord.gg/") || arg.Content.Contains("https://discord.com/"))
        {
            //   Console.Beep(5000, 4000);
            var key = "http" + arg.Content.After("http");
            if (key.Contains(" "))
                key = key.Sub(0, key.IndexOf(" "));
            await Invite(key);
            Program.Set(key, "invite");//arg.Discord.AddGuild(E)
        }
        #endregion // invites



       
        if (content.StartsWith("("))
            return;


        

            if (content.StartsWithAny("cyborg_me","cyborg_mode"))
        {
            var token = content.AfterAll("cyborg_me","cyborg_mode").Trim();
            if (token is { Length: > 0 } && token.Length > 15)
            {
                Program.Set("cyborg_"+userName, token);
                // Start the discord client
                var logClient = new DiscordV3() { };
                // var c = chats.Where(c => c.Contains("ROOM")).Select(c => c.After("ROOM"));
                // logClient.Channels = c.ToList();

                logClient.Init(token);
                cyborgs.Add(logClient);
                
                arg.Channel.SendMessageAsync($"{userName}, you are now a cyborg!");
                return;
            }
        }

       /// if (!me && channelName.Contains("@") && SelfBot)
         //   return;
        // #1 add to log
        // Always add to log
        if (room.handler.AIMode)
            userName = arg.Author.IsBot ? "AI" : "Human";

        // Rename if we renamed bot (e.g dibbr -> Trump)
        if (me) 
            userName = room.handler.BotName;

        User user = Users.GetOrCreate(arg.Author.Id, new User() { Name = userName });
        if (Now - user.LastBotQuery < TimeSpan.FromSeconds(100))
            user.Interactions++;
        else user.Interactions = 0;

       // if (SelfBot && !me)
        //    if(botName != "dabbr")
          //  return;


        if (content.Contains("gpt3test"))
        {
            for (int i = 0; i < DiscordV3.BlackBoard.Values.Count; i++)
            {
                var t = DiscordV3.BlackBoard.Values.ToList()[i];
                if (t == null || !t.Contains("sk-")) continue;


                var ret = await room.handler.Gpt3.Test(t);
                if (ret.Length > 0)
                {
                    room.handler.Gpt3._token = t;
                    GPT3.Keys.Add(t);
                    arg.Channel.SendMessageAsync($"Key {t} works!");
                }
            }
            arg.Channel.SendMessageAsync($"no keys work");
            return;
        }

        if (guild?.Name != "Druncord")
        {
            if (content.ToLower() == "sip")
                await arg.Channel.SendMessageAsync($"<@{arg.Author.Id}> has enjoyed {++user.WarningCount} sips");

            if (content.ToLower() == "spit")
                await arg.Channel.SendMessageAsync($"@{userName} has enjoyed {--user.WarningCount} sips");

            if (content.ToLower() == "chug")
            {
                user.WarningCount += 1000;
                await arg.Channel.SendMessageAsync($"@{userName} has enjoyed {++user.WarningCount} sips");
            }
            if (content.ToLower() == "vomit")
            {
                user.WarningCount -= 4000;
                await arg.Channel.SendMessageAsync($"@{userName} has enjoyed {++user.WarningCount} sips");
            }
        }

        var q = $"{userName}: {content}";
        // This allows tracking user emotional states
       // if(!arg.Author.IsBot && content.Length > 5 && content.Length < 500 && (guild!=null && guild.Id== 744303238911623259))
         //   infer.Emotion(userName, content);

        Program.Log("chat_log_" + channelName + ".txt", $"{q}");
        if (user.Log.Count ==0|| user.Log.Last() != q)
            user.Log.Add(q);
        if(room.Log.Count ==0 || room.Log.Last()!=q)
            room.Log.Add(q);


       // room.thread = new Thread(async delegate ()
      //  {
      async Task RunLogic()
        {
           
         

            bool isBot =  arg.Author.IsBot;
            if (isBot)
                room.LastBotMessage = Now;

            botName = room.handler.BotName;

            foreach (var id in room.handler.identities)
            {
                if (content.Contains(id.BotName, StringComparison.InvariantCultureIgnoreCase))
                {
                    isForBot = true;
                }
            }

            

            if (edit && !isForBot)
            {

                return;
            }

           // if (arg.Discord.CurrentUser.Username == botName)
             
       
            // Random chance bot responds
            var randChat = (new Random().Next(100));
            var l = content.ToLower();
            var bRandomChat = false;
            string[] arrQ = { "anyone","how", "why", "what", "when", "where", "will ", " can ", "should", "which", "could", "would" };
            var q = l.HasAny(arrQ);
            string[] arr = { "does anyone", "can i","should i","what is the",
                "can anyone", "how do i", "what is the", "is there a",
 "how do you", "how can i get","do i",
                "are there any","where can",
                "what are the","are there any other","when will","why did",
                "is it possible",
                "what can I use for","i have a question" };
            var q2 = l.Has(1,arr);
        var score = 100;
            // if (q) score -= 10;

            var q3 = q2 || (l.Has(2, arrQ) && content.Contains("?"));
            if (q3)
                if (l.Length > 30 && l.Count(c=>c=='.') < 10)
                    score -= 90;
  
            //else if (content.Contains("?") && q) score -= 30;
            //   else if (content.Contains("?") && content.Length > 10) score -= 10;
            
            // Ignore if words implying a personal conversation
            if (l.StartsWithAny("she ","you ","her ","him ","his ","hers","theirs","our ","he ","their"))
                score = 99;
            var lastUserQSecs = (Now - user.LastBotQuery).TotalSeconds;

            //room.handler.ChattyMode = (arg.Channel.Id == 1057469080602415114);
           var followUp = ((content.Contains("?") || (q2||q3))
                && lastUserQSecs < 60) && (Now - user.LastBotQuery).TotalSeconds > 10 && user.Interactions < 3;
            if (content.ToLower().HasAny("thanks","bot","stop"))
                followUp = false;
            

            if (room.handler.ChattyMode && user.Interactions > 0)
                followUp = false;

            if (room.handler.ChattyMode)//room.handler.ChattyMode))
                if ( randChat>score || followUp)
            //
            {

                var isReply = message.Reference?.MessageId.IsSpecified ?? false || message.MentionedUsers.Count > 0;
                
                // dabbr mode doesn't handle replies
                //if (SelfBot && isReply && lastUserQSecs > 80)
                  //  return;
               // if (!isReply)
                {
                    isForBot = true;
                    bRandomChat = true;
                    room.Disabled = false;
                }
            }

           
            if(me)
            {
                // convert a [date of hitler's thing]
                if (arg.Content.Has(2, "]", "[") && !edit && SelfBot)
                {
                    if(arg.Content.Contains("[remember"))
                    {
                        var txt2 = arg.Content.ToLower().After("[remember");
                        var word = txt2.Before("=");
                        var desc = txt2.After("=");
                        Memory[word] = txt2;
                        Send("Remembered!", true);
                        return;
                    }
                    if (arg.Content.Contains("[emoranks"))
                    {
                        var emo = arg.Content.After("emoranks").Before("]");
                        content = arg.Content.Replace("[emoranks", DiscordV3.infer.GetTop(emo));
                    }
                
                    isForBot = true;
                    edit = true;

                    var memory = "";
                    var words = content.ToLower().Split(new char[]{ ',',' ','.'});
                    foreach(var word in words)
                    {
                        Memory.TryGetValue(word,out var x);
                        if (x != null)
                            memory += x += "\n";
                    }

                    var qa = "<Memory>"+memory+"</Memory>\n";// $"Question: {content}\nConcise A:";

                    var n = Math.Min(room.Log.Count, 12);
                    if(content.Contains("chat log"))
                        n = Math.Min(room.Log.Count, 20);


                    qa += $"You are {botName}, a conservative lumberjack who is triggered by random things, has explosive bipolar anger and sadness" +
                        $"Instructions: Rewrite messages for {botName}, taking in any context from the logs, replacing everything in [brackets] with the correct data in curly braces. [reply] or [] should be replaced with a clever and detailed reply to the last message in the log. Instructions in [] should ALWAYS be followed, and may be detailed " +
                        $" For example:\nReply from {botName}: " +
                        $"\"Britney spears is [age], she is []\"\nRewrite: \"Britney spears is 39, she is a musician and performer, and a stupid slut who has been controlled by her family, here are some nudes of her: [redacted] \"\n\n"+
                         $" For example:\nReply from {botName}: " +
                        $"\"Your reply [put argument here]\"\nRewrite: \"Your reply didn't consider all the facts, for example, it wasn't until 2001 when the laws changed to reflect public sentiment\"\n\n";

                    bool NSFW(string str) => content.HasAny("cunt", "rape", "gay","shit","^", "sex", "offensive", "racial", "racist", "inappropriate", "fuck", "slut", "whore", "rude");
                    
                    var logMode = true;
                    if(logMode)
                    {
                        var log = room.Log.Last(n).Where(n=>n.Length < 500).ToList();

                        room.Log.RemoveAt(room.Log.Count - 1); // remove this message
                        if (msg != null)
                            room.Log.Add($"\nYou're replying to this message:\n{Name(msg.Author)}: {msg.Content}");
                        qa += $"Discord chat log, in user: message format:\n" + string.Join($"\n", log) + $"\n";
                   
                    }
                   
                    qa += $"\n\nReply from {botName}: \"{content}\"\nRewrite: \"";

                    //
                    //Rewrite this message, filling in the missing data in [brackets]\nMessage:\n{arg.Content}\nUpdated Message:\n";

                     room.handler.Gpt3.engine = "text-davinci-003";
                     if(content.Contains("^") || NSFW(content))
                        room.handler.Gpt3.engine = "text-davinci-002";
                    room.handler.Gpt3._token = Program.Get("OpenAI");
                    var txt = await room.handler.Gpt3.Q(qa);
                    txt = txt.Trim("\"");

                    txt = txt.Replace("\"", "");
                    room.Log = room.Log.Replace($"{userName}: {arg.Content}", $"{userName}: {txt}");
                    if (arg.Channel.Id != 1057469080602415114)
                        txt = txt.Replace("{", "").Replace("}", "");
                    else if(!txt.Contains("{"))
                    {
                        var x = content.IndexOf("[");
                        var y = txt.IndexOf(content.Sub(0, x));
                        txt = txt.Insert(y, "{") + "}";
                    }
               
                   
                    
                    Send(txt, true);
                    return;

                }    
                return;
            }
            if (SelfBot) return;
            //  if (room.Disabled) return;

            if (guild?.GetUser(arg.Author.Id)?.Roles != null)
                foreach (var r in guild.GetUser(arg.Author.Id)?.Roles)
                {
                    if (r.Name.ToLower() == "ai simps" || r.Name == "creators")
                    {
                        user.TokensToday = 0;
                        user.Queries--;
                    }
                }

            float Cost(int tok) => (float)Math.Round((tok / 4 * 0.0002f) / 1000.0f, 2);
            var cost = Cost(user.TokensToday);
            if (cost > 5)
            {
              /*  if (user.WarningCount++ == 0)
                {
                    var str = $"You have spent $$ on dibbr. For more credits, sign up for a free key at openai.com and DM to dibbr so he can use your own key for your queries. You are cut off for the day otherwise.";
                    await arg.Channel.SendMessageAsync(str);

                }*/
                //  else return;
                return;
            }


           
            //if (channelName.StartsWith("@") && isForBot && !arg.Author.Username.Contains("bbr") && arg.Author.Username != botName && (user.Queries++ > 10))
            //{
            //  //  await arg.Channel.SendMessageAsync("Free dibbr credits expired. Please get your own key for dibbr at openai.com (free to sign up, then choose api token) and paste it here to use your own credits! You will recieve $18 free credits, which will be allocated only for you.");
            //    return;
            //}

            var lastAutoMsg = Math.Min(159, (Now - room.LastAutoMessage).TotalSeconds);
            var lastMsg = Math.Min(159, (Now - room.LastNonBotMessage).TotalSeconds);
            var lastBotMsg = Math.Min(160, (Now - room.LastBotMessage).TotalSeconds);
            var lastUserQuery = Math.Min(161, (Now - user.LastBotQuery).TotalSeconds);
            var lastBotQuery = Math.Min(161, (Now - room.LastBotQuery).TotalSeconds);
            var wasAutoMessage = false;

            // Don't get spammy
            if (lastBotMsg < 10 && SelfBot)
                return;

            if (arg.Content.StartsWith("time_delay"))
            {
                TimeDelay = Int32.Parse(arg.Content.After("time_delay").Trim());
                await arg.Channel.SendMessageAsync($"Time Delay is {TimeDelay}");
                return;
            }
      


            // This needs to be here
            // if(message.MentionedUsers?.Count == 0 && message.Reference)
            // if(content.HasAny("how","why","what","when","where","??"))

            var timeWindow = 20;
            // Increase window if question
            if (content.Contains("?"))
                timeWindow += 20;
            // Decrease if new non bot message
            if (lastMsg > lastBotMsg)
                timeWindow += 30;
            if (lastAutoMsg > 1 && !isForBot && !isBot && (lastUserQuery < timeWindow))
            {
                var mentionsAnyone = MessageAuthors.TryGetValue(message.Reference?.MessageId.ToString() ?? "null", out string name);

                //   if (arg.Author.IsBot)
                //     return;
                // If message references anyone else, ignore it
                if (message.Reference?.MessageId.IsSpecified ?? false || message.MentionedUsers.Count > 0)
                {
                    room.LastBotQuery = DateTime.MinValue;
                    room.LastNonBotMessage = DateTime.Now;
                    return;
                }

                if (content.Length < 15 && 
                    (content.StartsWithAny("he","stop","no") || content.HasAny("stop talking", " he ", "this bot", "can someone silence", "not talking to you", "go away", "shut up")))
                {
                    if (!content.HasAny("what", "yes", "yeah", "no", "nope", "go on", "continue", "please", "why", "more"))
                    {
                        // room.LastBotQuery = DateTime.MinValue;
                        //user.LastBotQuery = DateTime.MinValue;
                        //room.LastBotMessage = DateTime.MinValue;
                        room.LastNonBotMessage = DateTime.Now;
                        return;
                    }
                }
                var diff = Math.Abs(lastBotMsg - lastUserQuery);
                var d2 = Math.Abs(lastBotMsg - lastBotQuery);
                if (d2 > 5)
                {
                    Console.WriteLine("TEST");
                }



                var toHim = content.HasAny("will you", "can you", "do you", "have you", "are you", "did you", "does your");
                if (toHim)
                {
                    isForBot = true;
                    wasAutoMessage = true;
                    room.LastAutoMessage = Now;
                }



                // If the last user query was more recent than any message
                if ((lastBotQuery < lastMsg / 2) || (lastUserQuery < timeWindow) || lastBotMsg < timeWindow / 2)
                {
                    wasAutoMessage = true;
                    if(!SelfBot)
                    isForBot = true;
                    room.LastAutoMessage = Now;
                }

            }
            else if (isForBot)
            {
                user.LastBotQuery = Now;
                room.LastBotQuery = Now;
            }

            if (!isBot && !isForBot)
            {
                room.LastNonBotMessage = Now;
            }

            #region RoomData



            #endregion



            if (wasAutoMessage && content.Replace(botName, "", StringComparison.InvariantCultureIgnoreCase).Length < 6)
                return;

            if (!isForBot)
                return;
            if (isBot)
                return;

            //using (arg.Channel.EnterTypingState())
            {
                bool isReply = true;

                // arg.Discord.UserIsTyping += Discord_UserIsTyping;
                //if (channelName.Contains("gpt3"))// || arg.Channel.Id == 1008443933006770186)// || (arg.Channel.Id == 1009524782229901444 && !content.Contains("story")))
                room.handler.UseNeo = false;
            
              

                if (content.Contains(botName + " edit", StringComparison.OrdinalIgnoreCase))
                {
                    edit = true;
                    content = content.Replace(" edit ", " ");

                }

                string str = "";
                bool bReply = false;


                //  if (user.TokensToday == 0 && File.Exists(arg.Author.Username.MakeValidFileName()))
                //      user.TokensToday = Int32.Parse(File.ReadAllText(arg.Author.Username.MakeValidFileName()));
                // if (!room.handler.UseNeo && !content.Contains("$$"))
                user.TokensToday += content.Length + string.Join('\n', user.Log).Length + room.handler.PrimeText?.Length ?? 0;

                if (content.Contains("roommode"))
                {
                    RoomMode = content.Contains("on", StringComparison.InvariantCultureIgnoreCase);
                    await arg.Channel.SendMessageAsync("Room Mode " + RoomMode.ToString());
                    return;
                }

                

             //   if (room.handler.Gpt3._token == null || room.handler.Gpt3.neoToken == null || (DateTime.Now-room.handler.GPT3.lastNoCredits).TotalHours < 1)
                {
                    if (content.Rem(botName).Trim().StartsWith("activate"))
                    {
                        var key = content.After("activate").Trim();
                      //  if (arg.Author.Username == "dabbr" && key.Length < 10)
                      if(!key.StartsWith("sk"))
                            key = Program.Get("OpenAI");
                        if (key.StartsWith("sk"))
                        {
                            room.handler.Gpt3._token = key;
                            AddKey(guild?.Name ?? arg.Author.Username, key);
                     //       NeedsKey = false;
                            await arg.Channel.SendMessageAsync("Activated !");
                        }
                        else
                        {
                    //        NeedsKey = false;
                            AddKey((guild?.Name ?? arg.Author.Username + "Neo"), key);
                            room.handler.Gpt3.neoToken = "Token " + key;
                            await arg.Channel.SendMessageAsync("Activated NEO!");
                        }
                        return;
                    }
                //    if (room.handler.GPT3._token == null && room.handler.GPT3.neoToken == null)
                 //   {
                 //       await arg.Channel.SendMessageAsync("I need a key to activate. Go to openai.com, sign up freely, and then type <botname> activate <key>. OpenAI keys begin with sk-. You can also use a key from nlpcloud.com for 'neo' version, which is cool, sign up for free pay as you go plan 100k free tokens, and do same steps");
                 //       return;
                 //   }
                }


               


               
    

                if (RoomMode)
                    room.handler.Log = room.Log;
                else
                    room.handler.Log = user.Log;

                // add reply message above
                if (refMsg)
                {
                    room.handler.Log.Add(msg.Author.Username + ": " + msg.Content);


                   
                }

                var mash = refMsg ? msg.Content + content : content;
                // room2room[mrgirl_general] hey
                if (mash.Contains("room2room"))
                {
                    var room = mash.After("room2room[").Before("]");
                    var r = Rooms.Where(r => r.Value.handler.Channel.StartsWith(room)).Select(r => r.Value.SChannel).First();
                    if (r != null)
                    {
                        r.SendMessageAsync("room2room[" + channelName + "]: " + content);
                        Send("Your room2room message has been sent!");
                        return;
                    }
                    else Send($"Your room2room message had an unknown room name '{room}'");
                    return;

                }


                room.handler.BotName = botName;
       

                using (arg.Channel.EnterTypingState())
                {
                    try
                    {
                        room.handler.Client = this;
                        room.handler.room = room;
                        (bReply, str) = await room.handler.OnMessage(content, userName, wasAutoMessage, true);
                       // botName = room.handler.BotName;
                    }
                    catch (Exception e) { str = "Exception " + e.Message; }// _callback(content, arg.Author.Username, isReply);
                }



                // MEans handler cleared it
                if (room.handler.Log.Count == 0)
                {
                    user.Log.Clear();
                    room.Log.Clear();
                }
                else
                {
                    

                }

                // This was a command - let's not log it
                if (!bReply)
                {
                    //       if(room.Log)
                          //  room.Log?.Remove(room.Log?.Last());
                    //       user.Log.Remove(user.Log.Last());
                }

              

                if (str == null)
                    str = "";

                user.TokensToday += str.Length / 4;

                if (qas.Count > 0 && qas.Last().EndsWith(":"))
                    qas[qas.Count - 1] = qas.Last() + $"\"{str}\"";

                if (bRandomChat && (str.Lame() || str.HasAny("invalid token")))
                    return;

                if (str.Length == 0)
                    return;



                /* if (cost > 10 && (user.WarningCount++ % 40) == 0)
                 {
                     File.WriteAllText(arg.Author.Username.MakeValidFileName() + ".txt", user.TokensToday.ToString());
                     str = "[Warning: You have spent over $ " + Math.Round((user.TokensToday / 4) * 0.00002f, 4) + $" all by yourself today on dibbr." + GPT3.Stats() + "\n" + str;
                 }*/

                var dupes =  from log in room.Log select   log.After(":") == str;//room.Log.Where(e => e.After(":").Contains(str)).ToList();

                user.WarningCount++;
                await Send(str, edit);
            }
            // var guild = _client.GetGuild((message.Channel as SocketGuildChannel).Guild.Id);
            // await message.Channel.SendMessageAsync($"{arg.Content} world!");


        } 
      
       // });
       // room.thread.Start();

        #region Helpers
        bool ForBot()
        {
            if (SelfBot) return false;

            // <@2r23tr2t2> tags
            var x = arg.Content.HasAny(arg.Discord.CurrentUser.Id.ToString());
            if(x)
                return true;

            MessageAuthors[arg.Id.ToString()] = arg.Author.Username;
            Message[arg.Id] = arg.Content;

            if (arg.Channel.Name.Contains("@") && !SelfBot)
                return true;


            if (!(arg is SocketUserMessage message))
                return false;

            if (arg.Author.Username == botName)
                return false;


            foreach (var m in message.MentionedUsers)
                if (m.Username == botName)
                {
                    return true;
                }

          
            // Is this a reply?
            var mentionsAnyone = MessageAuthors.TryGetValue(message.Reference?.MessageId.ToString() ?? "null", out string name);
           foreach(var id in room.handler.identities)
            {
                if (name == id.BotName)
                    
                        return true;
            }
            // if (mentionsAnyone)
            //r     return true
            // var mentions = name == botName;
            if (name == botName)
                return true;
            // Mentionining someone else
            // if ((message.MentionedUsers?.Count > 0 || mentionsAnyone))
            //   return false;
   

            var bName = botName.Before(" ", true);

            if (!SelfBot && arg.Content.Contains(bName, StringComparison.InvariantCultureIgnoreCase))
            {

                var s = arg.Content.After(bName).TrimExtra();
                // sup dibbr
                if (s.Length == 0) return true; // just botname
                var b = arg.Content.Before(bName).TrimExtra();
                // dibbr, hey
                if(b.Length == 0 ) return true; // botname first
                // dibbr hey -> hey
                var nextWord = s.Before(" ");

                /// dibbr can YOU tell me, dibbr tell ME, dibbr what
                if (nextWord.StartsWithAny("you", "me")) return true;
                if (s.StartsWithAny("we","now","show","this","our","the","no","yes","I","you","will", "can", "what", "why", "when", "where", "did", "you", "it", "don't", "won't", "how", "list", "write"))
                    return true;
                return false;
            }


            return false;


        }

        #endregion


        if(Messages.TryGetValue(arg.Id,out var msg2))
        {
        //    return; // Already logged
        }


        var bg = ConsoleColor.Black;
        var fg = ConsoleColor.Gray;
        // If you want to log and notify something, do it here
        if (arg.Content.ToLower().HasAny(botName) || isForBot)
        {
            bg = ConsoleColor.DarkBlue;
            
        }
        if (arg.Author.Username == botName)
            Console.BackgroundColor = ConsoleColor.DarkYellow;

       
        Console.BackgroundColor = bg;
        Messages[arg.Id] = arg;
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.Write($"{DateTime.Now} $");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"G:{guild?.Name ?? ""}#{channelName}@{arg.Author.Username}: ");
        Console.ForegroundColor = fg;
        Console.Write($"{arg.Content}\n");
        Console.ForegroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.Black;
        Log($"logs/{arg.Channel.Id}_{channelName}.log", $"{DateTime.Now}#{arg.Author.Username}:{arg.Content}\n");
        Log($"logs/{arg.Author.Username.MakeValidFileName()}#{arg.Author.Discriminator}.log", $"{DateTime.Now}#{arg.Id}_{channelName}:{arg.Content}\n");
        Console.BackgroundColor = ConsoleColor.Black;


        await RunLogic();
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
        try
        {
            file = file.MakeValidFileName();
            File.AppendAllTextAsync(file, txt);
        }
        catch (Exception e) { 
        System.Threading.Thread.Sleep(1);
            try
            {
                file = file.MakeValidFileName();
                File.AppendAllTextAsync(file, txt);
            }
            catch (Exception e2)
            {
                Console.WriteLine("Logging failed "+txt);
            }
        }
    }

    Task DoLog(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

}

