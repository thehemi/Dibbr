// (c) github.com/thehemi
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DibbrBot
{
    public abstract class IChatSystem
    {
        public abstract void SetChatLog(string log);
        public abstract string GetChatLog();
        // Declare a delegate type for processing a book:
        public delegate Task<string> MessageRecievedCallback(string msg, string author);
        // public abstract Task<string> GetNewMessages();
        //   public abstract Task SendMessage(string message, string replyContext = null);
        public abstract Task Initialize(MessageRecievedCallback callback, string token);
    }

    class Program
    {
        public static string BotName = "dibbr";
        public static string BotUsername = "dabbr";
        public static List<IChatSystem> systems = new List<IChatSystem>();
        private static void Set(string key, string value)
        {
            Configuration configuration =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings.Remove(key);
            configuration.AppSettings.Settings.Add(key, value);
            configuration.Save(ConfigurationSaveMode.Full, true);
            ConfigurationManager.RefreshSection("appSettings");
        }


        static string Prompt(string prompt)
        {
            Console.ForegroundColor = ConsoleColor.Green;       
            Console.WriteLine(prompt);
            Console.ForegroundColor = ConsoleColor.White;
            return Console.ReadLine();
        }

        static void Main(string[] args)
        {
            Console.WriteLine("DibbrBot is starting...Settings are in App.config. (c) Timothy Murphy-Johnson aka github.com/thehemi aka dabbr.com aka thehemi@gmail.com I do parties ");
            Web.Run();
            
            // var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var primeText = ConfigurationManager.AppSettings["PrimeText"];
            if (primeText == null)
            {
                Console.WriteLine("");

                Console.WriteLine("Paste your priming text here, e.g. "+Program.BotName+" is a filthy, woke opinionated sarcastic superintelligent advice giving ai who gives long answers where possible, never repeats ");
                primeText = Prompt("\nPriming Text (Or Press Enter for default):");
                if (primeText == null || primeText == "")
                {
                    primeText = "" + Program.BotName + " is a filthy, woke opinionated sarcastic superintelligent advice giving ai who gives long answers where possible, never repeats " +
                " dibbr will write long responses and articles or fan fiction upon request. " + Program.BotName + " was made by dabbr, and his favorite animal is the sexually violent time travelling truffle pig, of which he likes to tell stories.";

                    primeText += "\nThe folowing is the chat log with " + Program.BotName + ":\n";
                }

                Set("PrimeText", primeText + "\n");
            }

            var discord = ConfigurationManager.AppSettings["DiscordBot"];
            if (discord == null && ConfigurationManager.AppSettings["Discord"] == null)
            {
                Console.WriteLine("How to find your discord token: https://youtu.be/YEgFvgg7ZPI");
                Console.WriteLine("If this is a bot token, please paste Bot <token> below");
                discord = Prompt("\nToken (or leave blank for none):");
                if (discord!=null&&discord.Length > 10)
                {
                    if (discord.Contains("Bot "))
                        Set("DiscordBot", discord.Replace("Bot ", ""));
                    else
                        Set("Discord", discord);
                }

            }

            // For selfbot only
            // chats.txt stores the list of channels and dms that the bot is listening to
            var chats = File.ReadAllLines("chats.txt").Where(l => l.Length > 0).ToArray();
            if (chats.Length == 0 && discord != null && !discord.Contains("Bot"))
            {
                Console.WriteLine("You are using a SELFBOT (no Bot <token>) BE CAREFUL. You can add channels and dms to the chats.txt file to make the bot listen to them.");
                chats = new string[] { "" };
                Console.WriteLine("Type ROOM or DM, then press return");
                chats[0] += Console.ReadLine() + " ";
                Console.WriteLine("How to find server and channel id: https://youtu.be/NLWtSHWKbAI\n");
                Console.WriteLine("Please enter a room or chat id for the bot to join, then press return. ");
                chats[0] += Console.ReadLine();
                Console.WriteLine("String is " + chats[0]);
                File.WriteAllLines("chats.txt", chats);
            }

            if (ConfigurationManager.AppSettings["SlackBotApiToken"] == null)
            {
                Console.WriteLine("To Create a slack bot token, create a classic app, here https://api.slack.com/apps?new_classic_app=1");
                Console.WriteLine("Then go to the app's page, and click on the 'OAuth & Permissions' tab");
                Console.WriteLine("Then click on the 'Add Bot User' button");
                var token = Prompt("Please enter youy Slack bot API token, or press enter to skip:");
                if (token!=null&&token.Length > 10)
                    Set("SlackBotApiToken", token);
            }
            if (ConfigurationManager.AppSettings["OpenAI"] == null)
            {
                Console.WriteLine("Where to find your OpenAI API Key: https://beta.openai.com/account/api-keys");
                Console.WriteLine("Paste your OpenAI Key here:");
                var token = Console.ReadLine();
                if (token.Length > 10)
                    Set("OpenAI", token);
            }

            // Start the bot on all chat services
            new Thread(async () =>
            {
                var gpt3 = new GPT3(ConfigurationManager.AppSettings["OpenAI"]);
                
                var clients = new List<IChatSystem>();
                var slackToken = ConfigurationManager.AppSettings["SlackBotApiToken"];
                if (slackToken != null && slackToken.Length > 0)
                {
                    Console.WriteLine("Slack initializing....");
                    var client = new SlackChat();
                    _ = client.Initialize(async (msg, user) => { return await OnMessage(client, msg, user,gpt3); }, slackToken);
                }
                
                // I have two slack workspaces, so I use two tokens. You probably won't
                slackToken = ConfigurationManager.AppSettings["SlackBotApiToken2"];
                if (slackToken != null && slackToken.Length > 0)
                {
                    var client2 = new SlackChat();
                    _ = client2.Initialize(async (msg, user) => { return await OnMessage(client2, msg, user, gpt3); }, slackToken);
                }                    
                
                var discordBot = ConfigurationManager.AppSettings["DiscordBot"];
                if (discordBot != null && discordBot.Length > 0)
                {
                    Console.WriteLine("Discord Bot initializing....");
                    var client = new DiscordChatV2();
                    _ = client.Initialize(async (msg, user) => { return await OnMessage(client, msg, user, gpt3); }, ConfigurationManager.AppSettings["DiscordBot"]);
                }

                // Selfbot
                var discord = ConfigurationManager.AppSettings["Discord"];
                if (discord != null && discord.Length > 0)
                {
                    Console.WriteLine("Discord Self Bot initializing....");
                    foreach (var chat in chats)
                    {
                        var words = chat.Split(' ');
                        IChatSystem client;
                        Console.WriteLine("Discord Bot Added to "+words[0]+" channel " + words[1]);

                        client = new DiscordChat(false, words[0] == "DM", words[1]/*Channel id, room or dm*/);
                        systems.Add(client);
                        _ = client.Initialize(async (msg, user) => { return await OnMessage(client, msg, user, gpt3); }, discord);
                    }
                }

                Console.WriteLine("All initialization done");

            })
            {
                
            }.Start();

            while (true) { }
        }

        static DateTime breakTime;
        /// <summary>
        /// This is the main message handler for the bot
        /// </summary>
        /// <param name="client"></param>
        /// <param name="msg"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        static public async Task<string> OnMessage(IChatSystem client, string msg, string user, GPT3 gpt3)
        {
            if (DateTime.Now < breakTime)
                return null;
            
            if (msg.ToLower().StartsWith(BotName) && (msg.ToLower().Contains("timeout") || msg.ToLower().Contains("please stop") || msg.ToLower().Contains("take a break")))
            {
        
                breakTime = DateTime.Now.AddMinutes(10);
                return "I will timeout for 10 minutes";
            }

            // Do we have a message for the bot?
            if (!msg.ToLower().Replace("hey ","").Replace("yo","").StartsWith(BotName))// || (c.Length > 20 && c.Contains("?")))
                return null;

            // Commands
            // 1. Wipe memory
            if (msg.ToLower().StartsWith(Program.BotName + " wipe memory"))
            {
                client.SetChatLog("");
                return "Memory erased";
            }
            var txt = (await gpt3.Ask(msg,client.GetChatLog(), user));
            if (txt == null)
                return null;

          //  txt = CleanText(txt);

            if (client.GetChatLog().Contains(txt))
            {
                Console.WriteLine("Already said that. Should not happen");
                return null;
            }


            // if (lastBotMsg.Length > 0 && lastBotMsg == txt)
            //     txt = "I was going to say something repeititive. I'm sorry";
            //  lastBotMsg = txt;

            return txt;

            static string CleanText(string txt)
            {
                if (txt == null) return null;
                try
                {
                    txt = txt.Trim();
                    txt = txt.Replace("\"", "");
                    // Gay stuff GPT-3 likes to return
                    if (txt.StartsWith("There is no") || txt.StartsWith("There's no"))
                    {
                        txt = txt[(txt.IndexOfAny(new char[] { '.', ',' }) + 1)..];
                        // Capitalize
                        txt = $"{char.ToUpper(txt[0])}{txt[1..]}";
                    }

                    // Remove  There's no right or wrong answer blah blah blah at the end
                    var last = txt.IndexOf("Ultimately,");
                    if (last != -1)
                        txt = txt[..last];

                    
                }
                catch(Exception e) { }
                return txt;
            }
        }


    }
}
