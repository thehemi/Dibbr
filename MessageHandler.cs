//
// Handles messages from any client (slack, discord, etc)
// And returns a reply, if desired
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    // Store chat for each user, useful for future bot functionality
    public readonly Dictionary<string, string> Usernames = new();
    public DateTime BreakTime;
    public bool ChattyMode = true; // Will ask questions randomly
    public Gpt3 Gpt3;
    public DateTime LastMessageTime = DateTime.MinValue; // Last response by bot
    public string LastMsgUser = ""; // Last user to be replied to by bot

    // For randomly asking questions
    public DateTime LastQuestionTime = DateTime.Now;
    public string Log = "";
    public int MessageHistory = 10; // how many messages to keep in history to pass to GPT3
    public int MessageHistoryLimit = 50;
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
                    var (_, reply) = await OnMessage(Program.BotName + " ask", Program.BotName, true);
                    if (reply.Length > 0) sendMsg(reply);
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
                                                            bool testRun = false)
    {
        // Store per user logs
        if (!Usernames.ContainsKey(user))
            Usernames.Add(user, msg + Program.NewLogLine);
        else
            Usernames[user] += msg + Program.NewLogLine;

        // log is injected
        //Log += msg + ": " + user + "\n\r"; 

        // Hey DIBBR what's the time => what's the time
        var m = msg.ToLower().Replace("hey ", "").Replace("yo ", "").Trim().Replace(Program.BotName.ToLower(), "")
            .Trim();
        if (m.StartsWith(",")) m = m[1..].Trim();

        // Is for bot if we were the last ones to talk to bot, and this message is very recent
        isForBot |= LastMsgUser == user && LastMessageTime.AddSeconds(40) > DateTime.Now;

        // Is for bot if bot mentioned in first 1/4th
        var idx = msg.ToLower().IndexOf(Program.BotName);
        isForBot |= idx != -1 && idx < msg.Length / 4;

        if (isForBot)
        {
            LastMessageTime = DateTime.Now;
            LastMsgUser = user;
        }


        var isComment = false; //198225782156427265

        // All messages are replies unless marked otherwise

        var muted = this.Muted; // so we can change locally temporarily
        var useSteps = m.Contains("steps") || m.Contains("??");
        var isQuestion = m.StartsWith("calculate") || m.StartsWith("what") || m.EndsWith("?") ||
                         m.StartsWith("where") || m.StartsWith("would") || m.StartsWith("can") ||
                         m.StartsWith("does") || m.StartsWith("how") || m.Contains("?") || m.StartsWith("why") ||
                         m.StartsWith("how") || m.StartsWith("what") || m.Contains("can someone");

        var bAskQuestion = false;
        var isReply = true;
        //var qMode1 = (!isForBot && (messagesSincePost++ > talkInterval) && !msg.Contains("<@") && !isReply);
        if (!isForBot && ChattyMode) // && (messagesSincePost++ > talkInterval))
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


        if (DateTime.Now < BreakTime)
        {
            if (m.Contains("wake"))
                BreakTime = DateTime.MinValue;
            else
                return (false, null);

            ;
        }

        // Commands
        // 0. Help
        if (m == "help")
        {
            return (isReply,
                @$"{Program.BotName} interval 3 // how many chat messages befoire he will ask his own question or reply to a comment, depending on whether the comment is a quesiton or not. Default is 10\n
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
        if (Is("timeout") || Is("please stop") || Is("take a break"))
        {
            BreakTime = DateTime.Now.AddMinutes(10);
            return (isReply, "I will timeout for 10 minutes. You can wake me with the wake keyword");
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

        var suffix = isComment ? $"{Program.BotName}'s comment:" : $"{Program.BotName}'s response: ";
        if (ChattyMode && bAskQuestion)
        {
            suffix =
                $"{Program.BotName} should ask an interesting question, about an interesting topic, related to the chat log: {Program.BotName}'s question: ";
        }

        if (useSteps) suffix = $"{Program.BotName}'s response: Let's think step by step.";

        if (Die(AngerOdds)) suffix += $"({Program.BotName} is furious)";

        // Feed GPT-3 history of longer length if we're asking it how old a chatter is, or if it remembers something in the chat
        var history = MessageHistory;
        if (Is("remember") || Has("you remember") || Has("what was the") || Has("what did i") || Has("how old") ||
            Has("what i said"))
            history = MessageHistoryLimit;

        var log = string.Join(Program.NewLogLine, Log.TakeLastLines(history));

        if (testRun) return (true, "Would Respond");

        // Typing indicator before long process
        Client.Typing(true);

        var txt = await Gpt3.Ask(msg, log, user, suffix, log.Length);
        if (txt == "") return (false, null);

        // If repetitive, try again
        if (Log.TakeLastLines(4).Contains(txt) || (Usernames.ContainsKey(Program.BotUsername) &&
                                                   StringHelpers.Get(
                                                       Usernames[Program.BotUsername].TakeLastLines(1)?[0], txt) > 0.7))
        {
            txt = await Gpt3.Ask(msg, "", user, suffix, log.Length);
            txt += "\nDev Note: Second response. First had a Levenshtein distance too high";
        }


        // Bot may decide to not respond sometimes
        var c = txt.ToLower().Remove(Program.BotName).Remove(":").Trim();
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
}