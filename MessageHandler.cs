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
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ChatGPT3;
using dibbr;
using OpenAI_API;
using static DiscordV3;
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
    public string Admin;
    public bool NewEngine = false;
    public Room room;
    public class Identity
    {
        public string BotName;
        public string Prompt;
    }
    public List<Identity> identities = new List<Identity>();
    public string Channel;
    public string Guild;
    public bool UseNeo  = true;
    // Store chat for each user, useful for future bot functionality
    public readonly Dictionary<string, string> Usernames = new();
    public DateTime BreakTime;
    public bool ChattyMode = false; // Will ask questions randomly
    public GPT3 Gpt3;
    public DateTime LastMessageTime = DateTime.MinValue; // Last response by bot
    public string LastMsgUser = ""; // Last user to be replied to by bot

    // For randomly asking questionsFs
    public DateTime LastQuestionTime = DateTime.Now;
    public List<string> Log = new List<string>();

    /// <summary>
    /// This is how many messages get passed to GPT-3. Sometimes, dibbr will get stuck repeating, so it's better to lower this number
    /// </summary>
    public int MessageHistory = 8; // how many messages to keep in history to pass to GPT3

    public int MessageHistoryLimit = 14;
    public bool Muted = false;
  //  public Speech Speech = new();
    public bool AIMode = false;
    public int TalkInterval = 10; // How often to talk unprompted (in messages in chat)


    /*   class Timeout : ICommand
       {
           public override string[] Triggers() => new string[] { "timeout", "please stop", "take a break" };

           public string Run(string msg) { breaktime = DateTime.Now.AddMinutes(10); return "I will timeout for 10 minutes"; }

       }*/

    public MessageHandler(ChatSystem client, GPT3 gpt3 = null)
    {
        Client = client;
        Gpt3 = gpt3;
        if (gpt3 == null)
        {
            Gpt3 = new GPT3(null);// Program.Get("OpenAI"));
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

      

        Gpt3.botName = BotName;

    
     
        if (user == BotName) { LastMessageTime = DateTime.Now; }

       // PrimeText = Program.Get("PrimeText");
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


        bool commands = !(Client as DiscordV3)?.SelfBot ?? true;
        var history = MessageHistory;
        if (!Personal(m))
            history /= 2;

        if (commands)
        {
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
            if (m.StartsWithAny("what is your prompt") || m.StartsWithAny("what prompt"))
            {
                var idx1 = PrimeText.IndexOf("..");
                if (idx1 == -1) idx1 = PrimeText.Length;
                return (false, PrimeText);//.Substring(0, idx1)) ;
            }

            if (m.StartsWithAny("add prompt"))
            {
                PrimeText +=
                    $"{msg.Substring(11)}";
                return (false, "[ok");
            }
            if (m.StartsWith("you are now"))
            {
                var name = msg.After("you are now");
                var info = name.After(".");
                BotName = name;
                m = msg = "set prompt " + info;

            }

            if (m.StartsWith("only respond to me"))
            {
                Admin = user;
                return (false, "Ok sir");
            }
            if (Admin != null && Admin != user)
                return (false, "");

            if (m.StartsWithAny("don't ask questions", "don't talk here"))
            {
                ChattyMode = false;
                return (false, "Sorry,  I won't ask questions randomly here");
            }
            if (m.StartsWithAny("show emotions for "))
            {
                var user2 = m.After("show emotions for");
                return (false, DiscordV3.infer.GetScores(user2));
            }
            if (m.StartsWithAny("show my emotions", "show emotions", "show my emotional profile", "what's my emotional profile"))
            {
                return (false, DiscordV3.infer.GetScores(user));
            }

            if (m.StartsWithAny("get emotions ", "top emotions", "emotion scores", "emotion ranks"))
            {
                var emo = m.AfterAll("get emotions ", "top emotions", "emotion scores", "emotion ranks");
                return (false, DiscordV3.infer.GetTop(emo));
            }

            if (m.StartsWithAny("set prompt", "set name", "from now on"))
            {
                var prompt = msg.AfterAll("from now on", "set prompt", "set name");
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
                PrimeText = Program.Get(Channel + "PrimeText") ?? Program.Get("PrimeText"); // This will force it to get reloaded properly

                // replyPrompt = "";
                return (false, "I have reset all my configuration");
            }
            if (m.EndsWith("->") && m.Contains("+"))
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
            if (m.StartsWithAny("which engine", "what engine"))
                return (false, "I am using " + (NewEngine ? "ChatGPT3" : (useNeo ? "Neo20B engine" : Gpt3.engine)));
            if (m.StartsWithAny("set engine"))
            {
                NewEngine = m.Contains("ChatGPT");
                if (!NewEngine)
                    Gpt3.engine = m.After("set engine").Trim(); return (false, $"Engine set {(useNeo ? "But Neo engine is on. Type <botname> neo off to switch to GPT3" : "")}]");
            }

            bool Is(string str) => m.StartsWithAny(str) || m == str;

            bool Has(string str) => m.StartsWithAny(str) || m == str;

            /* if (Is("join "))
             {
                 var channel = m.After("join ");
                 (Client as DiscordChat).AddChannel(channel, false, new MessageHandler(Client, GPT3), true);
                 return (false, "[Joined channel!");
             }*/
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
            var muted = Muted; // so we can change locally temporarily
            if (m == "mute") muted = true;

            if (m == "unmute") muted = false;


            if (muted && user != "dabbr")
            {
                return
                    (isReply, null); // breakTime = DateTime.Now.AddMinutes(5); // Muted mode allows 1 message every 5 miuns
            }
            if (m.Contains("what's your number"))
            {
                return (false, "[You can SMS me at (USA+1) 970-696-8551");
            }
            if (m.StartsWith("smsdump"))
            {
                var res = Dibbr.Phone.Send("", "");
                return (false, res);
            }
            if(m.StartsWith("close portal"))
            {
                var us = (Client as DiscordV3).Rooms.Where(r => r.Value.handler.Channel == this.Channel).First().Value.SChannel;
                var p = (Client as DiscordV3).Rooms.Where(r => r.Value.Portals.Contains(us));
                foreach (var po in p)
                    po.Value.Portals.Remove(us);
                if (p.Count() > 0)
                    return (true, "Portal removed!");
                else return (true, "Portal not found!");


            }
            if (m.StartsWith("open p0rtal"))
            {
                var c = m.After("open p0rtal");
                if(c=="")
                    return (true, $"syntax is open portal <channel>. These are the channels: " + string.Join(", ", (Client as DiscordV3).Rooms.Select(r => r.Value.handler.Channel)));
                var chan = (Client as DiscordV3).Rooms.Where(r => r.Value.handler.Channel == c).ToList();
                if(chan.Count > 0)
                {
                    chan[0].Value.Portals.Add((Client as DiscordV3).Rooms.Where(r => r.Value.handler.Channel == this.Channel).First().Value.SChannel);
                }
            
       
            }
            if (m.StartsWith("broadcast"))
            {
                var guids = new Dictionary<string, int>();
                var text = m.After("broadcast ");
                foreach(var room in (Client as DiscordV3).Rooms)
                    if(!guids.ContainsKey(room.Value.Guild))
                    if(room.Value.handler.Channel.ToLower().Contains("dibbr"))
                    {
                           // if(room.Value.SChannel.d)
                        room.Value.SChannel.SendMessageAsync(text);
                        guids.Add(room.Value.Guild, 1);
                    }
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

            if (m.StartsWithAny("set name", "add name"))
            {
                var add = m.Contains("add name");
                var b = msg.AfterAll("set name", "add name").BeforeAll(",", ".").Trim();
                var desc = msg.AfterAll(",", ".");
                if (desc.Length == msg.Length)
                    desc = "This is a conversation between " + b + " and other people";
                // Save old identity
                // if(!add)
                if (identities.Where(d => d.BotName == msg.AfterAll("set name", "add name").Trim()).Count() == 0)
                    identities.Add(new Identity() { BotName = BotName, Prompt = PrimeText });

                var oldName = BotName;
                BotName = b;
                for (int k = 0; k < Log.Count; k++)
                {
                    string l = Log[k];
                    if (!add)
                        Log[k] = l.Replace(oldName, BotName);
                }
                if (PrimeText.Contains(BotName))
                {

                }
                else if (!add)
                {
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

            if (m.StartsWith("AIMode on")) AIMode = true;
            if (m.StartsWith("AIMode off")) AIMode = false;

            if (m.Contains("dump last query"))
                return (false, $"[{Gpt3.LastQuery}]");
            if (m.Contains("dump memory"))
                return (false, Memory);
            if (m.Trim() == "cost")
                return (false, GPT3.Stats());




            if (m.Contains("max memory"))
            {
                MessageHistory = 20;
                return (false, "");//, "+MessageHistory+" lines"); //$"Memory boosted to {MessageHistory} log lines");
            }

            if (m.Contains("boost memory"))
            {
                MessageHistory = MessageHistory * 3;
                return (false, "");//, "+MessageHistory+" lines"); //$"Memory boosted to {MessageHistory} log lines");
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
            /* if (m.Contains("list "))
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



             }*/

            /*
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
            if (m.StartsWithAny("execute", "evaluate", "run this", "exec"))
            {
                var c = msg.AfterAll("execute", "evaluate", "run this", "exec").Trim();


                // return (false, Exec2(c));


            }
            if (msg.Contains("engine="))
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
                return (false, $"ok, k will remember that. type recall to see memory, reset to forget");
            }

            if (msg.StartsWithAny("dump memory", "what do you remember", "recall")) { return (false, "[ Memory is " + Memory); }

        }

        // All messages are replies unless marked otherwise

       
        Regex hasMath = new Regex(@"(?<num1>[0-9]+)(?<op>[\*\-\+\\\^])(?<num2>[0-9]+)");
        var math = hasMath.IsMatch(m) || m.HasAny(" sin(","/x", " cos(","cosine"," sine","convergent","the sum","derivative");
      
       // else GPT3.engine = "text-dav"
        if (DateTime.Now < BreakTime)
        {
            if (m.StartsWithAny("come back")) return (false, "[I can't, it's too late");
            if (m.StartsWithAny("wake") || m.StartsWithAny("awake")) { BreakTime = DateTime.MinValue; }
            else
                return (false, "");
        }

       
      

        var dumb = BotName.ToLower().Contains("xib");

     
        bool IsDupe(string txt) => (Log.Last(3).Where(s => s.Contains(txt, StringComparison.InvariantCultureIgnoreCase)).Count() > 0 || (Usernames.ContainsKey(BotName) &&
                                              StringHelpers.Get(Usernames[BotName].TakeLastLines(1)?[0], txt) >
                                               0.4));
        bool IsContinue(string m) => m.Contains("response") || m.Contains("continue") || (m.Contains("rewrite") && m.Length < 30) || m.Contains("more detail") || m.Contains("go on") || m.Contains("keep going");
        bool IsWriting(string m) => m.Remove("please").Remove("can you").StartsWithAny("write") || (m.HasAny("write","make ", "design", "create", "compose", "generate") && m.HasAny(" rap ", "article", "story", "long", "dialog", "discussion", "script", "game",
            "detailed", "screenplay", "letter", "episode", "design", "theory", "explanation", "essay"));
        bool IsQuestion(string m) => m.StartsWithAny("is ","calculate", "when ", "where ", "where's", "would ", "can ", "does ", "could ", "why ",
            "how ", "what ", "can someone", "what's", "name  ") || m.HasAny("?");
        var queryOnly = msg.Contains("!!!");
        if (queryOnly)
            useNeo = false;
        var writeSong = (m.HasAny(" rap ","lyrics","song ") &&
                        !m.HasAny("mode2")) && IsWriting(m) && m.Length < 20;
        var rewrite = msg.Length < 15 && msg.HasAny("rewrite", "write it again", "more ", "less ", "try again", "write it", "change ", "longer", "shorter");

        var useSteps  = msg.Length < 15 && msg.HasAny("steps", "??","calculate", "solve", "given ", "if there are", "explain", 
            "what is ", " how ", "what would", "how do k", "what will") || math || m.Has(2, "how ", "what ", "why ", "when ", "where ");

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
        var suffix = $"{BotName}: ";// $"{(AIMode ? "AI" : BotName)}'s " + ((math || IsQuestion(msg)) ? "answered" : "replied") + $" to {user} saying \"" + (useSteps || math ? "Let's think in steps and end with a conclusion." : "");
        if (IsQuestion(m))
            suffix = $"{BotName}'s reply to {user}:";


        if (math || useSteps)
            suffix += "Let's think in steps.";

        if (writeSong)
            suffix += "Here are the lyrics:";
        else if (IsWriting(m) && m.Has(2,"write","story") && m.Length < 15)
            suffix +=
                   $" Here is the story:";
        else if (IsWriting(m) && m.HasAny("screenplay"))
            suffix += $"Here is the screenplay:";
        else if (IsWriting(m) && m.Has(2,"write","article") && m.Length < 20)
            suffix += $"Here is the article";
  



      //  if (GPT3.engine.Contains("code"))
        //    suffix = "";

        // else { suffix = $"The following is {BotName}'s lengthy, funny, brilliant, multi-paragraph attempt:"; }

       
        Console.WriteLine("Suffix == "+suffix);
      
        List<string> TrimLines(List<string> lines)
        {
            var newLines = new List<String>();
            for (int i = 0; i < lines.Count; i++)
            {
                string l = lines[i];

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

            var hasP = xt.HasAny("said","you're","your", "my"," it", " you ","said","him", "anyone", " we ", " our ", " he ", " his ", " she ", " her ", " here","this","more "," my "," our ", " this ","user","discord","chat ","remember"," i ", " that ")
                || xt.StartsWithAny("he","you","I ","but","then ","that","it","we","this")
                || room.HasUsername(xt);
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
                //txt += await GPT3.Ask((IsQuestion(msg) ? "Question " : "") + msg.Remove(BotName) + "\nAnswer: " + suffix, user);
            }

           // else
            {
                // if(msg.Contains("dibbrjones"))
                //     GPT3.
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
            if (useSteps)
                engine = "text-davinci-003";
            // GPT3.3 is better at these things
            if (math || msg.Has(2, "tell", "joke") || writeSong || IsWriting(msg) || msg.HasAny("write a rap", "write a song"))

            {
                if (!msg.HasAny("rude", "racist", "offensive", "sexist", "bitch", "fuck", "shit", "rape", "sex", "your", "women"))
                {
                    //NewEngine = true;
                   // engine = "text-davinci-003";
                }
            }

            // if(msg.Contains("dibbrjones"))
            //     GPT3.
            var useNew = msg.EndsWith("???") ||
               msg.EndsWith('!') || (msg.Contains("!NewEngine")
               || msg.Contains("!ChatGPT"));

            var tryNew = (msg.Length > 10 && msg.Contains("?") && NewEngine) && !useNew;

            useNew = false;
            tryNew = false;
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
            if(msg.Contains("UpdateCookies"))
            {
                GPT3.chatgpt.UpdateCookies(msg.After("UpdateCookies"));
                NewEngine = true;
                return (true, "Cookies updated! Try ChatGPT again now.");
            }
            msg = msg.Replace("!NewEngine","").Replace("!ChatGPT","");
            if (useNew||tryNew)
            {
                string RemoveSentence(string txt, string str)
                {
                    var idx = txt.IndexOf(str);

                    if (idx != -1)
                    {
                        // Find start of sentence
                        var idxp = txt.LastIndexOf('.', idx) + 1;

                        var idx2 = txt.IndexOf(".", idx);
                        if (idx2 != -1)
                            txt = txt.Remove(idxp, idx2 - idxp);
                        else txt = txt.Replace(str, "");
                    }
                    return txt.TrimExtra();
                }
                msg = msg.Replace(BotName, "");
                var txt2 = await Gpt3.Chat(msg);
                Console.WriteLine($"ChatGPT: {txt2}");
                txt = RemoveSentence(txt2, "browse the internet");
                txt = RemoveSentence(txt, "My purpose is to assist with generating human-like text");
                //txt = RemoveSentence(txt, "I'm sorry, but");
             //   txt = RemoveSentence(txt, "as a large language model trained by OpenAI");
                txt = RemoveSentence(txt, "model trained by OpenAI");
                if (txt.Length == 0)
                {
                    txt = txt2 + " (there is a bug in your code, dabbr)";
                }

             
           
                    NewEngine = oldNew;
                if(useNew && !tryNew)
                if (txt.StartsWith("Failure") || txt.Length == 0)// || txt.HasAny("appropriate","inappropriate","language model","openai"))
                {
                    //  if (m.Contains("!NewEngine") || m.Contains("!ChatGPT"))
                   
                    txt += ("\nTry updating cooies with UpdateCookies <ChatGPT3Cookies>. You can get this from the browser with F12. Until then NewEngine will be turned off " + txt);
                    useNew = false;
                    NewEngine = false;
                    //  engine = "text-davinci-002";
                    Console.WriteLine(txt);
                    return (true, txt);
                }
                else
                {
      
                    txt += " [ChatGPT]";
                    return (true, txt);
                }   
            }
            
          //  if (!useNew)
            {
                Gpt3.engine = engine;
                txt = await Gpt3.Ask(MakeText(history), msg, user, 2000, codeMode);
                txt = txt.Remove("[ChatGPT]");
                Gpt3.engine = oldEngine;

            }
           
        }

        debug += $"\n[UseNeo={useNeo}, history={history},useSteps={useSteps},isSong={writeSong}, queryOnly={queryOnly}, query length={Gpt3.LastQuery.Length},IsQuestion={IsQuestion(m)},IsWriting={IsWriting(m)}] ";

  

        ////////////////////////////////////////////// Response Reply //////////////////////////////
        ///


        var (pct,tx) = txt.Deduplicate(string.Join("\n>>", Log.Last(4)));
        txt = tx;

        if (txt.Lame() && isAutoMessage)
            return (false, DebugMode ? "Returning because lame and automsg" : "");

        Console.WriteLine(txt.Length); ;



        if (txt.IsNullOrEmpty() && isAutoMessage) return (false, DebugMode?debug:"");
     
        txt = txt.Replace("@everyone", "(at)everyone");

        if (txt.Contains("no credits left"))
        {
            //log = "";
            Log.Clear();
            //txt = await GPT3.Ask(MakeText(), msg);
        }


        // If the chat includes another chat starting, eg hi guys person: hey dibbr, filter it
        if (txt.Contains($"{BotName}:"))//cards",StringComparison.OrdinalIgnoreCase) && (txt.Contains(":") && txt.IndexOf(":") > txt.Length/2))
        {
            txt = txt.Substring(0, txt.IndexOf($"{BotName}:"));
            txt = txt.Sub(0, txt.LastIndexOf(" "));
        }


       
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
            // if (GPT3.engine.Contains("code")) return msg.Remove($"{BotName}");// + suffix;
            var warn = ""; //" (NOTE, If a leading question implies a fact, dibbr should not assume it is true) ";

            if (context is null or "")
                context = PrimeText;

            if (suf is null or "")
                suf = suffix;

            //  context = "This is a conversation with the world's smartest humans and an AI, dibbr. dibbr thinks in steps, gives long responses and knows everything. Dibbr is one of our brightest minds, hilarious and sarcastic.";
            //if (IsQuestion(m) && !isAutoMessage)
              //  history = 0;

                var log1 = Log.Last(history+1).Distinct().ToList();

          //  if(history == 0)
            
            for (int m = 0; m < log1.Count; m++)
            {
              //  log1[m] = log1[m].Replace(": ", " said \'") + "'";
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
            var log = pre+string.Join($"\n{pre}", Log.Last(history+1).ToList())+$"\n{pre}";

            if (log.Length > 2000)
                log = log;


            if (log.Length > 1500)
            {
                log = log[^1500..].Trim();
                log = "[...]\n" + log;
                var start = log.IndexOf("@@");
                //log = log.Substring(start != -1 ? start : 0);
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
            
            var r = room;
            // You can set channel topic to override prime text
            if (r.ChannelDesc.Contains(BotName + ":"))
                PrimeText = "";
            return $"<Date>: {DateTime.Now} PST, <Server>: {r.handler.Guild}, <Channel>: {r.handler.Channel}\n" +
                $"<Topic>{r.ChannelDesc}\n<Context>"+PrimeText+$"\n<Chat Log>\n{r.PinnedMessages}\n{log}";


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

    public static bool IsNullOrEmpty(this string str)
    { 
        return str == null || str.Length == 0;
    }

    public static List<string> Replace(this List<string> list, string find, string rep)
    {
       for(int i= list.Count - 1; i >= 0; i--)
        {
            if (list[i] == find || list[i].StartsWith(find))
            {
                list[i] = rep;
                return list;
            }
        }
        return list;
    }
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

    public static void Increment(this Dictionary<string, double> dict, string key, double d)
    {
        if (!dict.TryAdd(key, d))
        { 
            dict.TryGetValue(key, out double value);
            dict[key] = value + d;
        }
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