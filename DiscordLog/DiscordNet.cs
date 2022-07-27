using DibbrBot;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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


    public async Task Init(string token)
    {
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

    public async Task OnMessage(SocketMessage arg)
    {
        new Thread(async delegate () {

        if (!(arg is SocketUserMessage message)) return;// Task.CompletedTask;
        if (message.Source != MessageSource.User) return;// Task.CompletedTask;
  //      var guild = _client.GetGuild((message.Channel as SocketGuildChannel).Guild.Id);



        var found = false;
        MessageAuthors[arg.Id.ToString()] = arg.Author.Username;
            var id = message.Reference != null ? message.Reference.MessageId.ToString() : "null";
            var mentions = MessageAuthors.TryGetValue(id, out string name) && name == _client.CurrentUser.Username;

            foreach (var c in Channels) if (c == message.Channel.Id.ToString() || c == message.Channel.Name) found = true;
        if (!found && !mentions && !message.Channel.Name.StartsWith("@"))
            return;// Task.CompletedTask;


            if (!Rooms.TryGetValue(arg.Channel.Name, out Room room))
            {
                room = new Room() { handler = new MessageHandler(null, null) };
                room.handler.Channel = arg.Channel.Id.ToString();
                room.handler.BotName = _client.CurrentUser.Username;
                Rooms.Add(arg.Channel.Name, room);
            }
            string[] filters = { _client.CurrentUser.Username.ToLower(),"dalle " };
        string content = message.Content.ToLower();
        bool contains = filters.Any(x => content.Split(" ").Any(y => y.Contains(x)));
            //     var mention = message.MentionedUsers;
            if (contains || message.Content.ToLower().StartsWith("d ") || mentions || content.Contains("dalle") || message.Channel.Name.StartsWith("@"))
        {
            // Console.WriteLine(name + " " + c);
            bool isReply = true;

            content = Regex.Replace(content, "^d ", "", RegexOptions.IgnoreCase);
             content = Regex.Replace(content, "^dibbr", "", RegexOptions.IgnoreCase);

                if (!ChatLog.ContainsKey(arg.Channel.Name)) ChatLog.Add(arg.Channel.Name, "");
            var oldLog = ChatLog[arg.Channel.Name];
            var q = $"Q (from {arg.Author.Username}): {content}" + Program.NewLogLine; ;
            ChatLog[arg.Channel.Name] += q;

            //      if (ChatLog[name].Length > _maxBuffer) ChatLog[name] = ChatLog[name][^_maxBuffer..];

            room.handler.Log = ChatLog[arg.Channel.Name];
            var (bReply, str) = await room.handler.OnMessage(message.Content, arg.Author.Username, true, true);// _callback(content, arg.Author.Username, isReply);
            ChatLog[arg.Channel.Name] = room.handler.Log;

            var a = $"A (from {room.handler.BotName}): {str ?? ""}{Program.NewLogLine}";
            // Reasons to not keep this Q/A pair
            if (str is null or "")
            {
                ChatLog[arg.Channel.Name] = oldLog;
            } // Task.FromResult(Task.CompletedTask);
            else { ChatLog[arg.Channel.Name] += a; }

            if (str is null or "") return;

            await Program.Log("chat_log_" + arg.Channel.Name + ".txt", $"{q}{a}");

            await SendMessage(str);

                // var guild = _client.GetGuild((message.Channel as SocketGuildChannel).Guild.Id);
                // await message.Channel.SendMessageAsync($"{arg.Content} world!");
                async Task SendMessage(string str)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Sending: {str}");
                   
                    var msgs = new List<string>();
                    if (str.Length > 2000)
                    {
                        msgs.Add(str[..1999]);
                        msgs.Add(str[1999..]);
                    }
                    else
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

                            await message.Channel.SendMessageAsync($"{m}");
                        }
                        catch (Exception e) { Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine(e.Message); }

                        await Task.Delay(1);
                    }
                }
            }
    }).Start();


        



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

        Console.BackgroundColor = bg;
        Messages[arg.Id] = arg;
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.Write($"{DateTime.Now} ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"#{arg.Channel.Name}#{arg.Author.Username}: ");
        Console.ForegroundColor = fg;
        Console.Write($"{arg.Content}\n");
        Console.ForegroundColor = ConsoleColor.White;
        Console.BackgroundColor = ConsoleColor.Black;
        Log($"logs/{arg.Channel.Id}_{arg.Channel.Name}.log", $"{DateTime.Now}#{arg.Author.Username}:{arg.Content}\n");
        Log($"logs/{arg.Author.Username}#{arg.Author.Discriminator}.log", $"{DateTime.Now}#{arg.Id}_{arg.Channel.Name}:{arg.Content}\n");

        return ;
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

