using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace DibbrBot
{
    // Fill these in
    public static partial class APIKeys
    {
        public static readonly string OpenAI = "";
        // How to find your token: https://youtu.be/YEgFvgg7ZPI
        // If you're using a bot, make this string "Bot <token>"
        public static  string Discord = "";
        public static string DiscordBot = "";        
    }


    class Program
    {
        public static string BotName = "dibbr";
        static string BotUsername = "dabbr";


        static void Main(string[] args)
        {
            // So this file is a list of DM or channel ids that the bot is listening to
            // You can add more when the bot is running by saying allow id, and it'll be saved to disk
            // File format:
            // XX XX DiscordV2
            // ROOM 569702120190771200 DiscordV1
            // DM 553003716081614861 DiscordV1
            // BLAH SLACK Slack

            var chats = File.ReadAllLines("chats.txt").Where(l => l.Length > 0).ToArray();
            if (chats.Length == 0)
            {
                chats = new string[] { "" };
                Console.WriteLine("Type ROOM or DM, then press return");
                chats[0] += Console.ReadLine() + " ";

                Console.WriteLine("Please enter a room or chat id for the bot to join, then press return. ");
                Console.WriteLine("How to find server and channel id: https://youtu.be/NLWtSHWKbAI\n");
                chats[0] += Console.ReadLine() + " DiscordV2";
                File.WriteAllLines("chats.txt", chats);
            }
            new Thread(async () =>
            {
                var clients = new List<IChatSystem>();
                foreach (var chat in chats)
                {
                    var words = chat.Split(' ');
                    
                    IChatSystem client;
                    string token = null;

                    // My server, use bot token, and use V2 discord system, which supports bots
                    if (words[2] == "DiscordV2")
                    {
                        client = new DiscordChatV2();
                        token = APIKeys.DiscordBot;
                    }
                    else if (words[2] == "DiscordV1")
                        client = new DiscordChat(false, words[0] == "DM", words[1]);
                    else //if (words[2] == "Slack")
                        client = new SlackChat();

                    _ = client.Initialize(async (msg, user) => { return await OnMessage(client, msg, user); }, token);

                }
            }).Start();

            while (true) { }
        }        

        static async Task<string> OnMessage(IChatSystem client, string msg, string user)
        {
            if (msg.ToLower().StartsWith(BotName + " timeout") || msg.ToLower().StartsWith(BotName + ", timeout"))
            {
                await Task.Delay(5 * 60000);
                return null;
            }

            // Do we have a message for the bot?
            if (/*!dm && !(msg.ToLower().StartsWith(BotName) || */!msg.ToLower().StartsWith(BotName))// || (c.Length > 20 && c.Contains("?")))
                return null;


            /*  if (msg.StartsWith("allow"))
              {
                  var id = msg.Substring(msg.IndexOf("allow") + 6);
                  File.AppendAllLines("chats.txt", new string[] { id });
                  if (id.Length == 18)
                  {
                   // Add a new client here
                  }

              }*/
            
            // Author running the bot can trigger bot with dibbr, only, to avoid recursion
           // if (!msg.StartsWith(BotName))
            //    return null;

            var txt = await GPT3.Ask(client.GetChatLog(), user);
            // if (lastDibbrMsg.Length > 0 && lastDibbrMsg == txt)
            //     txt = "I was going to say something repeititive. I'm sorry";
            txt = txt.Replace("\n", "");
            txt = txt.Replace("\"", "");
            // Gay stuff GPT-3 likes to return
            if (txt.StartsWith("There is no") || txt.StartsWith("There's no"))
            {
                txt = txt.Substring(txt.IndexOfAny(new char[] { '.', ',' }) + 1);
            }

            // Remove  There's no right or wrong answer blah blah blah at the end
            var last = txt.IndexOf("Ultimately,");
            if (last != -1)
                txt = txt.Substring(0, last);


            return txt;
        }


    }
}
