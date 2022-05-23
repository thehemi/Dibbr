using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DibbrBot
{
    class ICommand
    {
        public virtual string[] Triggers()
        {
            return null;
        }

        public bool IsCommand(string message)
        {
            return Triggers().Any(s => message.StartsWith(s));
        }
    }




    class MessageHandler
    {
        int talkInterval = 10; // How often to talk unprompted (in messages in chat)
        int messagesSincePost = 0; // How many messages have been posted since last talk
        bool muted = false;
        bool chattyMode = true; // Will ask questions randomly
        GPT3 gpt3;
        IChatSystem client;
        DateTime breakTime;
        int MESSAGE_HISTORY = 10; // how many messages to keep in history to pass to GPT3
        int MESSAGE_HISTORY_LIMIT = 50;
        Speech speech = new Speech();

        /*   class Timeout : ICommand
           {
               public override string[] Triggers() => new string[] { "timeout", "please stop", "take a break" };

               public string Run(string msg) { breaktime = DateTime.Now.AddMinutes(10); return "I will timeout for 10 minutes"; }

           }*/

        public MessageHandler(IChatSystem client, GPT3 gpt3)
        {
            this.client = client;
            this.gpt3 = gpt3;
            breakTime = DateTime.Now;
            var client2 = client as DiscordChat;
            // Bot is not allowed here
            if (client2?.channel == "937151566266384394")
            {
                muted = true;
                chattyMode = false;
            }
        }

        bool Die(int odds)
        {
            return (new Random().Next(odds) < 1);
        }
        int runs = 0;
        Dictionary<string, string> Usernames = new Dictionary<string, string>();
        /// <summary>
        /// This is the main message handler for the bot
        /// </summary>
        /// <param name="client"></param>
        /// <param name="msg"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<(bool isReply, string msg)> OnMessage(string msg, string user, bool isForBot = false)
        {
            // Hey DIBBR what's the time => what's the time
            var m = msg.ToLower().Replace("hey ", "").Replace("yo ", "").Trim();

            if (!Usernames.ContainsKey(user))
                Usernames.Add(user, msg);
            else Usernames[user] += msg;

            var bAskQuestion = false;
            m = m.Replace(Program.BotName.ToLower(), "").Trim();
            if (m.StartsWith(","))
                m = m.Substring(1).Trim();

            if (m == null)
                return (false, null); ;

            var isComment = false; //198225782156427265

            // All messages are replies unless marked otherwise
            var isReply = true;

            if (m.ToLower().StartsWith("define "))
                isForBot = true;

            // TODO:
            // Bot replies preferentiably to friends
            runs++;
            if (user == "bobert" && runs < 5) isForBot = true;


            //var qMode1 = (!isForBot && (messagesSincePost++ > talkInterval) && !msg.Contains("<@") && !isReply);
            if (!isForBot && chattyMode)// && (messagesSincePost++ > talkInterval))
            {
                var isQuestion = m.StartsWith("what") || m.StartsWith("does") || m.StartsWith("would") || m.StartsWith("can") || m.StartsWith("does") || m.StartsWith("how") || m.Contains("?") || m.StartsWith("why") || m.StartsWith("how") || m.StartsWith("what") || m.Contains("can someone");

                // 1. Randomly answer some questions
                if (isQuestion && Die(15))
                    isForBot = true;
                // 2. Randomly comment on some posts
                if (Die(30))
                {
                    isComment = true;
                    isForBot = true;
                    isReply = false;
                }
                // 3. Randomly ask questions
                if (Die(30))
                {
                    isForBot = true;
                    isReply = false;
                    bAskQuestion = true;
                }
            }

            if (!isForBot) return (false, null);


            if (DateTime.Now < breakTime)
            {
                if (m.Contains("wake"))
                    breakTime = DateTime.MinValue;
                else
                    return (false, null); ;
            }

            // Bot message, erase counter
            messagesSincePost = 0;

            // Commands
            // 0. Help
            if (m == "help")
            {
                return (isReply, @$"{Program.BotName} interval 3 // how many chat messages befoire he will ask his own question or reply to a comment, depending on whether the comment is a quesiton or not. Default is 10\n
{Program.BotName} ask questions // ask questions or not. Default is true
{Program.BotName} stop asking  // stop asking questions
{Program.BotName} mute // mute the bot
{Program.BotName} unmute // unmute the bot
{Program.BotName} timeout / please stop / take a break // break the bot for 10 minutes
{Program.BotName} wake // wake the bot up
{Program.BotName} wipe memory // wipe the bot's memory

");
            }

            bool Is(string str) => m.StartsWith(str) || m == str;
            bool Has(string str) => m.StartsWith(str) || m == str;

            // 1. Wipe memory
            if (Is("wipe memory"))
            {
                client.SetChatLog("");
                return (isReply, "Memory erased");
            }

            // 2. Timeout
            if ((Is("timeout") || Is("please stop") || Is("take a break")))
            {
                breakTime = DateTime.Now.AddMinutes(10);
                return (isReply, "I will timeout for 10 minutes. You can wake me with the wake keyword");
            }

            // 3. Muting

            if (m == "mute")
                muted = true;
            if (m == "unmute")
                muted = false;


            if (muted)
                return (false, null);// breakTime = DateTime.Now.AddMinutes(5); // Muted mode allows 1 message every 5 miuns



            var suffix = isComment ? $"{Program.BotName}'s comment (put no response if {Program.BotName} would not comment next): " : $"{Program.BotName}'s response: ";

            // 4. Ask Mode
            if (Is("ask questions"))
                chattyMode = true;
            else if (Is("ask"))
            {
                bAskQuestion = true;
            }
            if (Is("stop asking"))
                chattyMode = false;
            if (Is("interval "))
            {
                talkInterval = Int32.Parse(m.Replace("interval ", ""));
                return (isReply, "Interval set to " + talkInterval);
            }

            if (chattyMode && bAskQuestion)
            {
                suffix = $"{Program.BotName} should ask an interesting question, about an interesting topic: {Program.BotName}: ";
            }

            // Feed GPT-3 history of longer length if we're asking it how old a chatter is, or if it remembers something in the chat
            var history = MESSAGE_HISTORY;
            if (Is("remember") || Has("you remember") || Has("what was the") || Has("what did i") || Has("how old") || Has("what i said"))
            {
                history = MESSAGE_HISTORY_LIMIT;
            }


            client.Typing(true);
            var log = client.GetChatLog(MESSAGE_HISTORY);
            var txt = (await gpt3.Ask(msg, log, user, suffix, log.Length));

            // If repetitive, try again
            if (Usernames.ContainsKey(Program.BotUsername) && LevenshteinDistance.Get(Usernames[Program.BotUsername].TakeLastLines(1)?[0], txt) > 0.4)
                txt = (await gpt3.Ask(msg, "", user, suffix, log.Length));

            if (txt == null)
                return (false, null); ;

            // Bot may not respond sometimes
            var c = txt.ToLower().Remove(Program.BotName).Remove(":");
            if (c.StartsWith("no response") || c.StartsWith("would not comment") || c.StartsWith("would not respond"))
            {
                Console.WriteLine("No response desired!!!!");
                return (false, null);
            }
            //  txt = CleanText(txt);

            if (client.GetChatLog(4).Contains(txt))
            {
                Console.WriteLine("Already said that. Should not happen");
                txt = await gpt3.Ask(msg, "", user, suffix);
                return (isReply, txt);// "I've answered that question, already";
            }


            // if (lastBotMsg.Length > 0 && lastBotMsg == txt)
            //     txt = "I was going to say something repeititive. I'm sorry";
            //  lastBotMsg = txt;
            if((client as DiscordChat)?.channel == "978095250474164224")
               await speech.Speak(txt);
            return (isReply, txt);

            string CleanText(string txt)
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
                catch (Exception e) { }
                return txt;
            }
        }
    }
}
