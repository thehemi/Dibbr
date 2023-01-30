using DibbrBot;
using Discord;
using SlackNet.Interaction;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
/*using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML.Voice;*/
using Task = System.Threading.Tasks.Task;

namespace Dibbr
{
    public class SmsChat
    {
        public List<string> messages;
        public string reply;
    }

    public class Phone
    {
        public static bool init;
        public static void Init()
        {
            if (init) return;
            string accountSid = "AC4273ddc8dda4d514a340e942936412aa";// Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
            string authToken = Program.Get("TwilioAuth");// Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
            if (authToken == null)
                return;

            //TwilioClient.Init(accountSid, authToken);
            if (gpt == null)
                gpt = new GPT3(Program.Get("OpenAI"));
            init = true;
        }
        public static GPT3 gpt;
        public static Dictionary<string, List<string>> Logs = new Dictionary<string, List<string>>();
        public static Dictionary<string, string> Messages = new Dictionary<string, string>();
        public static List<string> questions = new List<string>();

        public async static void HandleMessages()
        {
           // return;
           /* var tm = DateTime.Now - TimeSpan.FromSeconds(10);
            var me = new Twilio.Types.PhoneNumber("+19706968551");
            Init();
            var ret = ""; 
            var opt = new ReadMessageOptions();
            opt.DateSentAfter = tm;*/
            //opt.
        
               // var messages = await MessageResource.ReadAsync(opt);
           
          //  ret += "\nMessages Recieved: \n";
        //    foreach (var m in messages)
      
             /*   var seen = Messages.TryAdd(m.Sid, m.Body) || questions.Where(q=>q==m.Body).Count() > 0;
                if (seen || m.From.ToString() == me.ToString() || m.DateSent < tm) continue;
                questions.Add(m.Body);
                //Console.Beep();
                Console.WriteLine("SMS: "+m.Body);
                var logs = Logs.GetOrCreate(m.From.ToString(), new List<string>());
                logs.Add("Q: " + m.Body);
                var response = await gpt.Ask(string.Join("\n",logs)+"\nA:",m.Body, "");
                if (response.Length > 0)
                {
                   
                   
                    logs.Add("A: " + response);
                    Console.WriteLine("SMS: " + response);
                    var message = MessageResource.Create(
                       body: response,
                       from: me,
                       to: m.From//new Twilio.Types.PhoneNumber(number)
                   );
                    Messages.TryAdd(message.Sid, message.Body);

                }
               

                ret += $"From:{m.From.ToString()} Body:{m.Body}\n";
                }*/
     


        }

        public static string Send(string number, string msg)
        {
            if (number == "me")
                number = "+16193895563";
            if(number.Length > 0)
            number = number.Replace(" ","").Replace("-","").Trim();
            if (number.Length == 10)
                number = "+1" + number;
            // Find your Account SID and Auth Token at twilio.com/console
            // and set the environment variables. See http://twil.io/secure
           
            var ret = "";
            if (number.Length > 0)
            {
             /*   var message = MessageResource.Create(
                    body: msg,
                    from: new Twilio.Types.PhoneNumber("+19706968551"),
                    to: new Twilio.Types.PhoneNumber(number)
                );

                ret += $"ok! sid {message.Sid} status {message.Status} ";
                if (message.ErrorMessage is not null or "")
                    ret += message.ErrorMessage;*/
            }


           
            return ret;
        }
    }
}




public static class Repeat
{
    public static Task Interval(
        TimeSpan pollInterval,
        Action action,
        CancellationToken token)
    {
        // We don't use Observable.Interval:
        // If we block, the values start bunching up behind each other.
        return Task.Factory.StartNew(
            () =>
            {
                for (; ; )
                {
                    if (token.WaitCancellationRequested(pollInterval))
                        break;

                    action();
                }
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}

static class CancellationTokenExtensions
{
    public static bool WaitCancellationRequested(
        this CancellationToken token,
        TimeSpan timeout)
    {
        return token.WaitHandle.WaitOne(timeout);
    }
}