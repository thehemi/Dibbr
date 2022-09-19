//
// Handles messages from any client (slack, discord, etc)
// And returns a reply, if desired
//

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using ServiceStack;
using ServiceStack.Logging;
using ServiceStack.Script;
using Twilio.TwiML.Voice;
using Task = System.Threading.Tasks.Task;

// ReSharper disable StringIndexOfIsCultureSpecific.1

namespace DibbrBot;

// I started working on this, haven't finished it yet
class Command
{
    public virtual string[] Triggers() => null;

    public bool IsCommand(string message) { return Triggers().Any(s => message.StartsWithAny(s)); }
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
    public string Channel;
    public bool UseNeo  = true;
    // Store chat for each user, useful for future bot functionality
    public readonly Dictionary<string, string> Usernames = new();
    public DateTime BreakTime;
    public bool ChattyMode = false; // Will ask questions randomly
    public Gpt3 Gpt3;
    public DateTime LastMessageTime = DateTime.MinValue; // Last response by bot
    public string LastMsgUser = ""; // Last user to be replied to by bot

    // For randomly asking questionsFs
    public DateTime LastQuestionTime = DateTime.Now;
    public List<string> Log = new List<string>();

    /// <summary>
    /// This is how many messages get passed to GPT-3. Sometimes, dibbr will get stuck repeating, so it's better to lower this number
    /// </summary>
    public int MessageHistory = 8; // how many messages to keep in history to pass to GPT3

    public int MessageHistoryLimit = 10;
    public bool Muted = false;
    public Speech Speech = new();

    public int TalkInterval = 10; // How often to talk unprompted (in messages in chat)


    /*   class Timeout : ICommand
       {
           public override string[] Triggers() => new string[] { "timeout", "please stop", "take a break" };

           public string Run(string msg) { breaktime = DateTime.Now.AddMinutes(10); return "I will timeout for 10 minutes"; }

       }*/

    public MessageHandler(ChatSystem client, Gpt3 gpt3 = null)
    {
        this.Client = client;
        this.Gpt3 = gpt3;
        if (gpt3 == null)
                    this.Gpt3 = new Gpt3(ConfigurationManager.AppSettings["OpenAI"], "text-davinci-002");
                   
        BreakTime = DateTime.Now;
    }

    // Encapsulates a dice roll
    bool Die(int odds) => new Random().Next(odds) < 1;


