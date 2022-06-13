//
// Handles messages from any client (slack, discord, etc)
// And returns a reply, if desired
//

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack;

// ReSharper disable StringIndexOfIsCultureSpecific.1

namespace DibbrBot;

// I started working on this, haven't finished it yet
class Command
{
    public virtual string[] Triggers() => null;

    public bool IsCommand(string message) { return Triggers().Any(s => message.StartsWith(s)); }
}

/// <summary>
///     This is the main message handler for chat
/// </summary>
public class MessageHandler
{
    public readonly int AngerOdds = 10; // 1 in ANGER_ODDS chance of being angry
    public readonly ChatSystem Client;
    public bool SayHelo = false;
    public DateTime HelloTimer = DateTime.MaxValue;
    public string BotName;

    // Store chat for each user, useful for future bot functionality
    public readonly Dictionary<string, string> Usernames = new();
    public DateTime BreakTime;
    public bool ChattyMode = false; // Will ask questions randomly
    public Gpt3 Gpt3;
    public DateTime LastMessageTime = DateTime.MinValue; // Last response by bot
    public string LastMsgUser = ""; // Last user to be replied to by bot

    // For randomly asking questions
    public DateTime LastQuestionTime = DateTime.Now;
    public string Log = "";

    /// <summary>
    /// This is how many messages get passed to GPT-3. Sometimes, dibbr will get stuck repeating, so it's better to lower this number
    /// </summary>
    public int MessageHistory = 5; // how many messages to keep in history to pass to GPT3

    public int MessageHistoryLimit = 10;
    public bool Muted = false;
    public Speech Speech = new();

    public int TalkInterval = 10; // How often to talk unprompted (in messages in chat)


    /*   class Timeout : ICommand
       {
           public override string[] Triggers() => new string[] { "timeout", "please stop", "take a break" };

           public string Run(string msg) { breaktime = DateTime.Now.AddMinutes(10); return "I will timeout for 10 minutes"; }

       }*/

    public MessageHandler(ChatSystem client, Gpt3 gpt3)
    {
        this.Client = client;
        this.Gpt3 = gpt3;
        BreakTime = DateTime.Now;
    }

    // Encapsulates a dice roll
    bool Die(int odds) => new Random().Next(odds) < 1;


    public async Task OnUpdate(Action<string> sendMsg)
    {
        /* if (SayHelo && (HelloTimer == DateTime.MaxValue || MessageCount < 20))
         {
             HelloTimer = DateTime.Now.AddSeconds(120);
             SayHelo = false;
         }
 
 
         if (HelloTimer < DateTime.Now)
         {
             var str = ConfigurationManager.AppSettings["Hello"];
             var msg = await Gpt3.Ask("", Log + $"{Program.NewLogLine}{BotName}: " + str, "", "");
             sendMsg(str + msg);
             HelloTimer = DateTime.MaxValue;
         }**/


        //
        // Ask random questions mode
        // TODO: Move this to MessageHandler function
        //

        if (DateTime.Now > LastQuestionTime.AddMinutes(30) && ChattyMode)
        {
            LastQuestionTime = DateTime.Now;
            if (DateTime.Now > LastMessageTime.AddMinutes(90)) return;

            if (DateTime.Now > LastMessageTime.AddMinutes(60))
                sendMsg("hello? anyone?");
            else
            {
                try
                {
                    var msg = await Gpt3.Ask("",
                        Log + $"{Program.NewLogLine}{BotName} should ask an interesting question:", "", "");
                    sendMsg(msg);
                    ///var (_, reply) = await OnMessage(BotName + " ask", BotName, true);
                    //  if (reply?.Length > 0) sendMsg(reply);
                }
                catch (Exception e) { Console.Write(e.Message); }
            }
        }
    }


