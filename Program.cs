using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace DibbrBot
{
    
    class Program
    {
        // How to find your token: https://youtu.be/YEgFvgg7ZPI
        private static string DiscordUserOrBotToken = "xxx";
        // Is the token above a bot token or user (Selfbot) token?
        // Warning: Selfbots get banned on discord regularly for violation of TOS!!!
        private static bool IsBot = false;

        private static HttpClient client = new HttpClient();

        static void Main(string[] args)
        {
            // init stuff

            
            // How to find server and channel id: https://youtu.be/NLWtSHWKbAI");
  
            // So this file is a list of DM or channel ids that the bot is listening to
            // You can add more when the bot is running by saying allow id, and it'll be saved to disk
            var chats = File.ReadAllLines("chats.txt").Where(l => l.Length > 0).ToArray();
            if (chats.Length == 0)
            {
                Console.WriteLine("Please enter a room or chat id for the bot to join, then press return. ");
                chats = new string[] { Console.ReadLine() };
            }
            // set the headers
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US");
            if(IsBot)
                client.DefaultRequestHeaders.Add("Authorization","Bot "+ DiscordUserOrBotToken);
            else
              client.DefaultRequestHeaders.Add("Authorization", DiscordUserOrBotToken);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) discord/0.0.309 Chrome/83.0.4103.122 Electron/9.3.5 Safari/537.36");

            // SSL Certificate Bypass
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            // start the farm
            var s = new Thread(async () =>
            {
                // wait 2 seconds for the header to set
                Console.WriteLine("Starting...");
                await Task.Delay(2000);

                // mrgirl chat room bot
                // now: teelo
                // var f1 = new Bot(/*"937151566266384394"*/"228257431145283594", true,client);
                // DM bot
                foreach (var chat in chats)
                    Bot.bots.Add(new Bot(chat, true, client));


                while (true)
                {
                    for (int i = 0; i < Bot.bots.Count; i++)
                    {
                        await Bot.bots[i].StartListening();
                    }
                    await Task.Delay(500);
                }
            });
            s.Start();


            while (true) { }
        }
    }
}
