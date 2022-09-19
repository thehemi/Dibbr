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

public class DiscordV3
{
    public bool NeedsKey;
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

    }

    public class Room
    {
        public List<string> Log = new List<string>();
        int Spoke;
        public DateTime LastNonBotMessage, LastBotMessage, LastBotQuery, LastAutoMessage = DateTime.MinValue;
        public MessageHandler handler;
        public bool Disabled;
    }

    Dictionary<string, Room> Rooms = new Dictionary<string, Room>();
    Dictionary<ulong, User> Users = new Dictionary<ulong, User>();
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
        Token = token;
        var config = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            MessageCacheSize = 1000,
        };
        _client = new DiscordSocketClient(config);
        _client.Log += DoLog;
        _client.MessageReceived += OnMessage;
        //_client.MessageDeleted += OnDelete;
        _client.MessageUpdated += OnEdit;

        // Some alternative options would be to keep your token in an Environment Variable or json config
        // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
        //if (token == null)
          //  token = File.ReadAllText("token.txt");
        await _client.LoginAsync(bot?TokenType.Bot:TokenType.User, token);
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
            Log($"logs/{arg.Id}_{arg.Channel.Name}_Deletes.log", $"{DateTime.Now}#{arg.Author.Username}:{arg.Content}\n");
        else
            Log($"logs/{arg2.Id}_{arg2.Name}_Deletes.log", $"{DateTime.Now}#{arg1.Id}\n");


    }
    Dictionary<string, string> MessageAuthors = new Dictionary<string, string>();
    Dictionary<ulong, string> Message = new Dictionary<ulong, string>();
    static int d = 0;
    //Dictionary<ulong, int> Spoke = new Dictionary<ulong, int>();
    Dictionary<ulong, List<Discord.Rest.RestUserMessage>> LastMessage = new Dictionary<ulong, List<Discord.Rest.RestUserMessage>>();
    public List<string> qas = new List<string>();
    public async Task OnMessage(SocketMessage arg) => OnMessage(arg, false);

    public async Task OnMessage(SocketMessage arg, bool edit = false)
    {
        var botName = arg.Discord.CurrentUser.Username;
        var guild = _client.GetGuild((arg.Channel as SocketGuildChannel)?.Guild?.Id ?? 0);


        var isForBot = ForBot();
        if (edit && !isForBot)
            return;
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

            if (!(arg is SocketUserMessage message)) return;// Task.CompletedTask;
            if (message.Source != MessageSource.User) return;// Task.CompletedTask;

            if (LastMessage.TryGetValue(message.Channel.Id, out var restMsg))
            {
                var msg = arg.Content;
                var m = msg.After("delete").Trim();
                if (m.Length == 0)
                    m = "1";
                if (m == "all")
                    m = restMsg.Count.ToString();

                var n = Int32.Parse(m);
                for (int i = 0; i < n; i++)
                    restMsg[restMsg.Count - (i + 1)]?.DeleteAsync().Wait();
            }
            return;
        }
        #endregion

        #region Spam message filter
        var hash = arg.Content.Sub(0, 50) + arg.Channel.Name + arg.Author.Username;
        var dup = Content.Inc(hash);
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


        new Thread(async delegate ()
        {
            if (!(arg is SocketUserMessage message)) return;// Task.CompletedTask;
            if (message.Source != MessageSource.User) return;// Task.CompletedTask;

            if (arg.Content.StartsWith("("))
                return;

            var userName = Regex.Replace(arg.Author.Username, @"[^\u0000-\u007F]+", string.Empty);



            var room = Rooms.GetOrCreate(arg.Channel.Name, new Room()
            {
                handler = new MessageHandler(null, null) { Channel = arg.Channel.Id.ToString(), BotName = botName }
            });
            User user = Users.GetOrCreate(arg.Author.Id, new User() { Name = arg.Author.Username });

            string content = message.Content;
            if (content.Contains("<@") && content.Contains(">"))
                content = content.Replace(content.After("<").Before(">"), "");

          

            // Remove debug code [square brackets]
            content = Regex.Replace(content, @" \[(.*?)\]", string.Empty);
            // #1 add to log
            // Always add to log
            var q = $"{userName}: {content}";
            await Program.Log("chat_log_" + arg.Channel.Name + ".txt", $"{q}");
        
        if (!q.StartsWith("["))
            room.Log.Add(q);

            var oldLog = room.Log;
            if (Message.TryGetValue(message.Reference?.MessageId.Value ?? 0, out string context) && context != null && context != "")
            {
                room.Log.Add(MessageAuthors[message.Reference.MessageId.ToString()] + ": " + context);
            }

            #region APIKeys
            /*   if (arg.Content.Contains("sk-"))
               {
                   var key = arg.Content.Substring(arg.Content.IndexOf("sk-"));
                   var idx = key.IndexOf(" ");
                   if (idx == -1) idx = key.Length;
                   key = key.Substring(0, idx);

                 //  Program.Set("OpenAI_OLD", Gpt3._token);
                 //  Program.Set("OpenAI", key);
               //    Gpt3._token = key;
                   //_api = new(_token, new Engine() { Owner = "openai", Ready = true, EngineName = "text-davinci-002" });
                   user.Queries = -1000;
                   await arg.Channel.SendMessageAsync("Thank you for the key, " + arg.Author.Username + ", I have applied it. You get unlimited queries now.");
                   return;

               }*/

            #endregion

            bool isBot = arg.Author.Username == arg.Discord.CurrentUser.Username;
            if (isBot)
                room.LastBotMessage = Now;

            if (arg.Discord.CurrentUser.Username == botName)
                isForBot |= message.Channel.Name.StartsWith("@");

            if(arg.Channel.Name.Contains("uncivil",StringComparison.InvariantCultureIgnoreCase))
            {
                room.Disabled = true;
            }
            if (room.Disabled) return;

            if (guild != null)
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
                    return;
                }
                else return;
            }

            if (message.Channel.Name.StartsWith("@") && isForBot && !arg.Author.Username.Contains("bbr") && arg.Author.Username != botName && (user.Queries++ > 10))
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
            // This needs to be here
           // if(message.MentionedUsers?.Count == 0 && message.Reference)
            if (lastAutoMsg > 1 && !isForBot && !isBot && (lastUserQuery < 100 && lastBotQuery < 30))
            {
                var mentionsAnyone = MessageAuthors.TryGetValue(message.Reference?.MessageId.ToString() ?? "null", out string name);

               // If message references anyone else, ignore it
                if (message.Reference?.MessageId.IsSpecified ?? false || message.MentionedUsers.Count > 0)
                    return;

                if (content.Length < 10 && content.HasAny("stop talking"," he ","this bot", "can someone silence", "not talking to you", "go away", "shut up"))
                {
                    if (!content.HasAny("yes", "yeah", "no", "nope", "go on", "continue", "please","why","more"))
                    {
                        // room.LastBotQuery = DateTime.MinValue;
                        user.LastBotQuery = DateTime.MinValue;
                        room.LastBotMessage = DateTime.MinValue;
                        return;
                    }
                }
                var diff = Math.Abs(lastBotMsg - lastUserQuery);
                var d2 = Math.Abs(lastBotMsg - lastBotQuery);
                if (d2 > 5)
                {
                    Console.WriteLine("TEST");
                }


                if (lastBotQuery < 100)
                {
                    var toHim = content.HasAny("will you", "can you", "do you", "have you", "are you", "did you", "does your");
                    if (toHim)
                    {
                        isForBot = true;
                        wasAutoMessage = true;
                        room.LastAutoMessage = Now;
                    }

                }

                // If the last user query was more recent than any message
                if ((lastBotQuery < lastMsg / 2) || (lastBotQuery < 30 && lastUserQuery < 100))
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





            if (!isForBot)
                return;
            if (isBot)
                return;

            //using (arg.Channel.EnterTypingState())
            {
                bool isReply = true;

                // arg.Discord.UserIsTyping += Discord_UserIsTyping;
               // if (arg.Channel.Name.Contains("gpt3"))// || arg.Channel.Id == 1008443933006770186)// || (arg.Channel.Id == 1009524782229901444 && !content.Contains("story")))
               //     room.handler.UseNeo = false;
                if (arg.Channel.Name.HasAny("schitzo", "psychedelic", "general-chat"))
                {
                    room.handler.UseNeo = false;
                    if (arg.Channel.Name.Contains("general"))
                        room.handler.Gpt3.engine = "text-davinci-002";
                    else room.handler.Gpt3.engine = "text-davinci-001";
                    if (arg.Channel.Name.Contains("schitzo"))
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
                var typing = arg.Channel.EnterTypingState();

              //  if (user.TokensToday == 0 && File.Exists(arg.Author.Username.MakeValidFileName()))
              //      user.TokensToday = Int32.Parse(File.ReadAllText(arg.Author.Username.MakeValidFileName()));
               // if (!room.handler.UseNeo && !content.Contains("$$"))
                    user.TokensToday += content.Length + string.Join('\n', room.Log).Length + room.handler.PrimeText?.Length ?? 0;
           

                if(NeedsKey || room.handler.Gpt3._token == null)
                {
                    if(content.Contains("activate"))
                    {
                        var key = content.After("activate").Trim();
                        if(key.StartsWith("sk"))
                        {
                            room.handler.Gpt3._token = key;
                            NeedsKey = false;
                            await arg.Channel.SendMessageAsync("Activated GPT3!");
                        }
                        else
                        {
                            room.handler.Gpt3.neoToken = key;
                            await arg.Channel.SendMessageAsync("Activated NEO!");
                        }
                        return;
                    }
                }

                room.handler.Log = room.Log;
                try
                {
                    (bReply, str) = await room.handler.OnMessage(content, userName, wasAutoMessage, true);
                }
                catch (Exception e) { str = "Exception " + e.Message; }// _callback(content, arg.Author.Username, isReply);
                if (room.handler.Log.Count == 0)
                    room.Log.Clear();
                else room.Log = oldLog;

                var a = $"{room.handler.BotName}): {str ?? ""}{Program.NewLogLine}";
                typing.Dispose();
                if (str == null)
                    str = "";

                user.TokensToday += str.Length / 4;

                if (qas.Count > 0 && qas.Last().EndsWith(":"))
                    qas[qas.Count - 1] = qas.Last() + $"\"{str}\"";

                //  typing.Dispose();
                if (str is null or "")
                {
                    return;
                }




               /* if (cost > 10 && (user.WarningCount++ % 40) == 0)
                {
                    File.WriteAllText(arg.Author.Username.MakeValidFileName() + ".txt", user.TokensToday.ToString());
                    str = "[Warning: You have spent over $ " + Math.Round((user.TokensToday / 4) * 0.00002f, 4) + $" all by yourself today on dibbr." + Gpt3.Stats() + "\n" + str;
                }*/


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
                            LastMessage.TryAdd(message.Channel.Id, new List<Discord.Rest.RestUserMessage>());
                            LastMessage[message.Channel.Id].Add(await message.Channel.SendMessageAsync($"{m}",messageReference:msgref));
                        }

                    }
                    catch (Exception e) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(e.Message); }

                    await Task.Delay(1);
                }
            }

        }).Start();

        #region Helpers
        bool ForBot()
        {

            MessageAuthors[arg.Id.ToString()] = arg.Author.Username;
            Message[arg.Id] = arg.Content;
            if (arg.Content.HasAny("iddbr"," ibber","d i b b r"," dibb "," dbbr"," dibbs "))
                return true;
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
            // var mentions = name == botName;
            if (name == botName)
                return true;
            // Mentionining someone else
            if ((message.MentionedUsers?.Count > 0 || mentionsAnyone) && !message.Content.Contains(botName))
                return false;

            if (arg.Content.Contains(botName, StringComparison.InvariantCultureIgnoreCase))
            {
                var s = arg.Content.After(botName);
                var idx = s.IndexOf(" ");
                if (idx == -1) idx = s.Length;
                var word = s.Substring(0, idx);
                //This is a hacky way to catch people talking about dibbr
                // dibbr will be useful - won't trigger
                // dibbr will there be an update - will trigger
                if (arg.Content.HasAny($"the {botName}"))
                    return false;
                if (word.HasAny("has", "needs", "should", "can't", "doesn't", "didn't", "won't", " sis ", " or "," in ","like"))
                {
                    var next = s.After(word).Trim();
                    if (!next.HasAny("you", "there"))
                    {
                        return false;
                    }

                }
                return true;
            }


            return false;


        }

        #endregion





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
        Log($"logs/{arg.Author.Username.MakeValidFileName()}#{arg.Author.Discriminator}.log", $"{DateTime.Now}#{arg.Id}_{arg.Channel.Name}:{arg.Content}\n");

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
        file = file.MakeValidFileName();
        File.AppendAllText(file, txt);
    }

    Task DoLog(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

}

