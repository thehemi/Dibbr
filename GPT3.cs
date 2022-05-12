// (c) github.com/thehemi
// using System;
using OpenAI_API;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace DibbrBot
{
    /// <summary>
    /// Chat system that uses the OpenAI API to send and receive messages
    /// </summary>
    public class GPT3
    {
        // Shared between all instances
        private static OpenAIAPI api;
        private static readonly int MAX_TOKENS = 2000;


        /// <summary>
        /// Asks OpenAI
        /// </summary>
        /// <param name="q"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task<string> Ask(string q, string user = "")
        {
            if (api == null)
            {
                var eng = new Engine("text-davinci-002") { Owner = "openai", Ready = true };
                var k = ConfigurationManager.AppSettings["OpenAI"];
                api = new OpenAI_API.OpenAIAPI(apiKeys: k, engine: eng);
            }

            // Prime it with other questions here
            // q = "questions"+q;

            // Setup context, insert chat history
            var txt = ConfigurationManager.AppSettings["PrimeText"] + "\n"
                    + q + Program.BotName + ": ";

            var result = await api.Completions.CreateCompletionAsync(txt,
                temperature: 1.0, top_p: 1, max_tokens: MAX_TOKENS, stopSequences: new string[] { Program.BotName + ":" });

            var r = result.ToString();
            Console.WriteLine("GPT3 response: " + r);
            return r;
        }

        /// <summary>
        /// Asks OpenAI, but as a completion without history
        /// </summary>
        /// <param name="q"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<string> Ask2(string q, string user = "")
        {
            if (api == null)
            {
                var eng = new Engine("text-davinci-002") { Owner = "openai", Ready = true };
                var k = ConfigurationManager.AppSettings["OpenAI"];
                api = new OpenAI_API.OpenAIAPI(apiKeys: k, engine: eng);
            }
            var stops =
                new string[] { Program.BotName + ":" };
            var result = await api.Completions.CreateCompletionAsync(q,
                temperature: 0.8, top_p: 1, max_tokens: MAX_TOKENS, stopSequences: stops);

            var r = result.ToString();
            Console.WriteLine("GPT3 response: " + r);
            return r;
        }
    }
}
