using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
 
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using SlackNet.Bot;
using System.Text.Json;
using System.Text.Json.Serialization;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.Endpoints.Authentication;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.Endpoints.Models.Interactive.MessageActions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DibbrBot;
class DoStuff : IHandleAppMentions
{
    public bool ShouldHandle(AppMentionEvent slackEvent) => slackEvent.Text.Contains("hi");

    public Task<EventHandledResponse> Handle(EventMetaData meta, AppMentionEvent @evt)
    {
        Console.WriteLine("Doing stuff!");
        return Task.FromResult(new EventHandledResponse("yolo"));
    }
}
/// <summary>
///     A client to slack
/// </summary>
class SlackChat : ChatSystem
{
    public MessageRecievedCallback Callback;
    public string ChatLog = "";

    public SlackChat() => ChatLog = "";


    public override void Typing(bool start)
    {
        // API.Typing(client, channel, start);
    }


    // Initialize slack client
    public override async Task Initialize(MessageRecievedCallback callback, string token = null)
    {
        Callback = callback;
        var s = new Thread(async () =>
        {
            // app level token xapp-1-A05668NV8Q3-5203894624422-8fc71dbf0e5b27601e67015f2a07c14ab6bfb2f827818f24cba4017e55ff5ac2
            var builder = WebApplication.CreateBuilder( );
            // bot toekn xoxb-390820819394-5234179131504-u98ggIicWLZJKVFFFGypETJ8
            // Needed to verify that incoming event payloads are from Slack
            builder.Services.AddAuthentication()
                        .AddSlackbotEvents(c =>
                            c.SigningSecret = Environment.GetEnvironmentVariable("8a58587e631d9a5ff018803e4991339e")
                        );

            // Setup event handlers
            builder.Services.AddSlackBotEvents()
                            .AddAppMentionHandler<DoStuff>();


var app = builder.Build();
        app.UseSlackbot(); // event endpoint
     //   app.Run();


   // var bot = new SlackBot(token ?? ConfigurationManager.AppSettings["SlackBotApiToken"]);

            //bot.Responders.Add(x => !x.BotHasResponded, rc => "My responses are limited, you must ask the right question...");
            // .NET events
        /*    bot.OnMessage += (sender, message) =>
            {
                var txt = message.Text;

                // Change @bot to bot, query
                if (message.MentionsBot && !txt.ToLower().StartsWith(Program.BotName) || message.Conversation.IsIm)
                    txt = Program.BotName + " " + txt[(txt.IndexOf(">") + 1)..];

                ChatLog += message.User.Name + ": " + message.Text + Program.NewLogLine;

                if (txt.ToLower().StartsWith(Program.BotName))
                {
                    message.ReplyWith(async () =>
                    {
                        var (_, msg) = await callback(txt, message.User.Name, true);
                        if (msg == null) return null;

                        ChatLog += Program.BotName + ": " + msg + Program.NewLogLine;
                        return new() {Text = msg};
                    });
                }
                
            };*/
            //  bot.Messages.Subscribe(( message) => {
            //     Console.WriteLine(message.Text);
            // });
            //  bot.AddHandler(new MyMessageHandler(this));

            try
            {
             //   await bot.Connect();
              //  await Task.Delay(200);
            }
            catch (Exception e) { Console.WriteLine(e.Message); }

            while (true) { }
        });
        s.Start();
    }

    /*   class MyMessageHandler : IMessageHandler
       {
           public MyMessageHandler(SlackChat chat)
           {
               slack = chat;
           }
           public static SlackChat slack;

           public Task HandleMessage(IMessage message)
           {
               slack.ChatLog += message.User.Name + ": " + message.Text + "\n";
               message.ReplyWith(async () => {
                   var msg = await slack.Callback(message.Text, message.User.Name);
                   return new BotMessage { Text = msg };
               });
               return Task.FromResult(0);
           }
       }*/
    void Bot_MessageReceived(string messageText) { Console.WriteLine("Msg = " + messageText); }
}