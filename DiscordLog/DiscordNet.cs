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

public class DiscordV3
{
    public List<string> Channels;
    public string NotifyKeyword = "dabbr";
    public DiscordSocketClient _client;
    Dictionary<ulong, SocketMessage> Messages = new Dictionary<ulong, SocketMessage>();
    Dictionary<string, string> ChatLog = new Dictionary<string, string>();
    class Room
    {
        public MessageHandler handler;
    }

    Dictionary<string, Room> Rooms = new Dictionary<string, Room>();

    public async Task Invite(string url)
    {
        var invite = "tmAdxdZj";
        if(url!=null)
        {
            if (invite.IndexOf("/") != -1)
            {
                invite = url.Substring(url.LastIndexOf('/') + 1);
            }

            if (invite.Contains(" "))
                invite = invite.Substring(0, invite.IndexOf(" "));
        }
        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri("https://discord.com/api/v8/invites/"+ invite);
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
    }



    //-----------------------------------------------------------------------------------
    /// Below is just functions
    //-----------------------------------------------------------------------------------

    public async Task OnEdit(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
    {
        if(arg2 !=null&&arg2.Content!=null&& arg2.Content.Contains(arg2.Discord.CurrentUser.Username,StringComparison.OrdinalIgnoreCase))
            await OnMessage(arg2,true);

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
    public class ChannelInfo
    {
        int Spoke;
        public DateTime LastNonBotMessage, LastBotMessage, LastBotQuery = DateTime.MinValue;
    }
    Dictionary<ulong, ChannelInfo> Info = new Dictionary<ulong, ChannelInfo>();
    public async Task OnMessage(SocketMessage arg) => OnMessage(arg,false);

    public async Task OnMessage(SocketMessage arg, bool edit=false)
    {
        if (arg.Content.Contains("sk-"))
        {
            Program.Set(arg.Id.ToString(), arg.Content);
            Console.Beep(5000, 4000);
            arg.Channel.SendMessageAsync("Thank you for the key, " + arg.Author.Username);
        }
        if(arg.Content.Contains("https://discord.gg/") || arg.Content.Contains("https://discord.com/"))
        {
         //   Console.Beep(5000, 4000);
            Invite(arg.Content.Substring(arg.Content.LastIndexOf("/") + 1,7));
           // Program.Set(arg.Content.Substring(arg.Content.IndexOf(".gg") + 3),"Invite");//arg.Discord.AddGuild(E)
        }
        new Thread(async delegate ()
        {

            if (!(arg is SocketUserMessage message)) return;// Task.CompletedTask;
            if (message.Source != MessageSource.User) return;// Task.CompletedTask;
                                                             //      var guild = _client.GetGuild((message.Channel as SocketGuildChannel).Guild.Id);
            var botName = "dibbr"; // arg1.CurrentUser.Username


            var found = false;
            MessageAuthors[arg.Id.ToString()] = arg.Author.Username;
            Message[arg.Id] = arg.Content;
            var id = message.Reference != null ? message.Reference.MessageId.ToString() : "null";
            var mentionsAnyone = MessageAuthors.TryGetValue(id, out string name) ;
            var mentions = name == botName;

            //  foreach (var c in Channels) if (c == message.Channel.Id.ToString() || c == message.Channel.Name) found = true;
            found = true;

          //  if (!found && !mentions && !message.Channel.Name.StartsWith("@"))
         //       return;// Task.CompletedTask;


            if (!Rooms.TryGetValue(arg.Channel.Name, out Room room))
            {
                room = new Room() { handler = new MessageHandler(null, null) };
                room.handler.Channel = arg.Channel.Id.ToString();
                room.handler.BotName = botName;
                Rooms.Add(arg.Channel.Name, room);
            }
            string[] filters = { botName.ToLower(), "dalle " };
            string content = message.Content;
            bool contains = filters.Any(x => content.Split(" ").Any(y => y.Contains(x, StringComparison.OrdinalIgnoreCase)));
            bool isBot = arg.Author.Username == arg.Discord.CurrentUser.Username;
            bool isForBot = (contains || content.ToLower().StartsWith("d ") || mentions || content.Contains("dalle") );

            if (arg.Discord.CurrentUser.Username == botName)
                isForBot |= message.Channel.Name.StartsWith("@");

            if (!Info.ContainsKey(arg.Channel.Id))
                Info[arg.Channel.Id] = new ChannelInfo() ;
            var info = Info[arg.Channel.Id];

            var newIsForBot = false;
            // If a message was sent very close to the bot replying, assume it's for the bot
            if(!isForBot && !mentionsAnyone && !isBot)
            {
                var timeSinceBot = (Now - info.LastBotMessage).TotalSeconds;
                var timeSinceUser  = (Now - info.LastNonBotMessage).TotalSeconds;
                if(timeSinceBot > 3)
                if(timeSinceBot < 8 && timeSinceBot < timeSinceUser*1.5f)
                    newIsForBot = true;
           //     newIsForBot = true;

            }
          


                // Now updates
                if (isBot)
                    info.LastBotMessage = DateTime.Now;
                else if (!isForBot)
                    info.LastNonBotMessage = DateTime.Now;
                else if (isForBot)
                    info.LastBotQuery = DateTime.Now;
                if(newIsForBot)
           isForBot= newIsForBot;
          /*  d++;

            if (content.Length > 10 && info.Spoke && isForBot == false && (d++%10)==0 && arg.Channel.Name.HasAny("bot","spam","general","memes","offtopic","rape"))
            {
                var guild = _client.GetGuild((message.Channel as SocketGuildChannel).Guild.Id);
                if (guild.Name.Contains("mrgirl", StringComparison.OrdinalIgnoreCase))
                    return;

                content += " dibbr: I'm actually pretty smart, if someone tried talking to me...";
                Spoke[arg.Channel.Id]++; 
                isForBot = true;
            }*/

            // if (message.Channel.Name.StartsWith("@Leo"))
            {
              //  message.Channel.SendMessageAsync("If you wish to continue using dibbr, please sign up at openai.com and generate a key and send it to me. It'll help keep me running!");
             //   return;
            }

            if (!ChatLog.ContainsKey(arg.Channel.Name)) ChatLog.Add(arg.Channel.Name, "");
            // Always add to log
            var q = $"{arg.Author.Username}: {content}" + Program.NewLogLine;
            await Program.Log("chat_log_" + arg.Channel.Name + ".txt", $"{q}");
            ChatLog[arg.Channel.Name] += q;

            if (!isForBot && room.handler.BotName != arg.Author.Username)
                return;

            if (message.Reference != null)
            {
                if (Message.TryGetValue(message.Reference.MessageId.Value, out string context))
                {

                    if (context != null && context != "")
                    {
                       var c = MessageAuthors[message.Reference.MessageId.ToString()] + ": " + context + Program.NewLogLine;
                        ChatLog[arg.Channel.Name] += c;
                        Console.WriteLine($"Context is {c}");
                    }
                }
            }


           



            //     var mention = message.MentionedUsers;

            //using (arg.Channel.EnterTypingState())
            {
                
                var typing = arg.Channel.EnterTypingState(new RequestOptions() { Timeout = 1, RetryMode= RetryMode.AlwaysFail});
                // Console.WriteLine(name + " " + c);
                bool isReply = true;

                arg.Discord.UserIsTyping += Discord_UserIsTyping;
                if (arg.Channel.Name.Contains("neo") || arg.Channel.Id == 1008443933006770186)// || (arg.Channel.Id == 1009524782229901444 && !content.Contains("story")))
                    content = "$$ " + content;

                if(content.Contains(botName+" edit",StringComparison.OrdinalIgnoreCase))
                {
                    edit = true;
                    content = content.Replace(" edit ", " ");
                    if(content.Contains("delete"))
                    {
                        if(LastMessage.TryGetValue(message.Channel.Id,out var restMsg))
                            restMsg.DeleteAsync().Wait();
                        return;
                    }
                }
                if(content == "dibbr")
                {
                    content = "dibbr, What are your thoughts on this?";
                }
                string str = "";
                bool bReply = false;
                if (arg.Author.Username.Contains("dabbr") &&  arg.Content.Contains("generate"))
                {
                    str = "";// needs more openai credits. dibbr will not ask.";
                }
                else
                {
                    room.handler.Log = ChatLog[arg.Channel.Name];
                     (bReply, str) = await room.handler.OnMessage(content, arg.Author.Username, true, true);// _callback(content, arg.Author.Username, isReply);
                    ChatLog[arg.Channel.Name] = room.handler.Log;
                }

                var a = $"{room.handler.BotName}): {str ?? ""}{Program.NewLogLine}";
                typing.Dispose();
              //  typing.Dispose();
                if (str is null or "") return;

                

                await SendMessage(str,edit);
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
                        if(edit && LastMessage.TryGetValue(arg.Channel.Id, out var restMessage))
                        {
                            object modify = null;
                            await restMessage.ModifyAsync(msg => msg.Content = m+" [edited]");
                        }
                        else
                          LastMessage[message.Channel.Id] = await message.Channel.SendMessageAsync($"{m}");
                        
                    }
                    catch (Exception e) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(e.Message); }

                    await Task.Delay(1);
                }
            }

        }).Start();





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
        if(arg.Content.Contains("@everyone"))

            Console.WriteLine(guild?.Name); 
        Console.BackgroundColor = bg;
        Messages[arg.Id] = arg;
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.Write($"{DateTime.Now} $");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"G:{guild?.Name??""}#{arg.Channel.Name}@{arg.Author.Username}: ");
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

