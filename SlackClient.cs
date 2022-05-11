﻿using System;
using System.Threading;
using System.Threading.Tasks;
using SlackNet.Bot;
using System.Configuration;

namespace DibbrBot
{

    /// <summary>
    /// A client to slack
    /// </summary>
    class SlackChat : IChatSystem
    {
        private string ChatLog = "";
        public MessageRecievedCallback Callback;
        public SlackChat()
        {
            ChatLog = "";
        }

        public override string GetChatLog()
        {
            return ChatLog;
        }
        // Initialize slack client
        public override async Task Initialize(MessageRecievedCallback callback, string token = null)
        {
            Callback = callback;
            var s = new Thread(async () =>
            {
                var bot = new SlackBot(token??ConfigurationManager.AppSettings["SlackBotApiToken"]);

                //bot.Responders.Add(x => !x.BotHasResponded, rc => "My responses are limited, you must ask the right question...");
                // .NET events
                bot.OnMessage +=  (sender, message) => {
                    var txt = message.Text;

                    // Change @bot to bot, query
                    if (message.MentionsBot)
                    {
                        txt = Program.BotName +" "+ txt[(txt.IndexOf(">") + 1)..];
                    }
                    @ChatLog += message.User.Name + ": " + message.Text + "\n";                    

                    message.ReplyWith(async () => {
                        var msg = await callback(txt, message.User.Name);
                        if (msg == null)
                            return null;
                        else
                         return new BotMessage { Text = msg };
                    });
                    /* handle message */
                };
                bot.Messages.Subscribe(( message) => {
                    Console.WriteLine(message.Text);
                });
              //  bot.AddHandler(new MyMessageHandler(this));


                await bot.Connect();
                await Task.Delay(200);
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
        private void Bot_MessageReceived(string messageText)
        {
            Console.WriteLine("Msg = " + messageText);
        }

    }
}