//
// Handles messages from any client (slack, discord, etc)
// And returns a reply, if desired
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DibbrBot
{
    // I started working on this, haven't finished it yet
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



    /// <summary>
    /// This is the main message handler for chat
    /// </summary>
    public class MessageHandler
    {
        public string Log = "";
        int ANGER_ODDS = 10; // 1 in ANGER_ODDS chance of being angry
        int talkInterval = 10; // How often to talk unprompted (in messages in chat)
        int messagesSincePost = 0; // How many messages have been posted since last talk
        public bool muted = false;
        public bool chattyMode = true; // Will ask questions randomly
        public GPT3 gpt3;
        IChatSystem client;
        DateTime breakTime;
        public int MESSAGE_HISTORY = 10; // how many messages to keep in history to pass to GPT3
        public int MESSAGE_HISTORY_LIMIT = 50;
        Speech speech = new Speech();
        int runs = 0;
        // Store chat for each user, useful for future bot functionality
        Dictionary<string, string> Usernames = new Dictionary<string, string>();
        public string lastMsgUser = ""; // Last user to be replied to by bot
        public DateTime lastMessageTime = DateTime.MinValue; // Last response by bot


        // For randomly asking questions
        DateTime lastQuestionTime = DateTime.Now;


        
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

        }

        // Encapsulates a dice roll
        bool Die(int odds)
        {
            return (new Random().Next(odds) < 1);
        }

        public async Task OnUpdate(Action<string> sendMsg)
        {
            //
            // Ask random questions mode
            // TODO: Move this to MessageHandler function
            //
            if (DateTime.Now > lastQuestionTime.AddMinutes(30) && chattyMode)
            {
                lastQuestionTime = DateTime.Now;
                if (DateTime.Now > lastMessageTime.AddMinutes(90))
                    return;
                else if (DateTime.Now > lastMessageTime.AddMinutes(60))
                {
                    sendMsg("hello? anyone?");
                }
                else
                {
                    try
                    {
                        var (isReply1, reply) = await OnMessage(Program.BotName + " ask", "dibbr", true);
                        sendMsg(reply);
                       
                        
                    }
                    catch (Exception e)
                    {
                        Console.Write(e.Message);
                    }
                }

            }
        }
        
        
        
        /// <summary>
        /// This is the main message handler for the bot
        /// </summary>
        /// <param name="client"></param>
        /// <param name="msg"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<(bool isReply, string msg)> OnMessage(string msg, string user, bool isForBot = false, bool testRun = false)
        {
           // Store per user logs
            if (!Usernames.ContainsKey(user))
                Usernames.Add(user, msg + Program.NewLogLine);
            else Usernames[user] += msg + Program.NewLogLine;

            // log is injected
            //Log += msg + ": " + user + "\n\r"; 

            // Hey DIBBR what's the time => what's the time
            var m = msg.ToLower().Replace("hey ", "").Replace("yo ", "").Trim();
            m = m.Replace(Program.BotName.ToLower(), "").Trim();
            if (m.StartsWith(","))
                m = m.Substring(1).Trim();

            if (m == null)
                return (false, null); ;


            // Is for bot if we were the last ones to talk to bot, and this message is very recent
            isForBot |= lastMsgUser == user && (lastMessageTime.AddSeconds(40) > DateTime.Now);

            // Is for bot if bot mentioned in first 1/4th
            var idx = msg.ToLower().IndexOf(Program.BotName);
            isForBot |= (idx != -1 && idx < msg.Length / 4);

            if (isForBot)
            {
                lastMessageTime = DateTime.Now;
                lastMsgUser = user;
            }

            var bAskQuestion = false;
            var isComment = false; //198225782156427265

            // All messages are replies unless marked otherwise
            var isReply = true;
            var muted = this.muted;
            var useSteps = m.Contains("use steps") || m.Contains("usesteps");
            if (useSteps)
                m = m.Replace("use steps", "").Replace("usesteps", "").Trim();
            bool isQuestion = m.StartsWith("calculate") || m.StartsWith("what") || m.EndsWith("?") || m.StartsWith("where") || m.StartsWith("would") || m.StartsWith("can") || m.StartsWith("does") || m.StartsWith("how") || m.Contains("?") || m.StartsWith("why") || m.StartsWith("how") || m.StartsWith("what") || m.Contains("can someone");
       
            //var qMode1 = (!isForBot && (messagesSincePost++ > talkInterval) && !msg.Contains("<@") && !isReply);
            if (!isForBot && chattyMode)// && (messagesSincePost++ > talkInterval))
            {

                // 1. Randomly answer some questions
                if (isQuestion && Die(15))
                    isForBot = true;
                // 2. Randomly comment on some posts
                else if (Die(15))
                {
                    isComment = true;
                    isForBot = true;
                    isReply = false;
                }
                // 3. Randomly ask questions
                else if (Die(30))
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
                Log = "";
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
                if(user != "dabbr")
                     return (false, null);// breakTime = DateTime.Now.AddMinutes(5); // Muted mode allows 1 message every 5 miuns



          
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

            var suffix = isComment ? $"{Program.BotName}'s comment:" : $"{Program.BotName}'s response: ";
            if (chattyMode && bAskQuestion)
            {
                suffix = $"{Program.BotName} should ask an interesting question, about an interesting topic, related to the chat log: {Program.BotName}'s question: ";
            }
            if(useSteps || isQuestion)
                suffix = $"{Program.BotName}: Let's think step by step.";

            if (Die(ANGER_ODDS))
                suffix += $"({Program.BotName} is furious)";

            // Feed GPT-3 history of longer length if we're asking it how old a chatter is, or if it remembers something in the chat
            var history = MESSAGE_HISTORY;
            if (Is("remember") || Has("you remember") || Has("what was the") || Has("what did i") || Has("how old") || Has("what i said"))
            {
                history = MESSAGE_HISTORY_LIMIT;
            }
            var log = String.Join(Program.NewLogLine, Log.TakeLastLines(history));

            if (testRun)
            {
                return (true, "Would Respond");
            }
            // Typing indicator before long process
            client.Typing(true);
            
            var txt = await gpt3.Ask(msg, log, user, suffix, log.Length);

            // If repetitive, try again
            if (Log.TakeLastLines(4).Contains(txt) || ((Usernames.ContainsKey(Program.BotUsername) && StringHelpers.Get(Usernames[Program.BotUsername].TakeLastLines(1)?[0], txt) > 0.7))){
                txt = (await gpt3.Ask(msg, "", user, suffix, log.Length));
                if (txt != null)
                    txt += "\nDev Note: Second response. First had a Levenshtein distance too high";
            }                

            if (txt == null)
                return (false, null); ;

            // Bot may decide to not respond sometimes
            var c = txt.ToLower().Remove(Program.BotName).Remove(":");
            if (c.StartsWith("no response") || c.StartsWith("would not comment") || c.StartsWith("would not respond"))
            {
                Console.WriteLine("No response desired!!!!");
                return (false, null);
            }
    
            // Speak in voice channels
            // TODO: Need to pass in whether this is a voice chanel or not
            //if((client as DiscordChat)?.channel == "978095250474164224")
            //   await speech.Speak(txt);
            return (isReply, txt);
        }

        // GPT-3 says these pharses a lot, so you might want to parse them out...
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