    public async Task OnUpdate(Action<string> sendMsg, bool dm)
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
        if (!dm)
            if (DateTime.Now > LastQuestionTime.AddMinutes(60 * 12) || !ChattyMode)
            {
                ChattyMode = true;
                LastQuestionTime = DateTime.Now;
                //if (DateTime.Now > LastMessageTime.AddMinutes(90)) return;

                // if (DateTime.Now > LastMessageTime.AddMinutes(60))
                //     sendMsg("hello? anyone?");

                {
                    try
                    {
                        var Prime = ConfigurationManager.AppSettings["PrimeText"] + "\n";
                        var msg = await Gpt3.Ask(Prime + Log + $"{BotName} should say something or ask something original hilarious or zany or tell everyone you are back online after a crash:");
                        //        msg = msg.Trim(new char[] { '\n', '\r', ' ' });
                        sendMsg(msg);
                        //    var (_, reply) = await OnMessage(BotName + " ask", BotName, true);
                        //     if (reply?.Length > 0) sendMsg(reply);
                    }
                    catch (Exception e) { Console.Write(e.Message); }
                }
            }
    }

    string replyPrompt = "";
    public string PrimeText = "";
    public string Memory = "";
    public int x = 0;
    public bool DebugMode = false;
    public int ConversationFactor;
    /// <summary>
    ///     This is the main message handler for the bot
    ///     Auto messages are when the code thinks the bot should join in, e.g. a follow up
    ///     We use some clever tricks to bucket a message:
    ///     -Is it a question (where/when/what) that can stand by itself, and doesn't refer to the chat log or dibbr?
    ///         -Check for you, our, my, this, etc
    ///         -Rule out auto follow-ups
    ///         -Check length
    ///     /// </summary>
    /// <param name="client"></param>
    /// <param name="msg"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<(bool isReply, string msg)> OnMessage(string msg, string user, bool isAutoMessage = false,
                                                            bool isReply = true)
    {
        var useNeo = UseNeo;// && msg.Contains("$$");
        if (msg.EndsWith("debug on"))
        {
            DebugMode = true;
            return (true, "Debug mode on");
        }
        if (msg.EndsWith("debug off"))
        { 
            DebugMode = false;
            return (true, "Debug mode off");
        }

        bool IsDupe(string txt) => (Log.Last(3).Where(s => s.Contains(txt, StringComparison.InvariantCultureIgnoreCase)).Count() > 0 || (Usernames.ContainsKey(BotName) &&
                                                  StringHelpers.Get(Usernames[BotName].TakeLastLines(1)?[0], txt) >
                                                   0.4));
        bool IsContinue(string m) => m.Contains("continue") || (m.Contains("rewrite") && m.Length < 30) || m.Contains("more detail") || m.Contains("go on") || m.Contains("keep going");
        bool IsWriting(string m) => m.Remove("please").Remove("can you").StartsWithAny("write") || (m.HasAny("write", "design", "create", "compose", "generate") && m.HasAny(" rap", "article", "story", "long", "dialog", "discussion", "script", "game",
            "detailed", "screenplay", "letter", "episode", "design", "theory", "explanation", "essay"));
        bool IsQuestion(string m) => m.StartsWithAny("calculate", "when ", "where ", "where's", "would ", "can ", "does ", "could ", "why ",
            "how ", "what ", "can someone", "what's", "name  ") || m.Contains("?") ;
        var queryOnly = msg.Contains("!!!");
        var isForBot = true;
        if (user == BotName) { LastMessageTime = DateTime.Now; }

        if (user.ToLower() == BotName.ToLower() && !msg.ToLower().StartsWithAny(BotName.ToLower())) return (isReply, null);

        if (PrimeText == null || PrimeText.Length == 0)
        {
            PrimeText = ConfigurationManager.AppSettings[this.Channel + "PrimeText"] ?? ConfigurationManager.AppSettings["PrimeText"];
            Gpt3.context = PrimeText;
        }
        // log is injected
        //Log += msg + ": " + user + "\n\r"; 

        // Hey DIBBR what's the time => what's the time
        var m = msg.Rem(BotName,"please","can you").Replace(", "," ").Replace("  "," ").Trim(new char[] {' ',',','\n','\r','.'});


        if (m.StartsWithAny("shutdown123")) { Program.Shutdown = true; return (true, "Shutting down.."); }

        if (m.Contains("reddit.com"))
        {
            return (true, "https://www.unddit.com/" + m.After("reddit.com/"));

        }
        if (m.StartsWithAny("what is your prompt") || m.StartsWithAny("what prompt"))
        {
            var idx1 = PrimeText.IndexOf("..");
            if (idx1 == -1) idx1 = PrimeText.Length;
            return (isReply, PrimeText);//.Substring(0, idx1)) ;
        }
        if (m.StartsWithAny("is my daddy")) msg = "Tell me about being my daddy";

        if (m.StartsWithAny("add prompt"))
        {
            PrimeText +=
                $"{msg.Substring(11)}";
            return (isReply, "ok");
        }

        if (m.StartsWithAny("set prompt"))
        {
            PrimeText =
                $"{msg.After("set prompt")}";
            Gpt3.context = $"{msg.After("set prompt")}"; ;
            Program.Set(Channel + "Prime", PrimeText);
            if (user != "dabbr" && user != "edge_case" && !user.Contains("dΦbbr")) return (isReply, $"k dude");

            return (isReply, $"ok");
        }
        else if (m.StartsWithAny("reset"))
        {
            Gpt3.pairs.Clear();
            Memory = "";
            Log.Clear();
            PrimeText = ConfigurationManager.AppSettings["PrimeText"];
            Gpt3.context = PrimeText;
            // replyPrompt = "";
            return (true, "Reset all");
        }
        if(m.EndsWith("->") && m.Contains("+"))
        {

        }
        if (m.StartsWithAny("eat key"))
        {
            Gpt3._token = msg.After("eat key").Trim();
        }
        if (m == "neo on")
        {
            UseNeo = true;
            return (true, "Neo enabled");
        }
        if (m == "neo off")
        {
            UseNeo = false;
            return (true, "GPT3 enabled");
        }

        // This handles people replying quickly, where it should be assumed it is a reply, even though they don't say botname,
        // eg bot: hey
        // user: hey!
        // (bot should know that is a reply)
        // Is for bot if the last user to ask a question is the one saying something now
        //      isForBot |= (LastMsgUser == user && m.Contains("?"));

        // Is for bot if bot mentioned -----------in first 1/4th
        var idx = msg.ToLower().IndexOf(BotName);
        isForBot |= idx != -1; // && idx < msg.Length / 3;

        if (isForBot)
        {
            LastMessageTime = DateTime.Now;
            // 
            if (user != BotName) LastMsgUser = user;
        }


        var isComment = false; //198225782156427265

        // All messages are replies unless marked otherwise

        var muted = this.Muted; // so we can change locally temporarily
        Regex hasMath = new Regex(@"(?<num1>[0-9]+)(?<op>[\*\-\+\\\^])(?<num2>[0-9]+)");
        var math = hasMath.IsMatch(m);
        var useSteps = m.HasAny("steps", "??", "calculate", "solve", "given ", "if there are","explain","what is "," how ","what would","how do i","what will") || math || m.Has(2,"how","what","why","when","where");

        // Good questions will use step mode
        //  if (IsQuestion(m) && m.Length > "What is the bible".Length)
        //      useSteps = true;

        if (Personal(m))
            ConversationFactor += 2;
        else ConversationFactor -= 1;
        if(ConversationFactor < -2) ConversationFactor = -2;

       // if(ConversationFactor > 3) 

        if (Personal(m) || isAutoMessage)
            useSteps = false;

        if (useSteps)
            useNeo = false;
       

        /// if (UseNeo) useSteps = false;
        
        if (m.Contains("??"))
            useSteps = true;

        msg = msg.Replace("??", "?");


        if (!isForBot)
        {
            Console.WriteLine($"Skipped {msg} notForBot");
            return (isReply, null);
        }

        if (DateTime.Now < BreakTime)
        {
            if (m.StartsWithAny("come back")) return (true, "I can't, it's too late");
            if (m.StartsWithAny("wake") || m.StartsWithAny("awake")) { BreakTime = DateTime.MinValue; }
            else
                return (isReply, null);
        }

        // All potential trigger words for sleeping
        if (m.Length < 14)
        {
            if (m.HasAny("be quiet", "stupid bot", "timeout", "go away", "shut down", "shutdown", "fuck off", "silence", "shut up", "sleep", "mute", "be quiet", "ban") || m.Contains("stop talking") ||
                m.StartsWithAny("stop") || m.StartsWithAny("off") || m.StartsWithAny("turn off") || m.Contains("timeout") ||
                Is("timeout") || Is("please stop") || Is("take a break"))
            {
                BreakTime = DateTime.Now.AddMinutes(30);
                return (isReply, "I'm sorry :( I will timeout for 30 minutes. You can wake me with the wake keyword");
            }
        }

        if (m == "gpt3test")
        {
            for (int i = 0; i < 11; i++)
            {
                var t = ConfigurationManager.AppSettings["OpenAI" + i];
                if (t == null) continue;


                var ret = await this.Gpt3.Test(t);
                if (ret.Length > 0)
                {
                    Gpt3._token = t;
                    return (true, $"Key {i} works!");
                }
            }
            return (true, $"no keys work");
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

        bool Is(string str) => m.StartsWithAny(str) || m == str;

        bool Has(string str) => m.StartsWithAny(str) || m == str;

        /* if (Is("join "))
         {
             var channel = m.After("join ");
             (Client as DiscordChat).AddChannel(channel, false, new MessageHandler(Client, Gpt3), true);
             return (true, "Joined channel!");
         }*/

        // 1. Wipe memory
        if (m.Contains("wipe memory"))
        {
            //if (BotName is "dibbr")
            //     if (user != "dabbr" && user != "edge_case")
            //         return (isReply, "Denied");
            Log.Clear();
            Gpt3.pairs = new List<string>();
            return (isReply, "Memory erased");
        }

     

        // 3. Muting

        if (m == "mute") muted = true;

        if (m == "unmute") muted = false;


        if (muted && user != "dabbr")
        {
            return
                (isReply, null); // breakTime = DateTime.Now.AddMinutes(5); // Muted mode allows 1 message every 5 miuns
        }
        if(m.Contains("what's your number"))
        {
            return (true, "You can SMS me at (USA+1) 970-696-8551");
        }
        if(m.StartsWith("smsdump"))
        {
            var res = Dibbr.Phone.Send("", "");
            return (true, res);
        }
        if (m.StartsWithAny("text"))
        {
            try
            {
                var number = msg.After("text ");
                number = number.Sub(0, number.IndexOf(" "));
                var sms = msg.After(number);
                var res = Dibbr.Phone.Send(number, sms);
                return (true, res);
            }
            catch (Exception e) { return (true, "Sorry I crashed"); }
        }
        if (Is("interval "))
        {
            TalkInterval = int.Parse(m.Replace("interval ", ""));
            return (isReply, "Interval set to " + TalkInterval);
        }

        if (Is("print scores"))
        {
            var msgScore = "The scores (characters used) are:\n";
            for (var i = 0; i < ConfigurationManager.AppSettings.Keys.Count; i++)
            {
                var k = ConfigurationManager.AppSettings.Keys[i];
                var v = ConfigurationManager.AppSettings.GetValues(i);
                var val = v?.Length > 0 ? v[0] : null;
                if (int.TryParse(val, out int result))
                {
                    if (result > 0 && result < 200000)
                    {
                        msgScore += $"**{k}**   \t{string.Format("{0:#,0}", result)}\n";
                    }
                }

                Console.WriteLine($"{k} = {val}");
            }

            return (isReply, msgScore);
        }

        if (msg.ToLower().Contains("replyprompt"))
        {
            m = m.Remove("ReplyPrompt").Trim();
            replyPrompt = m + ":";
            if (replyPrompt.StartsWithAny(BotName)) replyPrompt = replyPrompt.Substring(BotName.Length + 1);
            return (true, "Reply prompt set as " + replyPrompt);
        }

        var dumb = BotName.ToLower().Contains("xib");

        // Feed GPT-3 history of longer length if we're asking it how old a chatter is, or if it remembers something in the chat
        var history = MessageHistory;
        if (!Personal(m))
            history /= 2;
        if (useSteps)
        {
           // history = 1;
        }
        /*if (Is("remember") || m.HasAny("chat log", "do you remember", "you remember", "what was the", "what did i",
                "how old", "what i said", "who has", "who is your", "what do you think of", "what is my", "who here",
                "who do", "users", "in this channel", "who is"))
            history = MessageHistoryLimit;*/

        //   if (user == "timmeh")
        //       MessageHistory = MessageHistory * 2;



        var writeStuff = IsWriting(m);
        var writeSong = (m.Contains("song") || m.Contains(" rap ") || m.Contains("lyrics ")) &&
                        !m.HasAny("mode2");


        if (!Personal(m) && writeStuff && !IsContinue(m) && !m.HasAny("mode2") && !isAutoMessage)
        { 
            
                queryOnly = true;
                history = 3;
        }

        if(!Personal(m) &&  m.Length > 50)
        {
            history = 0;
            queryOnly = true;
        }

     



        var suffix = IsQuestion(m)?$"{Program.NewLogLine}{BotName}:":$"{Program.NewLogLine}{BotName}:";
        // var suffix2 = IsQuestion(m)?"\nAnswer:":"";
        var prefix = "";
        //  if(!suffix.EndsWith("#"))
        if (writeSong)
        {
            suffix = $"{Program.NewLogLine}{BotName}: These are the lyrics I composed:";

            var rewrite = msg.HasAny("rewrite", "write it again", "more ", "less ", "try again", "write it", "change ", "longer", "shorter");
            if(!rewrite)
            {
               // prefix = "This is an award winning "
                queryOnly = true;
                history = 0;
            }
        }
        else if (writeStuff)
        {
            if (msg.Length > 30)
                queryOnly = true;
            //suffix2 = "\nWritten Answer:";
            suffix =
                $"{Program.NewLogLine}{BotName}'s lengthy, well thought out, brilliant, hilarious writing:";
            history = 0;
        }
        // else if (isQuestion)
        //      suffix = $"{Program.NewLogLine}dibbr's Answer:";
       // else
         //   suffix = $"{Program.NewLogLine}{BotName}'s response:";



        if (m.Contains("$1"))
        {
            suffix = $"{Program.NewLogLine}{BotName}:";
        }
        if (m.Contains("$2"))
        {
            suffix = $"{Program.NewLogLine}{BotName}'s Factual Answer:";
        }
        if (m.Contains("$3"))
        {
            suffix = $"{Program.NewLogLine}{BotName}'s Funny Answer:";
        }
        if (m.Contains("$4"))
        {
            suffix = $"{Program.NewLogLine}{BotName}'s Answer:";
        }
        if (m.Contains("$5"))
        {
            suffix = $"{Program.NewLogLine}{BotName}: I'll explain it like you're smart.";
        }
        msg = msg.Remove("$1").Remove("$2").Remove("$3").Remove("$4").Remove("$5");
        m = m.Remove("$1").Remove("$2").Remove("$3").Remove("$4").Remove("$5");
        if (Gpt3.engine.Contains("code"))
            suffix = "";

        if (isAutoMessage)
            suffix = $"{Program.NewLogLine}{BotName}:";
        // else { suffix = $"The following is {BotName}'s lengthy, funny, brilliant, multi-paragraph attempt:"; }

       
        Console.WriteLine(suffix);
        if (m.Contains("dump last query"))
            return (true, $"[{Gpt3.LastQuery}]");
        if (m.Contains("dump memory"))
            return (true, Memory);
        if (m.Trim() == "cost")
            return (true, Gpt3.Stats());


        if (useSteps) suffix += $"Let's think step by step and end with a conclusion.";
        if(m.HasAny("thoughts","you think","your thoughts","critique my","me feedback","response to my","to my","my message"))
        {
            suffix += "Here is my balanced critique of your message:";
        }


        if (m.Contains("max memory"))
        {
            MessageHistory = 20;
            return (isReply, "");//, "+MessageHistory+" lines"); //$"Memory boosted to {MessageHistory} log lines");
        }

        if (m.Contains("boost memory"))
        {
            MessageHistory = MessageHistory * 3;
            return (isReply, "");//, "+MessageHistory+" lines"); //$"Memory boosted to {MessageHistory} log lines");
        }

        if (m.Contains("shrink memory"))
        {
            MessageHistory = MessageHistory / 2;
            return (isReply, $"Memory shrank to {MessageHistory} log lines");
        }

        if (m.Contains("the way") || m.Contains("da wae"))
            return (true, "Do you need to find da wae? https://www.youtube.com/watch?v=MtzMUJIofNg");

        //   if (Die(AngerOdds)) suffix += $"{BotName}: [Bot State=Angry] ";
        //  if (user.ToLower().Contains("pep") && x++ < 44)
        //      return (isReply, "I said no, faggot boy");
        if (m.Contains("list "))
        {

            if (m.Contains("list2"))
            {
                m = "The purpose of this task is to create lists, as specified. e.g.\nQuestion: Write a list of fun places to eat and why\n" +
    "Answer: 1. In-N-Out Burger - because who doesn't love a good burger?2. Shake Shack - for the best shakes aroundn3. P.F. Chang's - because who doesn't love Chinese food?n4. The Cheesecake Factory - because who doesn't love cheesecake?n5. Chili's Grill & Bar - because who doesn't love chili?\n\nQuestion: " + m + "\nAnswer:";
                m = m.Replace("list2", "list");

                msg = m;
            }
            history = 0;



        }

        string Exec(string str)
        {
            var scriptOptions = ScriptOptions.Default;

            // Add reference to mscorlib
            var mscorlib = typeof(object).GetTypeInfo().Assembly;
            var systemCore = typeof(System.Linq.Enumerable).GetTypeInfo().Assembly;

            var references = new[] { mscorlib, systemCore };
            scriptOptions = scriptOptions.AddReferences(references);

            List<int> yList;
            using (var interactiveLoader = new InteractiveAssemblyLoader())
            {
                foreach (var reference in references)
                {
                    interactiveLoader.RegisterDependency(reference);
                }

                // Add namespaces
                scriptOptions = scriptOptions.AddImports("System");
                scriptOptions = scriptOptions.AddImports("System.IO");
                scriptOptions = scriptOptions.AddImports("System.Linq");
                scriptOptions = scriptOptions.AddImports("System.Collections.Generic");

                // Initialize script with custom interactive assembly loader
                var script = CSharpScript.Create(@"", scriptOptions, null, interactiveLoader);
                var state = script.RunAsync().Result;

                state = state.ContinueWithAsync(str).Result;
                return "NEW"+state.TextDump() + state.ReturnValue;


            }
        }
        string Exec2(string str)
        {
            return "";
            var libs = new string[] { "System", "System.Math", "System.IO", "System.Net" };
            var originalConsoleOut = Console.Out; // preserve the original stream
            var str2 = "";
            try
            {
                
                using (var writer = new StringWriter())
                {
                    Console.SetOut(writer);

                    var now2 = CSharpScript.EvaluateAsync(str, ScriptOptions.Default.WithImports(libs)).Result;
                    if (now2 != null)
                        Console.WriteLine(now2.ToString());

                    writer.Flush(); // when you're done, make sure everything is written out


                    str2 = writer.GetStringBuilder().ToString();
                }

                Console.SetOut(originalConsoleOut); // restore Console.Out
            }
            catch(Exception e)
            {
                Console.SetOut(originalConsoleOut);
                return "Exception:"+e.Message;
            }
           // Console.SetOut(originalConsoleOut); // restore Console.Out
            return str2;

        }
        if (m.StartsWithAny("execute","evaluate","run this","exec"))
        {
            var c = msg.AfterAll("execute","evaluate","run this","exec").Trim();
           
           
          //  return (true, Exec2(c));
           
           
        }
        if(msg.Contains("engine="))
        {
            UseNeo = false;
            useNeo = false;
        }
        if (msg.Contains("!!"))
        {
            msg = msg.Remove("!!");
            history = 0;
        }
        //  if(msg.Contains("remember "))
        if (msg.Contains("set memory"))
        {
            // if (!user.Contains("dabbr") && !user.Contains("dΦbbr"))
            //   return (true, "Denied!");
            Memory = msg.After("set memory");
            return (isReply, $"ok, i will remember that");
        }
        if (m.StartsWithAny("remember"))
        {
            // if (!user.Contains("dabbr"))
            //      return (true, "Denied!");
            Memory += ", " + msg.After("remember ");
            ConfigurationManager.AppSettings[Channel] = Memory;
            return (isReply, $"ok, i will remember that");
        }

        if (msg.HasAny("dump memory", "what do you remember", "recall")) { return (isReply, " Memory is " + Memory); }

        List<string> TrimLines(List<string> lines)
        {
            var newLines = new List<String>();
            for (int i = 0; i < lines.Count; i++)
            {
                string l = lines[i];

   
                
                if(( l.Length < 150) || i<lines.Count-1)
                    newLines.Add(l);
            }

            return newLines;
        }

        var startTxt = "";
      

      
        // This is begin bot reply mode, by inserting botname: at end of msg
        // Allows you to force a reply to start with given text
        idx = msg.ToLower().IndexOf($"{BotName.ToLower()}:");
        if (idx != -1)
        {
            startTxt = msg.After($"{BotName}:");
            suffix += startTxt;
            msg = msg.Substring(0, idx);
           // log = log.Remove($"{BotName}:" + startTxt);
            startTxt += " ";
        }

        // Make sure the chat line is in the log
      //  idx = log.IndexOf(msg);


        var codeMode = m.StartsWithAny("code ");

        
        bool Personal(string xt) => ConversationFactor > 0 && xt.HasAny("your","my", " you ", " we ", " our ") && !xt.HasAny("how do you","can you","said");
        string txt = "";
        //if (testRun) return (true, "Would Respond");

        Gpt3.memory = Memory;
       // useNeo = false;
        // Removes the reference to the bot in messages, just wasting processing power
        // log = log.Remove($"{BotName},").Replace($": {BotName}", ":");
        // Typing indicator before long process
        if (Client != null)
            Client.Typing(true);

        var debug = "";
        //  if (m.StartsWithAny("create")) return (isReply, "Create not supported by dibbr. Did you mean dalle create?");
        if (m.StartsWithAny("dalle create"))
        {
            //  if (user != "dabbr") return (isReply, "Not authorized");
            var cur = Blackboard.ContainsKey(user) ? Blackboard[user] : 0;

            if (cur > 3 && (user != "dabbr" && user != "edge_case")) return (isReply, "You are at your DALL-E quota!");
            Blackboard[user] = cur + 1;
            var req = m.After("dalle create ");
            txt = await Gpt3.Dalle(req);
            await Program.Log("DALLE_REQUESTS.txt", "\n\n\n" + txt);
            return (isReply, txt);
        }
        else if (DebugMode || writeSong || writeStuff)
        {
            txt = "[**First Attempt (Neo):**]\n";
            txt += (await Gpt3.Q2(msg.Remove("$$"), user)).Trim();
            if (txt.Length == 0)
            {
                txt = "[**Second Attempt (Neo):**]\n";
                Gpt3.pairs.Clear();
                txt += await Gpt3.Q2(msg.Remove("$$"), user);
            }
            txt += "\n [**Second Attempt (GPT3):**\n";

            if (queryOnly)// && !m.HasAny("your","you","my "," our ","thoughts"))
            {
                txt += await Gpt3.Ask((IsQuestion(msg) ? "Question " : "") + msg.Remove(BotName) + "\nAnswer: " + (useSteps ? ". Let's think in steps and end with our conclusion." : suffix), user);
            }

            else
            {
                // if(msg.Contains("dibbrjones"))
                //     Gpt3.
                txt += await Gpt3.Ask(MakeText(history, $"{user}:", suffix), msg, user, 2000, codeMode);

            }
            txt += "]";
        }
        else if (useNeo)
            txt = await Gpt3.Q2(msg.Remove("$$"), user);
        else if (queryOnly && !Personal(m))// && !m.HasAny("your","you","my "," our ","thoughts"))
        {
            txt = await Gpt3.Ask((IsQuestion(msg) ? "Question " : "") + msg.Remove(BotName) + "\nAnswer: " + (useSteps ? ". Let's think in steps and end with our conclusion." : ""), user);
        }

        else
        {
            // if(msg.Contains("dibbrjones"))
            //     Gpt3.
            txt = await Gpt3.Ask(MakeText(history, $"{user}:", suffix), msg, user, 2000, codeMode);
        }

        debug += $"\n[UseNeo={useNeo}, history={history},useSteps={useSteps},isSong={writeSong}, queryOnly={queryOnly}, query length={Gpt3.LastQuery.Length},IsQuestion={IsQuestion(m)},IsWriting={IsWriting(m)}] ";



        ////////////////////////////////////////////// Response Reply //////////////////////////////
        ///


        var (pct,tx) = txt.Deduplicate(string.Join("\n@@", Log.Last(4)));
        txt = tx;

        if (txt.Lame() && isAutoMessage)
            return (true, DebugMode ? "Returning because lame and automsg" : "");

        Console.WriteLine(txt.Length); ;

        // Q: blah
        // A: not sure
        //
        // Let's try stripping everything but the Q/A pair
        //
        if ((txt.Lame()||IsDupe(txt)))
        {
       
            if (m.Length <= "dibbr! ".Length || isAutoMessage)
                return (true, "[Debug: Dibbr chose to ignore this message. Was this correct?]");

            if (useNeo)
            {
                // try GPT3 - it tends to give less lame answers
                var txt2 = await Gpt3.Ask(MakeText(history, $"{user}:", suffix), msg, user, 2000, codeMode);
                if(txt2 is not null or "")
                {
                    debug += $"\n[Original: {txt}]";
                    txt = txt2 + " [GPT3]";
                   // return (true, txt);
                }
            }

                //  if (isQuestion && m.Length > 10)
                //      txt = await Gpt3.Ask("Question: "+msg.Remove("!!!").Remove(BotName) + (useSteps ? ".\n:Answer: Let's think in steps." : "\nAnswer:"), user);
                //  else

                // ZERO HISTORY
             if (IsQuestion(m) && m.Length > "What is the blahest bla".Length)
            {
                useSteps = true;
                var context = $"This is an interview between the world's smartest human, {user}, and the world's leading expert, who happens to be a superintelligent AI.";
                var txt2 = await Gpt3.Ask(MakeText(history: 0, suf: "Answer: Let's think in steps, and end with a conclusion.", context: context), msg, user, 2000, codeMode);
                int delame = 1;
                if (txt2.Lame() || IsDupe(txt2))
                {
                    txt2 = await Gpt3.Q2(msg, user);
                    delame++;
                }

                if (txt2.Length > txt.Length)
                {
                    debug += $" \n[Originaly answer Was Lame: {txt} ]";
                    txt = txt2 + $" [DeLamed {delame}]";
                }
                else
                {
                    debug += $"\n[Original answer was lame, but new query was lamer: '{txt2}']";
                    txt += $" [Delame failed]";
                }
            }
        }

       /* if (msg.HasAny("write","C#","code"))
        {
            var str = Exec2(txt);
            if (!str.Contains("Exception"))
                txt += "\nOutput: " + str;
        }
       */
        if (txt.Contains("skip", StringComparison.InvariantCultureIgnoreCase) && txt.Length < 10)
        {
            if (isAutoMessage)
                return (true, "[Debug: dibbr decided not to respond to this message]");
            else return (true, $"[Dibbr refuses to answer or engage with your message, {user}]");
        }

        //
        // Step 2 - Check for duplicate data
        //
       
        // If repetitive, try again

        if (IsDupe(txt))
        {
            if (isAutoMessage) return (true, "");
            Console.WriteLine("Trying again..");
            var txt2 = await Gpt3.Ask(MakeText(0));
            debug += $" [First Resposne Seemed Like Duplicate: '{txt}' ]";
            txt = txt2 + "\n[Original Was Duplicate: "+txt+"]";
            //   txt += "\nDev Note: Second response. First had a Levenshtein distance too high";
        }

       

 

        if (txt.IsNullOrEmpty() && isAutoMessage) return (isReply, DebugMode?debug:"");
        if (txt.IsNullOrEmpty() && !isAutoMessage)
        {
            // return (isReply, null);
        }


      /*  if (Lame(txt))
        {
            var m2 = msg.Remove("!!!").Remove(BotName).Remove("$$");
            var log2 = Log.Last(1)[0].After(":");
            var t = m2.Remove("!!!").Remove(BotName);//

            t = $"\nQuestions & Answers:\nQuestion (from {user}) :" + t;
            var txt2 = "";
            if (useNeo)
            {
                txt2 = await Gpt3.Ask(MakeText(), msg, user, 2000, codeMode);
               
            }
            else
            {
                useSteps = true;
                // Gpt3.pairs.Add("")
                txt2 = await Gpt3.Ask(msg.Remove("!!!").Remove(BotName) + (useSteps ? ".\n:Answer: Let's think in steps." : "\n"), user);
            }

            debug += $"\n[Original Answer Was Lame: {txt}, New Answer Lame={Lame(txt2)}]";
            //  if (Lame(txt2))
            //    txt = txt + " [OA: Same]";
            // else
            if (txt2.Length > txt.Length) txt = txt2;
           // txt = txt2 + "\nOA: " + txt + "";
        }*/

        txt = txt.Replace("@everyone", "(at)everyone");

        if (txt.Contains("no credits left"))
        {
            //log = "";
            Log.Clear();
            //txt = await Gpt3.Ask(MakeText(), msg);
        }



        // If the chat includes another chat starting, eg hi guys person: hey dibbr, filter it
        if (txt.Contains("dibbr:"))//cards",StringComparison.OrdinalIgnoreCase) && (txt.Contains(":") && txt.IndexOf(":") > txt.Length/2))
        {
            txt = txt.Substring(0, txt.IndexOf("dibbr:"));
            txt = txt.Sub(0, txt.LastIndexOf(" "));
        }

        new Thread(async delegate ()
        {
            //if (useNeo) return;
            if (true) return;
            if (Memory == null || Memory == "")
                Memory = ConfigurationManager.AppSettings[Channel];
            if (Memory != null && Memory.Length > 500)
                Memory = Memory[..500];

            if (Memory != "" && Memory != null)
            {
                try
                {
                    Program.Set(Channel, Memory);
                }
                catch (Exception e) { }
            }

            var newMemory = "";
            txt = txt;
           // if (0 == 1)
            //    newMemory = await Gpt3.Ask(MakeText() + txt +
             //                              "\nQ: what things do you want to remember if the chat log is wiped? Summarize the chat log in a few sentences. \nA:");
            if (newMemory.Length > 0)
            {
                if (newMemory.Length > 500)
                    newMemory = newMemory[..500];

                Memory = newMemory;
                Program.Set(Channel, Memory);
            }
        }).Start();

       


        // Bot may decide to not respond sometimes
    /*    var c = txt.ToLower().Remove(BotName).Remove(":").Trim();
        if (c.StartsWithAny("no response") || c.StartsWithAny("would not comment") || c.StartsWithAny("would not respond") ||
            c.StartsWithAny("no comment"))
        {
            Console.WriteLine("No response desired!!!!");
            return (true, "Dibbr did not wish to address your comment");
        }*/
       
        if (m.Contains("speak"))
        {
            txt = await GetAudio(startTxt + txt) + txt;

        }

        if (txt.ToLower().StartsWithAny("ignore"))
        {
            txt += $" [{BotName} has chosen to ignore your message. This message will disappear in 10 seconds]";
        }

  
        return (isReply, startTxt + txt + (DebugMode?debug:""));

        async Task<string> GetAudio(string str2)
        {
            HttpClient client = new HttpClient();

            var dict = new Dictionary<string, string>();
            dict.Add("service", "Polly");
            dict.Add("voice", "Brian");
            dict.Add("text", str2);


            var req = new HttpRequestMessage(HttpMethod.Post, "https://lazypy.ro/tts/proxy.php")
            {
                Content = new FormUrlEncodedContent(dict)
            };
            var res = await client.SendAsync(req);
            var str = await res.Content.ReadAsStringAsync();
            idx = str.IndexOf("http");
            if (idx != -1)
            {
                var url = str.Substring(idx);
                url = url.Substring(0, url.IndexOf("\""));
                return $"[Spoken Audio]({url}) ";

            }
            return "";
        }
        string MakeText(int history, string prefix="", string suf="",string context="")
        {
            if (Gpt3.engine.Contains("code")) return msg.Remove("dibbr");// + suffix;
            var warn = ""; //" (NOTE, If a leading question implies a fact, dibbr should not assume it is true) ";

            if (context is null or "")
                context = PrimeText;

            if (suf is null or "")
                suf = suffix;

          //  context = "This is a conversation with the world's smartest humans and an AI, dibbr. dibbr thinks in steps, gives long responses and knows everything. Dibbr is one of our brightest minds, hilarious and sarcastic.";

            
            var log1 = Log.Last(history);

            // Remove the user query from the log, because we add it back later
            if (log1.Count > 1)
                log1.RemoveAt(log1.Count - 1);

            // Make it into a string
            var log = string.Join("\n@@", TrimLines(log1.Last(history))).Replace(BotName,"",StringComparison.InvariantCultureIgnoreCase);

            if (log.Length > 2000)
                log = log;


            if (log.Length > 1000)
                log = log[^1000..].Trim();
           // if(!isAutoMessage)
             //   log += "\n-------------Question & Answer with dibbr----------------------\n";

           //  debug += $"\n[log.size={log.Length}] ";


            // var idx = log.LastIndexOf(msg.Sub(0, 20));
            if (IsQuestion(m) && !isAutoMessage)
            {
                log += $"\n@@Question: (from {user}): {msg}";
               // $"\n@@Answer (from {BotName}):";
            }
            else
            {
                log += $"\n@@{user}: {msg}";
               // log += $"\n@@{BotName}:";
            }
            log += suffix;
            // log += $"Question: (from {user}): {msg}\n"; // Program.NewLogLine;

            // if (isQuestion)
            //     log += user + ": Question: " + msg + "\nAnswer:";

            return $"Date: {DateTime.Now}\nThe following is a conversation between {BotName} and humans.\n {context}.\nMemory: {Memory}\nConversation:\n {log}";


            return "Date: " + DateTime.Now.ToString("F") + " PST.\n Context:" + PrimeText + "\nMemory: " + Memory + "\nConversation (format is name: message):\n" +
                   log + warn + suffix;
        }
    }

    public Dictionary<string, int> Blackboard = new Dictionary<string, int>();
    public int MessageCount { get; set; }
}

static class StringHelp2
{
    public static bool HasAny(this string str, params string[] values) => Has(str,1, values);

    public static bool Has(this string str, int numMatch = 1, params string[] values)
    {
        var strl = str;
        var count = 0;
        foreach (string value in values)
        {
            // String: 'hey dibbr, how r u'
            // ' hey' -> Matches b/c startswith case. so space at start says essentially 'space or start'
            if (strl.Contains(value, StringComparison.InvariantCultureIgnoreCase) || strl.Equals(value, StringComparison.InvariantCultureIgnoreCase)
                || strl.StartsWith(value.Trim(), StringComparison.InvariantCultureIgnoreCase)) count++;

            if (count >= numMatch)
                return true;
        }

        return false;
    }
    public static bool StartsWithAny(this string str, params string[] values)
    {
        var strl = str.Trim(new char[] {'\t',' ',',','\n','.','$','#','!'});
        foreach (string value in values)
        {
            if (strl.StartsWith(value,StringComparison.InvariantCultureIgnoreCase)) return true;
        }

        return false;
    }
}