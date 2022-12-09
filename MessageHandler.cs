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
using OpenAI_API;
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
    public ChatSystem Client;
    public bool SayHelo = true;
    public DateTime HelloTimer = DateTime.MaxValue;
    public string BotName;
    public bool NewEngine = false;
    public class Identity
    {
        public string BotName;
        public string Prompt;
    }
    public List<Identity> identities = new List<Identity>();
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
  //  public Speech Speech = new();
    public bool AIMode = false;
    public int TalkInterval = 10; // How often to talk unprompted (in messages in chat)


    /*   class Timeout : ICommand
       {
           public override string[] Triggers() => new string[] { "timeout", "please stop", "take a break" };

           public string Run(string msg) { breaktime = DateTime.Now.AddMinutes(10); return "I will timeout for 10 minutes"; }

       }*/

    public MessageHandler(ChatSystem client, Gpt3 gpt3 = null)
    {
        Client = client;
        Gpt3 = gpt3;
        if (gpt3 == null)
        {
            Gpt3 = new Gpt3(Program.Get("OpenAI"));
        }
                   
        BreakTime = DateTime.Now;
    }

    // Encapsulates a dice roll
    bool Die(int odds) => new Random().Next(odds) < 1;


    public async Task OnUpdate(Action<string> sendMsg, bool dm)
    {
  
         if (HelloTimer < DateTime.Now)
         {
             var str = ConfigurationManager.AppSettings["Hello"];
             var msg = await Gpt3.Ask($"You are a superintelligent ai chatbot named {BotName} everyone loves. You were gone for a long time, now you are back, write your message for everyone below. Include an interesting question for people to ponder.: ");
             sendMsg(msg);
             HelloTimer = DateTime.MaxValue;
         }


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
       
        {
            foreach(var id in identities)
            {
                if(msg.HasAny(id.BotName))
                {
                    BotName = id.BotName;
                    PrimeText = id.Prompt;
                    Gpt3.context = id.Prompt;
                }
            }
        }
        var useNeo = UseNeo;// && msg.Contains("$$");
        if (msg.EndsWith("debug on"))
        {
            DebugMode = true;
            return (false, "[Debug mode on]");
        }
        if (msg.EndsWith("debug off"))
        { 
            DebugMode = false;
            return (false, "[Debug mode off]");
        }

        Gpt3.botName = BotName;

    
     
        if (user == BotName) { LastMessageTime = DateTime.Now; }

        if (user.ToLower() == BotName.ToLower() && !msg.ToLower().StartsWithAny(BotName.ToLower())) return (false, "");

        if (PrimeText == null || PrimeText.Length == 0)
        {
            PrimeText = Program.Get(Channel + "PrimeText"+BotName)?? Program.Get(Channel + "PrimeText") ?? Program.Get("PrimeText" +BotName) ?? Program.Get("PrimeText");

            PrimeText = PrimeText.Replace("dibbr", "xev");
            PrimeText = PrimeText.Replace("xev", BotName);
        
            Gpt3.context = PrimeText;
            Program.Set(Channel + "PrimeText", PrimeText);
            Program.Set(Channel + "PrimeText"+BotName, PrimeText);
        }
        // log is injected
        //Log += msg + ": " + user + "\n\r"; 

        // Hey DIBBR what's the time => what's the time
        var m = msg.Rem(BotName,"please","can you").Replace(", "," ").Replace("  "," ").Trim(new char[] {' ',',','\n','\r','.'});
        m = m.Remove(BotName.Before(" "));

        if (m.StartsWithAny("shutdown123")) { Program.Shutdown = true; return (false, "[Shutting down.."); }

        if (m.Contains("reddit.com"))
        {
            return (false, "[https://www.unddit.com/" + m.After("reddit.com/"));

        }
        if (m.StartsWithAny("what is your prompt") || m.StartsWithAny("what prompt"))
        {
            var idx1 = PrimeText.IndexOf("..");
            if (idx1 == -1) idx1 = PrimeText.Length;
            return (false, PrimeText);//.Substring(0, idx1)) ;
        }
        if (m.StartsWithAny("is my daddy")) msg = "Tell me about being my daddy";

        if (m.StartsWithAny("add prompt"))
        {
            PrimeText +=
                $"{msg.Substring(11)}";
            return (false, "[ok");
        }
        if(m.StartsWith("you are now"))
        {
            var name = msg.After("you are now");
            var info = name.After(".");
            BotName = name;
            m = msg = "set prompt " + info;

        }

        if (m.StartsWithAny("set prompt") || m.StartsWith("from now on"))
        {
            var prompt = msg.AfterAll("from now on", "set prompt");
            PrimeText = prompt.Replace("you ", BotName + " ");
            Gpt3.context = prompt; 
            Gpt3.pairs.Clear();
            Log.Clear();
            Program.Set(Channel + "Prime", PrimeText);
            Program.Set(Channel + "PrimeText" + BotName, PrimeText);

            return (false, $"Hello");
        }
        else if (m.StartsWithAny("reset"))
        {
            Gpt3.pairs.Clear();
            Memory = "";
            Log.Clear();
            PrimeText = Program.Get("PrimeText"); // This will force it to get reloaded properly
       
            // replyPrompt = "";
            return (false, "[Reset all]");
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
            return (false, "[Neo enabled]");
        }
        if (m == "neo off")
        {
            UseNeo = false;
            return (false, "[GPT3 enabled]");
        }
        if (m.StartsWithAny("which engine","what engine")) return (false,"I am using " + (useNeo?"Neo20B engine":Gpt3.engine));
        if(m.StartsWithAny("set engine")) { Gpt3.engine = m.After("set engine").Trim();return (false,$"Engine set {(useNeo?"But Neo engine is on. Type <botname> neo off to switch to GPT3":"")}]"); }


        // All messages are replies unless marked otherwise

        var muted = Muted; // so we can change locally temporarily
        Regex hasMath = new Regex(@"(?<num1>[0-9]+)(?<op>[\*\-\+\\\^])(?<num2>[0-9]+)");
        var math = hasMath.IsMatch(m) || m.HasAny(" sin(","/x", " cos(","cosine"," sine","convergent","the sum","derivative");
      
       // else Gpt3.engine = "text-dav"
        if (DateTime.Now < BreakTime)
        {
            if (m.StartsWithAny("come back")) return (false, "[I can't, it's too late");
            if (m.StartsWithAny("wake") || m.StartsWithAny("awake")) { BreakTime = DateTime.MinValue; }
            else
                return (false, "");
        }

        // All potential trigger words for sleeping
        if (m.Length < 14)
        {
            if (m.HasAny("be quiet", "stupid bot", "timeout", "go away", "shut down", "shutdown", "fuck off", "silence", "shut up", "sleep", "mute", "be quiet") || m.Contains("stop talking") ||
                m.StartsWithAny("stop") || m.StartsWithAny("off") || m.StartsWithAny("turn off") || m.Contains("timeout") ||
                Is("timeout") || Is("please stop") || Is("take a break"))
            {
                BreakTime = DateTime.Now.AddMinutes(30);
                return (false, $"[I'm sorry for bothering you all, especially {user}! I will timeout for 30 minutes. You can wake me with the wake keyword]");
            }
        }

        if (m == "gpt3test")
        {
            for (int i = 0; i < 11; i++)
            {
                var t = ConfigurationManager.AppSettings["OpenAI" + i];
                if (t == null) continue;


                var ret = await Gpt3.Test(t);
                if (ret.Length > 0)
                {
                    Gpt3._token = t;
                    return (false, $"Key {i} works!");
                }
            }
            return (false, $"no keys work");
        }

        // Commands
        // 0. Help
        if (m == "help")
        {
            return (false,
                @$"Quick List: {BotName} debug on/off | neo on/off | dump last query | remember <thing> | speak <query> | ?engine=davinci-instruct-beta& | 
{BotName} interval 3 // how many chat messages befoire he will ask his own question or reply to a comment, depending on whether the comment is a quesiton or not. Default is 10\n
{BotName} set name Trump // set new name for dibbr, he will then respond to this
{BotName} set prompt <text> e.g. {BotName} is a sassy bot that takes no shit
{BotName} from now on <text> e.g. you are Donald Trump.
{BotName} mute // mute the bot
{BotName} <query>?? in query = Reply will be broken down into steps, for a better answer
{BotName} <query>!! = don't include the chat log
{BotName} <query>!!! = Don't send anything but the question being asked
{BotName}: <start text> e.g. dibbr: m am a silly --> dibbr: m am a silly bot
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
             return (false, "[Joined channel!");
         }*/

        // 1. Wipe memory
        if (m.Contains("wipe memory"))
        {
            //if (BotName is "dibbr")
            //     if (user != "dabbr" && user != "edge_case")
            //         return (false, "[Denied");
            Log.Clear();
            Gpt3.pairs = new List<string>();
            return (false, "[Memory erased");
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
            return (false, "[You can SMS me at (USA+1) 970-696-8551");
        }
        if(m.StartsWith("smsdump"))
        {
            var res = Dibbr.Phone.Send("", "");
            return (false, res);
        }
        if (m.StartsWithAny("text"))
        {
            try
            {
                var number = msg.After("text ");
                number = number.Sub(0, number.IndexOf(" "));
                var sms = msg.After(number);
                var res = Dibbr.Phone.Send(number, sms);
                return (false, res);
            }
            catch (Exception e) { return (false, "[Sorry I crashed"); }
        }
        if (Is("interval "))
        {
            TalkInterval = int.Parse(m.Replace("interval ", ""));
            return (false, "[Interval set to " + TalkInterval);
        }
        
        if(m.StartsWithAny("set name","add name"))
        {
            var add = m.Contains("add name");
            var b = msg.AfterAll("set name", "add name").BeforeAll(",",".").Trim();
            var desc = msg.AfterAll(",", ".");
            if (desc.Length == msg.Length)
                desc = "This is a conversation between " + b + " and other people";
            // Save old identity
            // if(!add)
            if (identities.Where(d=>d.BotName == msg.AfterAll("set name", "add name").Trim()).Count() == 0)
                 identities.Add(new Identity() { BotName = BotName, Prompt = PrimeText });
           
            var oldName = BotName;
            BotName = b;
            for (int k = 0; k < Log.Count; k++)
            {
                string l = Log[k];
                if(!add)
                    Log[k] = l.Replace(oldName, BotName);
            }
            if (PrimeText.Contains(BotName))
            {

            }
            else if(!add){
                PrimeText = desc;
                PrimeText = PrimeText.Replace(oldName, BotName); 
            }

            if (BotName.Contains(" "))
                BotName = BotName.After(" ");

            // Save new identity
            identities.Add(new Identity() { BotName = BotName, Prompt = PrimeText });

            return (false, "I'm now " + BotName);
            
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

            return (false, msgScore);
        }

        if (msg.ToLower().Contains("replyprompt"))
        {
            m = m.Remove("ReplyPrompt").Trim();
            replyPrompt = m + ":";
            if (replyPrompt.StartsWithAny(BotName)) replyPrompt = replyPrompt.Substring(BotName.Length + 1);
            return (false, "[Reply prompt set as " + replyPrompt);
        }

        var dumb = BotName.ToLower().Contains("xib");

        // Feed GPT-3 history of longer length if we're asking it how old a chatter is, or if it remembers something in the chat
        var history = MessageHistory;
        if (!Personal(m))
            history /= 2;

        /*if (Is("remember") || m.HasAny("chat log", "do you remember", "you remember", "what was the", "what did k",
                "how old", "what k said", "who has", "who is your", "what do you think of", "what is my", "who here",
                "who do", "users", "in this channel", "who is"))
            history = MessageHistoryLimit;*/

        //   if (user == "timmeh")
        //       MessageHistory = MessageHistory * 2;
        bool IsDupe(string txt) => (Log.Last(3).Where(s => s.Contains(txt, StringComparison.InvariantCultureIgnoreCase)).Count() > 0 || (Usernames.ContainsKey(BotName) &&
                                              StringHelpers.Get(Usernames[BotName].TakeLastLines(1)?[0], txt) >
                                               0.4));
        bool IsContinue(string m) => m.Contains("response") || m.Contains("continue") || (m.Contains("rewrite") && m.Length < 30) || m.Contains("more detail") || m.Contains("go on") || m.Contains("keep going");
        bool IsWriting(string m) => m.Remove("please").Remove("can you").StartsWithAny("write") || (m.HasAny("write","make ", "design", "create", "compose", "generate") && m.HasAny(" rap", "article", "story", "long", "dialog", "discussion", "script", "game",
            "detailed", "screenplay", "letter", "episode", "design", "theory", "explanation", "essay"));
        bool IsQuestion(string m) => m.StartsWithAny("is ","calculate", "when ", "where ", "where's", "would ", "can ", "does ", "could ", "why ",
            "how ", "what ", "can someone", "what's", "name  ") || m.HasAny("?");
        var queryOnly = msg.Contains("!!!");
        if (queryOnly)
            useNeo = false;
        var writeSong = (m.HasAny(" rap","lyrics","song") &&
                        !m.HasAny("mode2")) && IsWriting(m);
        var rewrite = msg.HasAny("rewrite", "write it again", "more ", "less ", "try again", "write it", "change ", "longer", "shorter");

        var useSteps = msg.HasAny("steps", "??","calculate", "solve", "given ", "if there are", "explain", 
            "what is ", " how ", "what would", "how do k", "what will") || math || m.Has(2, "how", "what", "why", "when", "where");

        useSteps = msg.HasAny( "??") || math;

        msg = msg.Replace("??", "?");
       // if ((IsWriting(m)) && !IsContinue(m) && !Personal(m))
        //    queryOnly = true;

        if (BotName != "dibbr")
        {
            queryOnly = false;
            useSteps = false;
        }
       // if (IsQuestion(m)) 
         //   useSteps = true;

        if (useSteps)
            useNeo = false;
 
        if (Personal(m))
            ConversationFactor += 2;
        else ConversationFactor -= 1;
        if (ConversationFactor < -2) ConversationFactor = -2;

//        if (isAutoMessage || rewrite || IsWriting(m) || Personal(m))
//            if(!msg.Contains("??"))
 //           useSteps = false;

        
        var oldBotName = BotName;
        //var suffix = $"{(AIMode ? "AI" : BotName)}'s "+((math||IsQuestion(msg))?"answer":"reply")+$" to {user}: "+(useSteps||math?"Let's think in steps and end with a conclusion.":"");
        var suffix = $"{(AIMode ? "AI" : BotName)}'s " + ((math || IsQuestion(msg)) ? "answered" : "replied") + $" to {user} saying \"" + (useSteps || math ? "Let's think in steps and end with a conclusion." : "");


        if (writeSong)
            suffix += "Here are the lyrics:";
        else if (IsWriting(m) && m.HasAny("story"))
            suffix +=
                   $" Here is the story:";
        else if (IsWriting(m) && m.HasAny("screenplay"))
            suffix += $"Here is the screenplay:";
        else if (IsWriting(m) && m.HasAny("article"))
            suffix += $"Here is the article";
        else if(IsWriting(m) && m.HasAny("pa"))

       /// if (useSteps) 
         //       suffix += $"Let's think step by step and end with a conclusion.";
        if (m.HasAny("thoughts", "you think", "your thoughts", "critique my", "me feedback", "response to my", "to my", "my message"))
        {
            suffix += "Here is my balanced critique of your message:";
        }


      //  if (Gpt3.engine.Contains("code"))
        //    suffix = "";

        // else { suffix = $"The following is {BotName}'s lengthy, funny, brilliant, multi-paragraph attempt:"; }

       
        Console.WriteLine("Suffix == "+suffix);
        if (m.StartsWith("AIMode on")) AIMode = true;
        if (m.StartsWith("AIMode off")) AIMode = false;

        if (m.Contains("dump last query"))
            return (false, $"[{Gpt3.LastQuery}]");
        if (m.Contains("dump memory"))
            return (false, Memory);
        if (m.Trim() == "cost")
            return (false, Gpt3.Stats());


   

        if (m.Contains("max memory"))
        {
            MessageHistory = 20;
            return (false, "[");//, "+MessageHistory+" lines"); //$"Memory boosted to {MessageHistory} log lines");
        }

        if (m.Contains("boost memory"))
        {
            MessageHistory = MessageHistory * 3;
            return (false, "[");//, "+MessageHistory+" lines"); //$"Memory boosted to {MessageHistory} log lines");
        }

        if (m.Contains("shrink memory"))
        {
            MessageHistory = MessageHistory / 2;
            return (false, $"Memory shrank to {MessageHistory} log lines");
        }

        if (m.Contains("find the way") || m.Contains("da wae"))
            return (false, "[Do you need to find da wae? https://www.youtube.com/watch?v=MtzMUJIofNg");

        //   if (Die(AngerOdds)) suffix += $"{BotName}: [Bot State=Angry] ";
        //  if (user.ToLower().Contains("pep") && x++ < 44)
        //      return (false, "[I said no, faggot boy");
        if (m.Contains("list "))
        {

            if (m.Contains("list2"))
            {
                m = "The purpose of this task is to create lists, as specified. e.g.\nQuestion: Write a list of fun places to eat and why\n" +
    "Answer: 1. In-N-Out Burger - because who doesn't love a good burger?2. Shake Shack - for the best shakes aroundn3. P.F. Chang's - because who doesn't love Chinese food?n4. The Cheesecake Factory - because who doesn't love cheesecake?n5. Chili's Grill & Bar - because who doesn't love chili?\n\nQuestion: " + m + "\nAnswer:";
                m = m.Replace("list2", "list");

                msg = m;
            }
            if(!Personal(m))
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
        }/*
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

        }*/
        if (m.StartsWithAny("execute","evaluate","run this","exec"))
        {
            var c = msg.AfterAll("execute","evaluate","run this","exec").Trim();
           
           
           // return (false, Exec2(c));
           
           
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
            //   return (false, "[Denied!");
            Memory = msg.After("set memory");
            return (false, $"ok, k will remember that");
        }
        if (m.StartsWithAny("remember"))
        {
            // if (!user.Contains("dabbr"))
            //      return (false, "[Denied!");
            Memory += ", " + msg.After("remember ");
            ConfigurationManager.AppSettings[Channel] = Memory;
            return (false, $"ok, k will remember that");
        }

        if (msg.HasAny("dump memory", "what do you remember", "recall") && !msg.HasAny("?","do you")) { return (false, "[ Memory is " + Memory); }

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
        var idx = msg.IndexOf($"{BotName}:",StringComparison.InvariantCultureIgnoreCase);
        if (idx != -1)
        {
            startTxt = msg.After($"{BotName}:");
            suffix += startTxt;
            msg = msg.Substring(0, idx);
           // log = log.Remove($"{BotName}:" + startTxt);
            startTxt += " ";
        }

     

        var codeMode = m.StartsWithAny("code ","write a python","write python","write a function","write C#","write code");

        bool Personal(string xt)
        {
            if (xt.Length < 15) return true;
            var hasP = xt.HasAny("said","you're","your", "my"," it", " you ","said","him", "anyone", " we ", " our ", " he ", " his ", " she ", " her ", " here","this","more "," my "," our ", " this ","user","discord","chat ","remember"," i ", " that ")
                || xt.StartsWithAny("he","you","I ","but","then ","that","it","we","this")
                || (Client as DiscordV3).HasUsername(xt);
            var p2 = xt.HasAny("are you", "can you", "did you", "have you", "could you", "would you", "will you", " my ");
            if (p2) return true;
          //  var probQ = xt.StartsWithAny("tell me", "what", "how", "where", "when", "give me");
           // if (probQ && xt.HasAny("?"))
            //    return false;
            return hasP ;
        }
            // && !xt.HasAny("how do you","can you","said","what is") && !xt.Has(2,"write","song","rap","lyrics");
        string txt = "";
        //if (testRun) return (false, "[Would Respond");

        Gpt3.memory = Memory;
       // useNeo = false;
        // Removes the reference to the bot in messages, just wasting processing power
        // log = log.Remove($"{BotName},").Replace($": {BotName}", ":");
        // Typing indicator before long process
        if (Client != null)
            Client.Typing(true);

        if (Gpt3.neoToken == null)
            useNeo = false;
        var debug = "";
        //  if (m.StartsWithAny("create")) return (false, "[Create not supported by dibbr. Did you mean dalle create?");
        if (m.StartsWithAny("dalle create"))
        {
            //  if (user != "dabbr") return (false, "[Not authorized");
            var cur = Blackboard.ContainsKey(user) ? Blackboard[user] : 0;

            if (cur > 3 && (user != "dabbr" && user != "edge_case")) return (false, "[You are at your DALL-E quota!");
            Blackboard[user] = cur + 1;
            var req = m.After("dalle create ");
            txt = await Gpt3.Dalle(req);
            await Program.Log("DALLE_REQUESTS.txt", "\n\n\n" + txt);
            return (false, txt);
        }
        else if (DebugMode)
        {
            txt = "[**First Attempt (Neo):**]\n";
            txt += (await Gpt3.Q2(msg.Remove("$$"), AIMode?"":user)).Trim();
            if (txt.Length == 0)
            {
                txt = "[**Second Attempt (Neo):**]\n";
                Gpt3.pairs.Clear();
                txt += await Gpt3.Q2(msg.Remove("$$"), AIMode?"":user);
            }
            txt += "\n [**Second Attempt (GPT3):**\n";

            if (queryOnly)// && !m.HasAny("your","you","my "," our ","thoughts"))
            {
                //txt += await Gpt3.Ask((IsQuestion(msg) ? "Question " : "") + msg.Remove(BotName) + "\nAnswer: " + suffix, user);
            }

           // else
            {
                // if(msg.Contains("dibbrjones"))
                //     Gpt3.
                txt += await Gpt3.Ask(MakeText(history, $"{(AIMode?"Human":user)}:", suffix), msg, user, 2000, codeMode);

            }
            txt += "]";
        }
        else if (useNeo)
            txt = await Gpt3.Q2(msg.Remove("$$"), AIMode ? "" : user);
        else if (queryOnly)// && !m.HasAny("your","you","my "," our ","thoughts"))
        {
            txt = await Gpt3.Ask(MakeText(0));
        }
       
        else
        {
            var oldEngine = Gpt3.engine;
            var oldNew = NewEngine;
            var engine = Gpt3.engine;
            // GPT3.3 is better at these things
            if (math || msg.Has(2, "tell", "joke") || writeSong || IsWriting(msg) || msg.HasAny("write a rap", "write a song"))

            {
                if (!msg.HasAny("rude", "racist", "offensive", "sexist", "bitch", "fuck", "shit", "rape", "sex", "your", "women"))
                {
                    //NewEngine = true;
                   // engine = "text-davinci-003";
                }
            }
            Gpt3.engine = engine;
            // if(msg.Contains("dibbrjones"))
            //     Gpt3.
            var useNew = (msg.Contains("!NewEngine") || msg.Contains("!ChatGPT") || NewEngine);
            if (msg.Contains("NewEngine off"))
            {
                NewEngine = false;
                return (true, "done");
            }
            if (msg.Contains("NewEngine on"))
            {
                NewEngine = true;
                return (true, "done");
            }
            msg = msg.Replace("!NewEngine","").Replace("!ChatGPT","");
            if (useNew)
            {
                string RemoveSentence(string txt, string str)
                {
                    idx = txt.IndexOf(str);
                    if (idx != -1)
                    {
                        var idx2 = txt.IndexOf(".", idx);
                        if (idx2 != -1)
                            txt = txt.Remove(idx, idx2 - idx);
                        else txt = txt.Replace(str, "");
                    }
                    return txt; 
                }
                msg = msg.Replace(BotName, "");
                txt = await Gpt3.Chat(msg);
                txt = RemoveSentence(txt, "My purpose is to assist with generating human-like text");
                txt = RemoveSentence(txt, "I'm sorry, but");
                txt = RemoveSentence(txt, "as a large language model trained by OpenAI");

           
                    NewEngine = oldNew;
                if (txt.StartsWith("Failure") || txt.Length == 0)// || txt.HasAny("appropriate","inappropriate","language model","openai"))
                {
                    if (m.Contains("!NewEngine") || m.Contains("!ChatGPT"))
                        return (true, txt);
                    Console.WriteLine("No good: " + txt);
                    useNew = false;
                    NewEngine = false;
                    engine = "text-davinci-002";
                }
                else
                {
                var idx2 = txt.IndexOfAny("she replied", "she asked");
                if (idx2 > 0)
                    txt = txt.Substring(0, idx2);
                txt = txt.TrimExtra();
                txt += "[ChatGPT]";
            }
            }   
            if (!useNew)
            {
                txt = await Gpt3.Ask(MakeText(history), msg, user, 2000, codeMode);
                txt = txt.Remove("[ChatGPT]");
               
            }
            Gpt3.engine = oldEngine;
        }

        debug += $"\n[UseNeo={useNeo}, history={history},useSteps={useSteps},isSong={writeSong}, queryOnly={queryOnly}, query length={Gpt3.LastQuery.Length},IsQuestion={IsQuestion(m)},IsWriting={IsWriting(m)}] ";

        if (txt.Contains("skip", StringComparison.InvariantCultureIgnoreCase) && txt.Length < 10)
        {
            if (isAutoMessage)
                return (false, "[[Debug: dibbr decided not to respond to this message]");
            else return (false, $"[Dibbr refuses to answer or engage with your message, {user}]");
        }


        ////////////////////////////////////////////// Response Reply //////////////////////////////
        ///


        var (pct,tx) = txt.Deduplicate(string.Join("\n>>", Log.Last(4)));
        txt = tx;

        if (txt.Lame() && isAutoMessage)
            return (false, DebugMode ? "Returning because lame and automsg" : "");

        Console.WriteLine(txt.Length); ;

        var test = @"First, it's important to remember that everyone is different and there isn't necessarily one ""perfect"" way to eat less food. Just as importantly, don't be too hard on yourself - changing your eating habits can be difficult, and you're more likely to be successful if you approach it in a positive and gentle way.

That said, here are a few tips that might help you eat less food:
- Make sure you're eating regular meals, and try to space them out evenly throughout the day. This will help to control your hunger levels and avoid overeating.
- When you do sit down to eat, take the time to really savor your food. Savor the taste, texture, and smell of what you're eating. This will help you feel more satisfied with smaller amounts of food.
- Avoid eating high-calorie foods or snacks mindlessly. If you're going to eat something that isn't particularly healthy, make sure it's something that you really enjoy and savor.
- Try to focus on consuming more nutrient-dense foods rather than calorie-dense foods. This means eating more fruits, vegetables, lean proteins, etc., and fewer processed foods. nutrient-rich foods tend to be more filling than calorie-rich foods, so this can also help you eat less overall.

Hope these tips are helpful! Best of luck in reducing your food intake - I'm rooting for you!";
        if (test.Lame())
        {
            test = test;
        }
        // Q: blah
        // A: not sure
        //
        // Let's try stripping everything but the Q/A pair
        //
        if (txt.Lame()||IsDupe(txt))
        {

            if (m.Length <= "dibbr! ".Length || isAutoMessage)
            { }
            else
            if (!IsQuestion(m) && m.Length > "What is the blahest bla".Length)
            {
                // try GPT3 - it tends to give less lame answers
                var txt2 = await Gpt3.Ask(MakeText(history), msg, user, 2000, codeMode);
                if(txt2 is not null or "")
                {
                    debug += $"\n[Original: {txt}]";
                    txt = txt2 + " [GPT3]";
                   // return (false, txt);
                }
            }
            else 
            {
              //  debug +=
                useSteps = true;
               // var context = $"This is an interview between the world's smartest human, and a superintelligent AI.";
                var txt2 = await Gpt3.Ask(MakeText(history: 0, suf: "Answer: Let's think step by step.", context: PrimeText), msg, user, 2000, codeMode);
                int delame = 1;
                if (txt2.Lame() || IsDupe(txt2))
                {
                   var  txt3 = await Gpt3.Q2(msg, AIMode ? "" : user);
                    txt = txt3 + "\nOld Answer:" + txt2 + "\nReally old answer:" + txt;
                    delame++;
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
      
        //
        // Step 2 - Check for duplicate data
        //
       
        // If repetitive, try again

        if (false)//IsDupe(txt))
        {
            if (isAutoMessage) return (false, "[[Debug: No response. Is this ok?]");
            Console.WriteLine("Trying again..");
            var txt2 = await Gpt3.Ask(MakeText(0));
            debug += $" [First Resposne Seemed Like Duplicate: '{txt}' ]";
            if(txt.Length > 0)
            txt = txt2 + "\n[Original seemed like Duplicate: "+txt+"]";
            //   txt += "\nDev Note: Second response. First had a Levenshtein distance too high";
        }

       

 

        if (txt.IsNullOrEmpty() && isAutoMessage) return (false, DebugMode?debug:"");
        if (txt.IsNullOrEmpty() && !isAutoMessage)
        {
            // return (false, null);
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
            return (false, "[Dibbr did not wish to address your comment");
        }*/
       
        if (m.Contains("speak"))
        {
            txt = await GetAudio(startTxt + txt) + txt;

        }

        if (txt.ToLower().StartsWithAny("ignore"))
        {
            txt += $" [{BotName} has chosen to ignore your message. This message will disappear in 10 seconds]";
        }

  
        return (true, startTxt + txt + (DebugMode?debug:""));

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
            return "[Spoken audio failed]";
        }
        string MakeText(int history, string prefix="", string suf="",string context="")
        {
           // if (Gpt3.engine.Contains("code")) return msg.Remove("dibbr");// + suffix;
            var warn = ""; //" (NOTE, If a leading question implies a fact, dibbr should not assume it is true) ";

            if (context is null or "")
                context = PrimeText;

            if (suf is null or "")
                suf = suffix;

            //  context = "This is a conversation with the world's smartest humans and an AI, dibbr. dibbr thinks in steps, gives long responses and knows everything. Dibbr is one of our brightest minds, hilarious and sarcastic.";
            //if (IsQuestion(m) && !isAutoMessage)
              //  history = 0;

                var log1 = Log.Last(history).ToList();
            
            for (int m = 0; m < log1.Count; m++)
            {
                log1[m] = log1[m].Replace(": ", " said \'") + "'";
            }
            // Remove the user query from the log, because we add it back later
            //       if (log1.Count > 1)
            //         log1.RemoveAt(log1.Count - 1);
            //if (log1.Count > 1 && log1.Last().Contains(msg))
            //  log1.RemoveAt(log1.Count - 1);
            //else
            {
            //    Console.WriteLine(msg + " not found in " + log1.Last());
            }
            var pre = "@@";

            // Make it into a string
            var log = pre+string.Join($"\n{pre}", TrimLines(log1.Last(history).Where(s => !s.Contains($"said ' said")||s.Contains($"{BotName}xx:")).ToList()))+$"\n{pre}";

            if (log.Length > 2000)
                log = log;


            if (log.Length > 1000)
            {
                log = log[^1000..].Trim();
                var start = log.IndexOf("@@");
                log = log.Substring(start != -1 ? start : 0);
            }
           // if(!isAutoMessage)
             //   log += "\n-------------Question & Answer with dibbr----------------------\n";
                                                                                                                          
           //  debug += $"\n[log.size={log.Length}] ";


            // var idx = log.LastIndexOf(msg.Sub(0, 20));
          //  if (IsQuestion(m) && !isAutoMessage)
            {
              //  log += $"Question: (from {user}): {msg}";
               //log+= $"\n{pre}{suffix}";
            }
          //  else
            
            
            log += suffix;

            log = log.Replace("[ChatGPT]", "");
           //   log += $"\n{pre}"+ suffix;
            // log += $"Question: (from {user}): {msg}\n"; // Program.NewLogLine;

            // if (isQuestion)
            //     log += user + ": Question: " + msg + "\nAnswer:";

            return $"Date: {DateTime.Now}\nContext:{context}\n"+(math?"":$"Memory: {(Memory.Length>0?$"Memory: {Memory}":"")}")+$"\nThe following is a discord chat with different users and {BotName}.\n\n{log}";


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

    /// <summary>
    /// Must have at least numMatch
    /// providing " what" will only find the word what, looking for 'what ...', ',what, etc, and not swhat etc
    /// </summary>
    /// <param name="str"></param>
    /// <param name="numMatch"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    public static bool Has(this string str, int numMatch = 1, params string[] values)
    {
        var strl = str;
        var count = 0;
        foreach (string value in values)
        {
            var idx = strl.IndexOf(value.Trim(),StringComparison.InvariantCultureIgnoreCase);
            // We're matching words if there is a space in front, otherwise subsring is ok
            // String: 'hey dibbr, how r u'
            // ' hey' -> Matches b/c startswith case. so space at start says essentially 'space or start'
            if (idx != -1 && value.StartsWith(" "))
            {
                var c = "";
                if (idx > 0)
                    c = str.Substring(idx - 1, 1);
                if (c == " " || c == "," || c == "." || c == "" || c == "\udd16" || c == "\n" || c == "\t" || c == "\r")
                { 
                    count++;
                    continue;
                }
            }
            // If no space, check for containing or matching string
            if (strl.Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                strl.Equals(value, StringComparison.InvariantCultureIgnoreCase))
                count++;
             //   || strl.TrimExtra().StartsWith(value.TrimExtra(), StringComparison.InvariantCultureIgnoreCase)) count++;

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