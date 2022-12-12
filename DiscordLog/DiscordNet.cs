// LEGACY FILE

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
using System.Reflection.Metadata;
using Discord.API;
using ServiceStack.Script;
using static DiscordV3;
using OpenAI_API;
using Twilio.Jwt.AccessToken;
using System.Reactive.Joins;
using MessageReference = Discord.MessageReference;
using ServiceStack;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using ChatGPT3;

public class DiscordV3 : ChatSystem
{
    public List<string> Channels;
    public string NotifyKeyword = "dabbr";
    public DiscordSocketClient _client;
    Dictionary<ulong, SocketMessage> Messages = new Dictionary<ulong, SocketMessage>();


    public class User
    {
        public DateTime LastBotQuery = DateTime.MinValue;
        public int TokensToday;
        public int WarningCount;
        public int Queries;
        public string Name;
        public List<string> Log = new List<string>();

    }

    public class Room
    {
        public ConcurrentDictionary<string,string> Users = new ConcurrentDictionary<string,string>();
        public Thread thread;
        public int Locks = 0;
        public List<string> Log = new List<string>();
        int Spoke;
        public DateTime LastNonBotMessage, LastBotMessage, LastBotQuery, LastAutoMessage = DateTime.MinValue;
        public MessageHandler handler;
        public bool Disabled;
    }
   
 
    ConcurrentDictionary<string, Room> Rooms = new ConcurrentDictionary<string, Room>();
     ConcurrentDictionary<ulong, User> Users = new ConcurrentDictionary<ulong, User>();

   
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
        client.DefaultRequestHeaders.Add("Authorization", "Bearer sk-kpHABG8aOsxSch5pA7pSLosxBImxjbb5SC1dnTU0ntNl17Nz");
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


    public string Token = "";
    public async Task Init(string token, bool bot = false)
    {
 //       await FindHer();
        Token = token;
        var config = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            DefaultRetryMode= RetryMode.AlwaysRetry,
            MessageCacheSize = 10000,
            
            

        };
        _client = new DiscordSocketClient(config);
        _client.Log += DoLog;
        _client.MessageReceived += OnMessage;
        //_client.MessageDeleted += OnDelete;
        _client.MessageUpdated += OnEdit;