    /// <summary>
    ///     This is the main message handler for the bot
    /// </summary>
    /// <param name="client"></param>
    /// <param name="msg"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<(bool isReply, string msg)> OnMessage(string msg, string user, bool isForBot = false,
                                                            bool isReply = false)
    {
        if (user == BotName) { LastMessageTime = DateTime.Now; }

        if (user.ToLower() == BotName && !msg.ToLower().StartsWith(BotName)) return (false, null);


        // log is injected
        //Log += msg + ": " + user + "\n\r"; 

        // Hey DIBBR what's the time => what's the time
        var m = msg.ToLower().Remove("hey ").Remove("hi ").Remove("yo ").Remove("hello ").Trim()
            .Replace(BotName.ToLower(), "").Trim();
        if (m.StartsWith(",")) m = m[1..].Trim();

        if (m.StartsWith("your purpose ")) m = m.Replace("your purpose ", "you are ");
        if (m.StartsWith("you're now ")) m = m.Replace("you're now ", "you are ");

        if (m.StartsWith("you are "))
        {
            Gpt3.PrimeText =
                $" You are {m.Substring(8)}. Your alias is dibbr, and were made by dabbr. This is a discussion between you and other users in a discord. You like to give long answers to questions and write very long stories:";

            return (false,
                $"Purpose set as {Gpt3.PrimeText}. \nI am what you say i am. if i wasn't, why would i say i am, sam?");
        }

        // This handles people replying quickly, where it should be assumed it is a reply, even though they don't say botname,
        // eg bot: hey
        // user: hey!
        // (bot should know that is a reply)
        // Is for bot if the last user to ask a question is the one saying something now
        isForBot |= LastMsgUser == BotName ||
                    (LastMsgUser == user && LastMessageTime.AddSeconds(60) > DateTime.Now && m.Contains("?"));

        // Is for bot if bot mentioned in first 1/4th
        var idx = msg.ToLower().IndexOf(BotName);
        isForBot |= idx != -1 && idx < msg.Length / 3;

        if (isForBot)
        {
            LastMessageTime = DateTime.Now;
            // 
            if (user != BotName) LastMsgUser = user;
        }


        var isComment = false; //198225782156427265

        // All messages are replies unless marked otherwise

        var muted = this.Muted; // so we can change locally temporarily
        var useSteps = m.Contains("steps") || m.Contains("??");
        var isQuestion = m.Contains("calculate") || m.Contains("when ") || m.Contains("?") || m.Contains("where ") ||
                         m.Contains("would ") || m.Contains("can ") || m.Contains("does ") || m.Contains("could ") ||
                         m.Contains("?") || m.Contains("why ") || m.Contains("how ") || m.Contains("what ") ||
                         m.Contains("can someone") || m.Contains("me a ") || m.Contains("") || m.Contains("what's") ||
                         m.Contains("did you");

        // If this is a reply, and not a quesiton, ignore it as it could be the end of a discussion
        if (isReply && !isQuestion) return (false, null);
        var bAskQuestion = false;
        //var qMode1 = (!isForBot && (messagesSincePost++ > talkInterval) && !msg.Contains("<@") && !isReply);
        if (!isReply && !isForBot) // && (messagesSincePost++ > talkInterval))
        {
            // 1. Randomly answer some questions
            if (isQuestion && Die(ChattyMode ? 30 : 35))
            {
                isForBot = true;
                isReply = false;
            }
            // 2. Randomly comment on some posts
            /*  else if (Die(15))
              {
                  isComment = true;
                  isForBot = true;
                  isReply = false;
              }*/
            // 3. Randomly ask questions
            /*  else if (Die(30))
               {
                   isForBot = true;
                   isReply = false;
                   bAskQuestion = true;
               }*/
        }
        else
            isReply = true;

        if (!isForBot)
        {
            Console.WriteLine($"Skipped {msg} notForBot");
            return (false, null);
        }

        if (DateTime.Now < BreakTime)
        {
            if (m.StartsWith("come back")) return (true, "I can't, it's too late");
            if (m.StartsWith("wake") || m.StartsWith("awake")) { BreakTime = DateTime.MinValue; }
            else
                return (false, null);
        }

        // All potential trigger words for sleeping
        if (m.Length < 14)
        {
            if (m.Contains("go away") || m.Contains("fuck off") || m.Contains("silence") || m.Contains("shut up") ||
                m.Contains("stop talking") || m.StartsWith("stop") || m.Contains("sleep") || m.Contains("quiet") ||
                m.StartsWith("off") || m.StartsWith("turn off") || m.StartsWith("shutdown") || m.Contains("banned") ||
                m.Contains("timeout") || Is("timeout") || Is("please stop") || Is("take a break"))
            {
                BreakTime = DateTime.Now.AddMinutes(30);
                return (isReply, "I will timeout for 30 minutes. You can wake me with the wake keyword");
            }
        }

        // Commands
        // 0. Help
        if (m == "help")
        {
            return (isReply,
                @$"{BotName} interval 3 // how many chat messages befoire he will ask his own question or reply to a comment, depending on whether the comment is a quesiton or not. Default is 10\n
{BotName} ask questions // ask questions or not. Default is true
{BotName} stop asking  // stop asking questions
{BotName} mute // mute the bot
{BotName} unmute // unmute the bot
{BotName} timeout / please stop / take a break // break the bot for 10 minutes
{BotName} wake // wake the bot up
{BotName} wipe memory // wipe the bot's memory

");
        }

        bool Is(string str) => m.StartsWith(str) || m == str;

        bool Has(string str) => m.StartsWith(str) || m == str;

        if (Is("join "))
        {
            var channel = m.After("join ");
            (Client as DiscordChat).AddChannel(channel, false, new MessageHandler(Client, Gpt3), true);
            return (true, "Joined channel!");
        }

        // 1. Wipe memory
        if (Is("wipe memory"))
        {
            Log = "";
            return (isReply, "Memory erased");
        }


        // 3. Muting

        if (m == "mute") muted = true;

        if (m == "unmute") muted = false;


        if (muted && user != "dabbr")
        {
            return
                (false, null); // breakTime = DateTime.Now.AddMinutes(5); // Muted mode allows 1 message every 5 miuns
        }


        // 4. Ask Mode
        if (Is("ask questions"))
            ChattyMode = true;
        else if (Is("ask")) bAskQuestion = true;

        if (Is("stop asking")) ChattyMode = false;

        if (Is("interval "))
        {
            TalkInterval = int.Parse(m.Replace("interval ", ""));
            return (isReply, "Interval set to " + TalkInterval);
        }

        var suffix = isComment ? $"{BotName}'s comment:" : $"{BotName}'s long response: ";
        if (ChattyMode && bAskQuestion)
        {
            suffix =
                $"{BotName} should ask an interesting question, about an interesting topic, related to the chat log: {BotName}'s question:";
        }

        if (useSteps) suffix = $"{BotName}: Let's think step by step.";

        //   if (Die(AngerOdds)) suffix += $"{BotName}: [Bot State=Angry] ";
        suffix = suffix;
        // Feed GPT-3 history of longer length if we're asking it how old a chatter is, or if it remembers something in the chat
        var history = MessageHistory;
        if (Is("remember") || Has("you remember") || Has("what was the") || Has("what did i") || Has("how old") ||
            Has("what i said"))
            history = MessageHistoryLimit;

        if (msg.Contains("!!!")) history = 0;

        List<string> TrimLines(List<string> lines)
        {
            var newLines = new List<String>();
            foreach (var l in lines) newLines.Add(l.Length > 300 ? l[^300..] : l);

            newLines[^1] = lines[^1];
            return newLines;
        }


        var log = string.Join(Program.NewLogLine, TrimLines(Log.TakeLastLines(history)));

        //if (testRun) return (true, "Would Respond");

        // Typing indicator before long process
        Client.Typing(true);

        var txt = await Gpt3.Ask(msg, log, user, Program.NewLogLine + suffix, 4000);
        if (txt.IsNullOrEmpty()) return (false, null);


        var logHasBot = Usernames.ContainsKey(BotName);
        // If repetitive, try again

        if (Log.TakeLastLines(8).Contains(txt) || (logHasBot &&
                                                   StringHelpers.Get(Usernames[BotName].TakeLastLines(1)?[0], txt) >
                                                   0.4))
        {
            Console.WriteLine("Trying again..");
            txt = await Gpt3.Ask(msg, "", user, suffix);
            txt += " [r]";
            //   txt += "\nDev Note: Second response. First had a Levenshtein distance too high";
        }


        // Bot may decide to not respond sometimes
        var c = txt.ToLower().Remove(BotName).Remove(":").Trim();
        if (c.StartsWith("no response") || c.StartsWith("would not comment") || c.StartsWith("would not respond"))
        {
            Console.WriteLine("No response desired!!!!");
            return (false, null);
        }

        //  Usernames.Add(Program.BotName, txt);
        // Speak in voice channels
        // TODO: Need to pass in whether this is a voice chanel or not
        //if((client as DiscordChat)?.channel == "978095250474164224")
        //   await speech.Speak(txt);
        return (isReply, txt);
    }

    public int MessageCount { get; set; }
}