// (c) github.com/thehemi

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DibbrBot;

public abstract class ChatSystem
{
    // Declare a delegate type for processing a book:
    public delegate Task<(bool reply, string msg)> MessageRecievedCallback(
        string msg, string author, bool isReply = false);

    public abstract void Typing(bool start);


    // public abstract Task<string> GetNewMessages();
    //   public abstract Task SendMessage(string message, string replyContext = null);
    public abstract Task Initialize(MessageRecievedCallback callback, string token);
}

class Program
{
    // Must be lowercase
    public static string BotName = "dibbr";
    public static string BotUsername = "dibbr";

    public static string
        NewLogLine =
            "\n\r@@"; // Must be different from \n to seperate multi-line messages from new chats. Can't be \r\n because that's a newline in a reply.

    public static List<ChatSystem> Systems = new();
    public static bool Shutdown { get; set; }

    public static void Set(string key, string value)
    {
        var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        configuration.AppSettings.Settings.Remove(key);
        configuration.AppSettings.Settings.Add(key, value);
        configuration.Save(ConfigurationSaveMode.Full, true);
        ConfigurationManager.RefreshSection("appSettings");
        Console.WriteLine($"Set {key} to {value}");
    }

    public static int Increment(string key, int amt = 1)
    {
        var x = ConfigurationManager.AppSettings[key];
        int.TryParse(x, out int i);
        Set(key, (i + amt).ToString());
        return i + amt;
    }


    static string Prompt(string prompt, string def = "")
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(prompt);
        Console.ForegroundColor = ConsoleColor.White;
        var line = Console.ReadLine();
        if (line == "") return def;