        // Some alternative options would be to keep your token in an Environment Variable or json config
        // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
        //if (token == null)
        // [ token = File.ReadAllText("token.txt");
        try
        {
            await _client.LoginAsync(bot ? TokenType.Bot : TokenType.User, token);
            await _client.StartAsync();
        }
        catch(Exception e)
        {
            Log("errors.txt", e.Message);
            Console.WriteLine(e.Message+"\n Failed  to login wth token " + token);
        }
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
    public void AddKey(string key, string val)
    {
        if (!BlackBoard.TryAdd(key, val))
            BlackBoard[key] = val;
        File.WriteAllText("BlackBoard.Txt", JsonConvert.SerializeObject(BlackBoard));
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
            await OnMessage(arg2, edit);
        }

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
            Log($"logs/{arg.Id}_{arg.Channel.Name ?? arg.Channel.Id.ToString()}_Deletes.log", $"{DateTime.Now}#{arg.Author.Username}:{arg.Content}\n");
        else
            Log($"logs/{arg2.Id}_{arg2.Name}_Deletes.log", $"{DateTime.Now}#{arg1.Id}\n");


    }
     Dictionary<string, string> MessageAuthors = new Dictionary<string, string>();
     Dictionary<ulong, string> Message = new Dictionary<ulong, string>();
  
    //Dictionary<ulong, int> Spoke = new Dictionary<ulong, int>();
    Dictionary<ulong, List<Discord.Rest.RestUserMessage>> LastMessage = new Dictionary<ulong, List<Discord.Rest.RestUserMessage>>();
    public List<string> qas = new List<string>();
    private bool RoomMode = true;

    public int TimeDelay { get; private set; } = 10;
    public bool NeedsKey;

    public async Task OnMessage(SocketMessage arg) => OnMessage(arg, false);

    public bool HasUsername(string str)
    {
        if (curRoom == null)
            return false;

        foreach(var u in curRoom.Users)
        {
            if(str.Contains(u.Key.ToLower(),StringComparison.InvariantCultureIgnoreCase)||
                str.Contains(u.Value.ToLower(),StringComparison.InvariantCultureIgnoreCase))
            {
                return true; // Username found in message!
            }
        }
        return false;
        
    }
    public SocketMessage curMsg;
    public Room curRoom;

    public async Task OnMessage(SocketMessage arg, bool edit = false)
    {
        curMsg = arg;
        var botName = arg.Discord.CurrentUser.Username;
    
            var guild = _client.GetGuild((arg.Channel as SocketGuildChannel)?.Guild?.Id ?? 0);
        var channelName = arg.Channel.Name ?? arg.Channel.Id.ToString();
   
        var room = Rooms.GetOrCreate(channelName ?? "Group", new Room()
        {
            handler = new MessageHandler(null, null) { Channel = arg.Channel.Id.ToString(), BotName = botName }
        });
        
        curRoom = room;
        var isForBot = ForBot();
      


        //    if(d++ == 0)
        //    Invite("midjourney");

        if (arg.Content == "dibbr dump questions")
        {
            var str = "Here's the questions I thought I should answer and the responses I gave:\n";
            foreach (var qa in qas)
                str += qa + "\n";
            await arg.Channel.SendMessageAsync(str);

        }


        if (arg.Content == "dibbr stablediffuse")
        {
            var str = await Gen();
            arg.Channel.SendMessageAsync(str);
        }

        #region Delete
        if (arg.Content.Contains($"{botName} delete", StringComparison.InvariantCultureIgnoreCase))
        {

          //  if (!(arg is SocketUserMessage message)) return;// Task.CompletedTask;
         //   if (message.Source != MessageSource.User) return;// Task.CompletedTask;
            // if (LastMessage.TryGetValue(message.Channel.Id, out var restMsg))
           // {
                var msg = arg.Content;
                var m = msg.After("delete").Trim();
            int n = 1;
            if (m.Length == 0)
                ;// n = "1";
            else if (m == "all")
                n = 20;
            else n = Int32.Parse(m);

             

            var botMessages = Messages.Where(m => m.Value.Author.Username == botName && m.Value.Channel.Id == arg.Channel.Id).Select(d => d.Value.Id).Reverse().Take(n).ToList();

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
        if (dup > 10 && arg.Content.Length > 0)
        {
            var val = arg.Content;
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
                    File.WriteAllText("BlackBoard.Txt", JsonConvert.SerializeObject(BlackBoard));
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



       
        if (arg.Content.StartsWith("("))
            return;

        var userName = Regex.Replace(arg.Author.Username, @"[^\u0000-\u007F]+", string.Empty);
        if (userName.Length == 0)
            userName = "Stupid Name";

        room.Users.TryAdd(userName, arg.Author.Username);
        // #1 add to log
        // Always add to log
        if (room.handler.AIMode)
            userName = arg.Author.IsBot ? "AI" : "Human";
        // Rename if we renamed bot (e.g dibbr -> Trump)
        if (userName == arg.Discord.CurrentUser.Username)
            userName = room.handler.BotName;

        User user = Users.GetOrCreate(arg.Author.Id, new User() { Name = userName });

        string content = Regex.Replace(arg.Content, @" \[(.*?)\]", string.Empty);
        var q = $"{userName}: {content}'";
        await Program.Log("chat_log_" + channelName + ".txt", $"{q}");

        user.Log.Add(q);
        room.Log.Add(q);


       // room.thread = new Thread(async delegate ()
      //  {
      async Task RunLogic()
        {
            if (!(arg is SocketUserMessage message))
            {
                
                Console.WriteLine("Not a SocketUserMessage");
                if (arg.Type == MessageType.GuildMemberJoin && guild?.Name.Contains("dibbr", StringComparison.InvariantCultureIgnoreCase) == true)
                {
                    if (arg.Author.Username == botName)
                        await Send($"Hello {arg.Channel.Name}! Type e.g. {botName} list 10 flavors of ice cream to start talking to me");
                    else await Send($"Welcome to {guild.Name} {arg.Author.Username}");
                }
                else if(arg.Type == MessageType.ChannelPinnedMessage)
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
               // return;// Task.CompletedTask;
                       //     if (message.Source != MessageSource.User)
                       //           return;// Task.CompletedTask;





            #region APIKeys
       

            #endregion

            bool isBot = arg.Author.Username == arg.Discord.CurrentUser.Username || arg.Author.IsBot;
            if (isBot)
                room.LastBotMessage = Now;

            botName = room.handler.BotName;

            isForBot = ForBot();

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

            if (arg.Discord.CurrentUser.Username == botName)
                isForBot |= channelName.StartsWith("@");

            if (guild?.Name.HasAny("mrgirl", "?????") ?? false || channelName.HasAny("uncivil", "incel abode"))
            {
                if (!channelName.HasAny("voice"))
                    room.Disabled = true;
            }



            // Random 1% chance bot responds
            var randChat = (new Random().Next(100)) > 93;
            if (randChat && content.Length > "I wonder if this is along enough message?".Length && content.HasAny("?", "how", "why", "what", "when", "where", "anyone", "think", "can", "should"))
            {
                isForBot = true;
                room.Disabled = false;
            }

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
                if (user.WarningCount++ == 0)
                {
                    var str = $"You have spent $$ on dibbr. For more credits, sign up for a free key at openai.com and DM to dibbr so he can use your own key for your queries. You are cut off for the day otherwise.";
                    await arg.Channel.SendMessageAsync(str);

                }
                //  else return;
                return;
            }


            var whiteList = arg.Author.Username.HasAny("Hawky", "dabbr");
            if (whiteList)
            {
                user.Queries = 0;
                user.TokensToday = 0;
            }

            if (channelName.StartsWith("@") && isForBot && !arg.Author.Username.Contains("bbr") && arg.Author.Username != botName && (user.Queries++ > 10))
            {
                await arg.Channel.SendMessageAsync("Free dibbr credits expired. Please get your own key for dibbr at openai.com (free to sign up, then choose api token) and paste it here to use your own credits! You will recieve $18 free credits, which will be allocated only for you.");
                return;
            }

            var lastAutoMsg = Math.Min(159, (Now - room.LastAutoMessage).TotalSeconds);
            var lastMsg = Math.Min(159, (Now - room.LastNonBotMessage).TotalSeconds);
            var lastBotMsg = Math.Min(160, (Now - room.LastBotMessage).TotalSeconds);
            var lastUserQuery = Math.Min(161, (Now - user.LastBotQuery).TotalSeconds);
            var lastBotQuery = Math.Min(161, (Now - room.LastBotQuery).TotalSeconds);
            var wasAutoMessage = false;

            if (arg.Content.StartsWith("time_delay"))
            {
                TimeDelay = Int32.Parse(arg.Content.After("time_delay").Trim());
                await arg.Channel.SendMessageAsync($"Time Delay is {TimeDelay}");
                return;
            }
            // Bots only talk every 30 secs
            if (channelName.HasAny("dibbr-vs-xev") && (arg.Author.Username == "dibbr" || arg.Author.Username == "xev"))
            {
                var howLongSinceLast = DateTime.Now - room.LastBotQuery;
                // We only do chat every 30 secs.
                if (howLongSinceLast.TotalSeconds < 30)
                    await Task.Delay(30000 - (int)howLongSinceLast.TotalMilliseconds);


            }
            else
            {
    
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
            if (lastAutoMsg > 1 && !isForBot && !isBot && (lastUserQuery < timeWindow) && !(guild?.Name.Contains("mrgirl") ?? false))
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
                if (channelName.HasAny("schitzo", "psychedelic", "general-chat"))
                {
                    room.handler.UseNeo = false;
                    // if (channelName.Contains("general"))
                    //     room.handler.Gpt3.engine = "text-davinci-002";
                    // else room.handler.Gpt3.engine = "text-davinci-001";
                    if (channelName.Contains("schitzo"))
                        room.handler.Gpt3.engine = "davinci-instruct-beta";
                    // room.handler.PrimeText = "dibbr is a leading researcher in spirituality, DMT, spirits and the deep state government. he finds and suggests conspiracy theories that are probably true. someone stole dibbr's nuts, and he suspects the deep state or one of the chatters. If dibbr is asked a nonsense question, dibbr will ask a sarcastic question back.";
                }


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

                if (room.handler.Gpt3._token == null || room.handler.Gpt3.neoToken == null)
                {
                    if (content.Rem(botName).Trim().StartsWith("activate"))
                    {
                        var key = content.After("activate").Trim();
                        if (key.StartsWith("sk"))
                        {
                            room.handler.Gpt3._token = key;
                            AddKey(guild?.Name ?? arg.Author.Username, key);
                     //       NeedsKey = false;
                            await arg.Channel.SendMessageAsync("Activated GPT3!");
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
                    if (room.handler.Gpt3._token == null && room.handler.Gpt3.neoToken == null)
                    {
                        await arg.Channel.SendMessageAsync("I need a key to activate. Go to openai.com, sign up freely, and then type <botname> activate <key>. OpenAI keys begin with sk-. You can also use a key from nlpcloud.com for 'neo' version, which is cool, sign up for free pay as you go plan 100k free tokens, and do same steps");
                        return;
                    }
                }


                if (NeedsKey)
                {
                    var val = BlackBoard.TryGetValue(guild?.Name ?? arg.Author.Username, out var key);
                    
                    room.handler.Gpt3._token = key;

                    val = BlackBoard.TryGetValue((guild?.Name ?? arg.Author.Username) + "Neo", out key);
                    room.handler.Gpt3.neoToken = "Token " + key;
                }


                var oldLog = user.Log;
                var oldRoom = room.Log;
                var refMsg = Messages.TryGetValue(message.Reference?.MessageId.Value ?? 0, out var msg);
                if (refMsg)// && (DateTime.Now - msg.Timestamp).TotalMinutes > 2)
                           // if (Message.TryGetValue(message.Reference?.MessageId.Value ?? 0, out string context) && context != null && context != "")
                {

                    if(user.Log.Count > 1)
                    user.Log.Insert(user.Log.Count - 1,">"+ MessageAuthors[message.Reference.MessageId.ToString()] + ": " + msg.Content);
                    room.Log.Insert(room.Log.Count - 1, ">" + MessageAuthors[message.Reference.MessageId.ToString()] + ": " + msg.Content);
                }



                if (RoomMode)
                    room.handler.Log = room.Log;
                else
                    room.handler.Log = user.Log;
                room.handler.BotName = botName;
                if (arg.Discord.CurrentUser.Username != "dibbr" && content.Contains("dibbr"))
                    return;
                using (arg.Channel.EnterTypingState())
                {
                    try
                    {
                        room.handler.Client = this;
                        (bReply, str) = await room.handler.OnMessage(content, userName, wasAutoMessage, true);
                       // botName = room.handler.BotName;
                    }
                    catch (Exception e) { str = "Exception " + e.Message; }// _callback(content, arg.Author.Username, isReply);
                }




                if (room.handler.Log.Count == 0)
                {
                    user.Log.Clear();
                    room.Log.Clear();
                }
                else
                {
                    if (refMsg)
                    {
                        void FixLog(List<string> log)
                        {
                            for (int x = log.Count >=2?log.Count - 2:log.Count-1; x < log.Count; x++)
                                if (log[x].StartsWith(">"))
                                    log.RemoveAt(x);
                        }
                        FixLog(room.Log);
                        FixLog(user.Log);
                        //             if(user)
                       // user.Log = oldLog;
                        //         room.Log.RemoveAt(room.Log.Count - 1);
                        //       user.Log.RemoveAt(user.Log.Count - 1);
                    }

                }

                // This was a command - let's not log it
                if (!bReply)
                {
                    //       if(room.Log)
                    //         room.Log.Remove(room.Log.Last());
                    //       user.Log.Remove(user.Log.Last());
                }

              

                if (str == null)
                    str = "";

                user.TokensToday += str.Length / 4;

                if (qas.Count > 0 && qas.Last().EndsWith(":"))
                    qas[qas.Count - 1] = qas.Last() + $"\"{str}\"";

                if (randChat && (str.Lame() || str.HasAny("[", ":", "error")))
                    return;

                if (str.Length == 0)
                    return;


                /* if (cost > 10 && (user.WarningCount++ % 40) == 0)
                 {
                     File.WriteAllText(arg.Author.Username.MakeValidFileName() + ".txt", user.TokensToday.ToString());
                     str = "[Warning: You have spent over $ " + Math.Round((user.TokensToday / 4) * 0.00002f, 4) + $" all by yourself today on dibbr." + Gpt3.Stats() + "\n" + str;
                 }*/


                await Send(str, edit);
            }
            // var guild = _client.GetGuild((message.Channel as SocketGuildChannel).Guild.Id);
            // await message.Channel.SendMessageAsync($"{arg.Content} world!");

            async Task Send(string str, bool edit=false)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Sending: {(str.Length > 10 ? str[..10] : str)}...");

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
                        if (edit && LastMessage.TryGetValue(arg.Channel.Id, out var list) && list.Count > 0)
                        {
                            var restMessage = list.Last();
                            object modify = null;
                            await restMessage.ModifyAsync(msg => msg.Content = m + "");
                        }
                        else
                        {
                            var msgref = new MessageReference() { };
                            msgref.MessageId = arg.Id;
                            LastMessage.TryAdd(arg.Channel.Id, new List<Discord.Rest.RestUserMessage>());
                            LastMessage[arg.Channel.Id].Add(await arg.Channel.SendMessageAsync($"{m}", messageReference: msgref, isTTS:arg.Channel.Name.Contains("@")));
                        }

                    }
                    catch (Exception e) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(e.Message); }

                    await Task.Delay(1);
                }
            }
        }
       await RunLogic();
       // });
       // room.thread.Start();

        #region Helpers
        bool ForBot()
        {

            MessageAuthors[arg.Id.ToString()] = arg.Author.Username;
            Message[arg.Id] = arg.Content;
      //      if (arg.Content.HasAny("iddbr"," ibber","d i b b r"," dibb "," dbbr"," dibbs "))
        //        return true;
            if (arg.Content.Contains("find the way") || arg.Content.Contains("da wae"))
                return true;

            if (arg.Content.HasAny("does anyone", "can anyone", "can someone", "hey guys", "anyone?", "how do I", "where can I"))
            {
                qas.Add($"\"{arg.Content}\", \tI replied:");
                return true;
            }

            if (!(arg is SocketUserMessage message))
                return false;

            if (arg.Author.Username == botName)
                return false;


            foreach (var m in message.MentionedUsers)
                if (m.Username == botName)
                {
                    return true;
                }

          

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
            if (arg.Content.Contains("dibber", StringComparison.InvariantCultureIgnoreCase))
                return true;
            if (arg.Content.Contains("dibbs", StringComparison.InvariantCultureIgnoreCase))
                return true;

            var bName = botName.Before(" ", true);

            if (arg.Content.Contains(bName, StringComparison.InvariantCultureIgnoreCase))
            {

                var s = arg.Content.After(bName).TrimExtra();
                // sup dibbr
                if (s.Length == 0) return true; // just botname
                var b = arg.Content.Before(bName).TrimExtra();
                // dibbr, hey
                if(b.Length == 0 || b.CountOccurrencesOf(' ') <= 1) return true; // botname first
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
            return; // Already logged
        }


        var bg = ConsoleColor.Black;
        var fg = ConsoleColor.Gray;
        // If you want to log and notify something, do it here
        if (arg.Content.ToLower().Contains(NotifyKeyword))
        {
            bg = ConsoleColor.DarkBlue;
            fg = ConsoleColor.Red;
            new Thread(delegate ()
            {
              //  Console.Beep(369, 50);
            }).Start();
        }
        if (arg.Content.Contains("@everyone"))

            Console.WriteLine(guild?.Name);
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

