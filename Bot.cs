using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenAI_API;

namespace DibbrBot
{
    public abstract class IChatSystem
    {
        public abstract string GetChatLog();
        // Declare a delegate type for processing a book:
        public delegate Task<string> MessageRecievedCallback(string msg, string author);
       // public abstract Task<string> GetNewMessages();
     //   public abstract Task SendMessage(string message, string replyContext = null);
        public abstract Task Initialize(MessageRecievedCallback callback, string token);
    }

    /// <summary>
    /// Discord chat system
    /// TODO: This will replace the messy code below, like this
    /// var discord = new DiscordChat(true,true,"channelid");
    /// discord.Initialize(GPTHandleMessage);
    /// 
    /// </summary>
    public class DiscordChat : IChatSystem
    {
        public bool IsBot;
        public bool dm;
        public string channel;
        HttpClient client; // Shared
        public string ChatLog = "";
        private readonly int MAX_BUFFER = 600; // Chat buffer to GPT3 (memory)
        private readonly int MAX_MESSAGE = 2000; // Max message length
        
        public override string GetChatLog()
        {
            return ChatLog;
        }

        public DiscordChat(bool IsBot, bool isdm, string channel)
        {
            this.dm = isdm;
            this.channel = channel;
        }
 
        public override async Task Initialize(MessageRecievedCallback callback, string token = null)
        {
            if (client == null)
            {
                client = new HttpClient();
                // set the headers
                client.DefaultRequestHeaders.Add("Accept", "*/*");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US");
                client.DefaultRequestHeaders.Add("Authorization", token ?? APIKeys.Discord);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) discord/0.0.309 Chrome/83.0.4103.122 Electron/9.3.5 Safari/537.36");

                // SSL Certificate Bypass
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                // 2 secs for headers to take
                await Task.Delay(2000);
            }
            //
            new Thread(async delegate ()
            {
                string lastMsg = "";
                while (true)
                {
                    await Task.Delay(500);

                    // Read message
                    var message = dm ? await API.getLatestdm(client, channel) : await API.getLatestMessage(client, channel);
                    if (message == null)
                    {
                        dm = false;
                        message = await API.getLatestMessage(client, channel);
                        if (message == null)
                            continue;
                    }
                    var msgid = message["id"].ToString();
                    var msg = message["content"].ToString();
                    var auth = message["author"]["username"].ToString();
                    auth = auth.Replace("?????", "Q"); // Crap usernames
                    var c = auth + ": " + msg + "\n";
                    if (c == lastMsg)
                        continue;

                    bool first = lastMsg == "";
                    ChatLog += c;
                    lastMsg = c;

                    if (first)
                    {
                        // First message, we don't respond to it
                        continue;
                    }
                    
                    if (ChatLog.Length > MAX_BUFFER)
                        ChatLog = ChatLog.Substring(ChatLog.Length - MAX_BUFFER);

                    Console.WriteLine(c);
                    File.AppendAllText("chat_log_" + channel + ".txt", c);

                    var reply = await callback(msg, auth);
                    if(reply != null)
                    {
                        var response = dm ? await API.send_dm(client, channel, reply) : await API.send_message(client, channel, reply, msgid);
                        while (response == null || !response.IsSuccessStatusCode)
                            response = dm ? await API.send_dm(client, channel, reply) : await API.send_message(client, channel, reply, msgid);
                    }
                }

                Console.WriteLine("How did I get here?");
            }).Start();
          
            }

    }



    public class GPT3
    {
      
        // Shared between all instances
        private static OpenAIAPI api;
        

        /// <summary>
        /// Asks OpenAI
        /// </summary>
        /// <param name="q"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task<string> Ask(string q, string user = "")
        {
            if (api == null)
            {
                var eng = new Engine("text-davinci-002") { Owner = "openai", Ready = true };
                var k = new APIAuthentication(APIKeys.OpenAI);
                api = new OpenAI_API.OpenAIAPI(apiKeys: k, engine: eng);
            }
         // Prime it with other questions here
         // q = "questions"+q;

            // Setup context, insert chat history
            var txt = "Dibbr is a filthy, woke opinionated sarcastic superintelligent advice giving ai who gives long answers where possible, never repeats."+
                " dibbr will write long responses and articles or fan fiction upon request.\nThe folowing is the discord chat log:\n"
                    + q + "Write your response or article here:dibbr:";
            
            var result = await api.Completions.CreateCompletionAsync(txt,
                temperature: 1.0, top_p: 1, max_tokens: 2000, stopSequences: new string[] { Program.BotName + ":" });

            var r = result.ToString();
            Console.WriteLine("(GPT 3): " + r);
            return r;
        }


        public async Task<string> Ask2(string q, string user = "")
        {
            if (api == null)
            {
                var eng = new Engine("text-davinci-002") { Owner = "openai", Ready = true };
                var k = new APIAuthentication(APIKeys.OpenAI);
                api = new OpenAI_API.OpenAIAPI(apiKeys: k, engine: eng);
            }
            var stops =
                new string[] { Program.BotName+":"};
            var result = await api.Completions.CreateCompletionAsync(q,
                temperature: 0.8, top_p: 1, max_tokens: 2000, stopSequences: stops);

            var r = result.ToString();
            Console.WriteLine("GPT3 response: "+r);
            return r;
        }
    }        
}
