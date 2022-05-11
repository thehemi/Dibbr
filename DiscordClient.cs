using System;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus;
//using DSharpPlus.CommandsNext;

namespace DibbrBot
{


    /// <summary>
    /// API Client based discord access. More features than DiscordV1, notably you can use with bots
    /// Doesn't support selfbots
    /// </summary>
    class DiscordChatV2 : IChatSystem, IDisposable
    {
        public override string GetChatLog() { return ""; }
        private DiscordClient _discordClient;
        //  private CommandsNextUtilities _commandsNext;
        readonly bool disposed = false;
    //    public string token = "";
        MessageRecievedCallback Callback;


       // public DiscordChatV2(string token)
       // {
     //       this.token = token;
     //   }

        public async override Task Initialize(MessageRecievedCallback callback, string token)
        {
            Callback = callback;
            try
            {
                var clientConfig = new DiscordConfiguration
                {
                    //  MinimumLogLevel = LogLevel.Critical,
                    Token = token,
                    TokenType = TokenType.Bot,
                    //  UseInternalLogHandler = true
                };
              
                /* INSTANTIATE DISCORDCLIENT */
                _discordClient = new DiscordClient(clientConfig);
                _discordClient.MessageCreated += _discordClient_MessageCreated;

                await _discordClient.ConnectAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                System.Threading.Thread.Sleep(5000);
                Environment.Exit(0);
            }
            await Task.Delay(-1);
        }

        private async Task<Task> _discordClient_MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            Console.WriteLine(e.Channel.Name);
            Console.WriteLine(e.Message.Content);
            Console.WriteLine(e.Author.Username);
            var str = await Callback(e.Message.Content, e.Author.Username);
            if(str != null)
              await sender.SendMessageAsync(e.Channel, str);

            return Task.CompletedTask;
        }


        /* DISPOSE OF MANAGED RESOURCES */
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
            {
              //  Initialize().Dispose();
            }

        }
    }
}
