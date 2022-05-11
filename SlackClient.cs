using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.Windsor;
using MargieBot;
using System.Configuration;
using Castle.MicroKernel.Registration;

namespace DibbrBot
{

    /// <summary>
    /// A client to slack
    /// </summary>
    class SlackChat : IChatSystem
    {
        private string ChatLog = "";

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
            var s = new Thread(() =>
            {
                var container = new WindsorContainer();
                container.Register(Component.For<IResponder>().ImplementedBy<BotResponder>());

                var bot = new MargieBot.Bot();
                var responders = container.ResolveAll<IResponder>();
                foreach (var responder in responders)
                {
                    (responder as BotResponder).Chat = this;
                    bot.Responders.Add(responder);
                }
                var connect = bot.Connect(token ?? ConfigurationManager.AppSettings["SlackBotApiToken"]);

                while (Console.ReadLine() != "close") { }
            });
            s.Start();
        }


        public class BotResponder : IResponder
        {
            public SlackChat Chat { get; set; }
            public IChatSystem.MessageRecievedCallback Callback;
            public bool CanRespond(ResponseContext context)
            {
                return context.Message.MentionsBot
                      && !context.BotHasResponded
                      && context.Message.Text.ToLower().Contains("dibbr");
            }

            public BotMessage GetResponse(ResponseContext context)
            {
                Chat.ChatLog += context.Message.Text + "\n";

                return new BotMessage { Text = Task.Run<string>(async () => (await Callback(context.Message.Text, context.Message.User.ToString()))).Result };
            }
        }

    }
}