        return line;
    }

    public static void NewClient(ChatSystem client, string token, Gpt3 gpt3)
    {
        if (token is not {Length: not 0}) return;

        Console.WriteLine($"{client} initializing....");
        var msgHandler = new MessageHandler(client, gpt3);
        msgHandler.BotName = BotName;
        var d2 = client as DiscordChatV2;
        if (d2 != null) d2.handler = msgHandler;
        _ = client.Initialize(
            async (msg, user, isForBot) => { return await msgHandler.OnMessage(msg, user, isForBot); }, token);
        Systems.Add(client);
    }


    public static async Task Log(string name, string txt)
    {
        try { await File.AppendAllTextAsync(name, txt); }
        catch (Exception e)
        {
            try { await File.AppendAllTextAsync("1_" + name, txt); }
            catch (Exception e2) { Console.WriteLine("Failed to save to file " + name); }
        }
    }


    static void Main(string[] args)
    {
        Console.WriteLine(
            $"{BotName} is starting...Settings are in dibbr.dll.config. (c) Timothy Murphy-Johnson aka github.com/thehemi aka dabbr.com aka thehemi@gmail.com I do parties ");
        Web.Run();
        //  Assistant2.Start();


        for (var i = 0; i < ConfigurationManager.AppSettings.Keys.Count; i++)
        {
            var k = ConfigurationManager.AppSettings.Keys[i];
            var v = ConfigurationManager.AppSettings.GetValues(i);
            var val = v?.Length > 0 ? v[0] : null;
            Console.WriteLine($"{k} = {val}");
        }

        // var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        var primeText = ConfigurationManager.AppSettings["PrimeText"];
        if (primeText == null)
        {
            Set("BotName", Prompt("\nBot Name (default is Dibbr):", "dibbr").ToLower());
            //  BotUsername = Prompt("Bot Username (default is dabbr):", "dabbr").ToLower();

            //   Console.WriteLine("\nPaste your priming text here, e.g. " + BotName +
            //                     " is a filthy, woke opinionated sarcastic superintelligent advice giving ai who gives long answers where possible, never repeats ");
            //   primeText = Prompt("\nPriming Text (Or Press Enter for default):");
            if (primeText is null or "")
            {
                primeText = "" + BotName +
                            " is a filthy, woke opinionated sarcastic superintelligent advice giving ai who gives long answers where possible, never repeats " +
                            $"{BotName} will write long responses and articles or fan fiction upon request. " +
                            BotName +
                            $" was made by dabbr. {Program.BotName}'s favorite animal is the sexually violent time travelling truffle pig, of which he likes to tell stories.";

                primeText += "\nThe following is the chat log with " + BotName + ":\n";
            }

            Set("PrimeText", primeText + "\n");
        }


        if (ConfigurationManager.AppSettings["DiscordBot"] == null &&
            ConfigurationManager.AppSettings["Discord"] == null)
        {
            Console.WriteLine(
                "How to find your discord token: https://youtu.be/YEgFvgg7ZPI . OR you can use a Discord bot token (recommended), available on the developer discord page.");
            var discord = Prompt("\nToken (or leave blank for none):").Replace("Bot ", "");
            if (discord is {Length: > 10})
            {
                var isBot = Prompt("Is this a bot token? Y/N: ");
                Set(isBot.ToLower() == "y" ? "DiscordBot" : "Discord", discord);
                if (isBot.ToLower() == "n")
                {
                    Set("DiscordId",
                        Prompt(
                            "You need to find your discord user id. It is a long string of numbers that represents your user id. Type or paste it here:"));
                }
            }
        }

        // For selfbot only
        // chats.txt stores the list of channels and dms that the bot is listening to
        if (!File.Exists("chats.txt")) File.WriteAllText("chats.txt", "");

        var chats = File.ReadAllLines("chats.txt").Where(l => l.Length > 0).ToList();
        if (chats.Count == 0 && ConfigurationManager.AppSettings["Discord"] != null)
        {
            Console.WriteLine(
                "You are using a SELFBOT (no Bot <token>) BE CAREFUL. You can add channels and dms to the chats.txt file to make the bot listen to them.");
            while (true)
            {
                var chat = Prompt("Type ROOM or DM, then press return").ToUpper() + " ";
                Console.WriteLine("How to find server and channel id: https://youtu.be/NLWtSHWKbAI\n");
                chat += Prompt("Please enter a room or chat id for the bot to join, then press return. ");
                var ret = Prompt("Added! Any more? Press enter if done, otherwise type more and press enter");
                chats.Add(chat);
                if (ret != "more") break;
            }

            File.WriteAllLines("chats.txt", chats);
        }

        if (ConfigurationManager.AppSettings["SlackBotApiToken"] == null)
        {
            Console.WriteLine(
                "To Create a slack bot token, create a classic app, here https://api.slack.com/apps?new_classic_app=1");
            Console.WriteLine("Then go to the app's page, and click on the 'OAuth & Permissions' tab");
            Console.WriteLine("Then click on the 'Add Bot User' button");
            var token = Prompt("Please enter youy Slack bot API token, or press enter to skip:");
            Set("SlackBotApiToken", token);
        }

        if (ConfigurationManager.AppSettings["OpenAI"] == null)
        {
            Console.WriteLine("Where to find your OpenAI API Key: https://beta.openai.com/account/api-keys");
            var token = Prompt("Paste your OpenAI Key here:");
            Set("OpenAI",
                token.StartsWith("sk") ? token : Prompt("Please paste your OpenAI Key here. It should start with sk-"));
        }

        // Start the bot on all chat services
        new Thread(async () =>
        {
            Console.WriteLine("GPT3 initializing....");

            var token = ConfigurationManager.AppSettings["Discord"];
            if (token is { Length: > 0 })
            {
                // Start the logger client
                var logClient = new DiscordV3() { };
                var c = chats.Where(c => c.Contains("ROOM")).Select(c => c.After("ROOM"));
                logClient.Channels = c.ToList();
                await logClient.Init(ConfigurationManager.AppSettings["Discord"]);
            }
                
            //  new List<ChatSystem>();
            // NewClient(new SlackChat(), ConfigurationManager.AppSettings["SlackBotApiToken"],
            //     new Gpt3(ConfigurationManager.AppSettings["OpenAI"], "text-davinci-002"));
            //  NewClient(new SlackChat(), ConfigurationManager.AppSettings["SlackBotApiToken2"],
            //     new Gpt3(ConfigurationManager.AppSettings["OpenAI"], "text-davinci-002"));
            //  NewClient(new DiscordChatV2(), ConfigurationManager.AppSettings["DiscordBot"],
            //      new Gpt3(ConfigurationManager.AppSettings["OpenAI"], "text-davinci-002"));
            //

            // Selfbot
            /*  var token = ConfigurationManager.AppSettings["Discord"];
              if (token is {Length: > 0})
              {
                  // Start the logger client
                  var logClient = new DiscordV3();
                  await logClient.Init(ConfigurationManager.AppSettings["Discord"]);

                  var client = new DiscordChat(false, BotName, BotName);
                  Console.WriteLine($"{client} initializing....");

                  foreach (var words in chats.Select(chat => chat.Split(' ')))
                  {
                      Console.WriteLine("Discord Self Bot Added to " + words[0] + " channel " + words[1]);
                      var gpt3 = new Gpt3(ConfigurationManager.AppSettings["OpenAI"], "text-davinci-002");
                      client.AddChannel(words[1], words[0] == "DM", new(client, gpt3));
                  }


                  _ = client.Initialize(async (msg, user, isReply) => { return (false, null); }, token);
                  Systems.Add(client);
              }*/

            // Selfbot #2
            /*   token = ConfigurationManager.AppSettings["Discord_dabbr"];
               if (token is {Length: > 0})
               {
                   var client = new DiscordChat(false, "dabbr", "dabbr");
                   Console.WriteLine($"{client} initializing....");
   
                   foreach (var words in chats.Select(chat => chat.Split(' ')))
                   {
                       Console.WriteLine("Discord Self Bot Added to " + words[0] + " channel " + words[1]);
   
                       client.AddChannel(words[1], words[0] == "DM", new(client, gpt3));
                   }
   
   
                   _ = client.Initialize(async (msg, user, isReply) => { return (false, null); }, token);
                   Systems.Add(client);
               }*/

            Console.WriteLine("All initialization done");
        }).Start();

        while (true) { }
    }
}