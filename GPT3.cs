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
        public GPT3(string token)
        {
            this.token = token;
        }

        string token;
        private OpenAIAPI api;
        private int MAX_CHARS = 100; // GPT-3 working memory is 100 chars

        static string CleanText(string txt)
        {
            if (txt == null) return null;
            txt = txt.Trim();
            txt = txt.Replace("\"", "");
            // Gay stuff GPT-3 likes to return
            if (txt.StartsWith("There is no") || txt.StartsWith("There's no"))
            {
                txt = txt[(txt.IndexOfAny(new char[] { '.', ',' }) + 1)..];
            }

            // Remove  There's no right or wrong answer blah blah blah at the end
            var last = txt.IndexOf("Ultimately,");
            if (last != -1)
                txt = txt[..last];
            return txt;
        }

        
         float fp = 0, pp = 0.5f, temp = 1;
         string engine = "text-davinci-002";
         Engine e;
        /// <summary>
        /// Asks OpenAI
        /// </summary>
        /// <param name="q"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public  async Task<string> Ask(string msg, string log, string user = "")
        {
            if (api == null)
            {
                e= new Engine(engine) { Owner = "openai", Ready = true };
                api = new OpenAI_API.OpenAIAPI(apiKeys: token, engine: e);
            }
            
            
            // Set variables like this
            // dibbr hey ?fp=1&pp=2
            var line = msg;
            if (line.Contains("=") && line.Contains("?"))
            {
                string[] query = line.Split('?');
                if (query.Length == 2)
                {
                    foreach (string pairs in query[1].Split('&'))
                    {
                        string[] values = pairs.Split('=');
                        if (values[0] == "pp")
                            pp = float.Parse(values[1]);
                        if (values[0] == "buffer")
                            MAX_CHARS = int.Parse(values[1]);
                        if (values[0] == "fp")
                            fp = float.Parse(values[1]);
                        if (values[0] == "temp")
                            temp = float.Parse(values[1]);
                        if (values[0] == "engine")
                        {
                            engine = values[1];
                            e = new Engine(engine) { Owner = "openai", Ready = true };
                            api = new OpenAI_API.OpenAIAPI(apiKeys: token, engine: e);
                        }
                    }

                    return $"Changes made. Now, fp={fp} pp={pp} temp={temp} engine={engine}";
                }
            }


            string MakeText()
            {
                if (log.Length > MAX_CHARS)
                    log = log[^MAX_CHARS..];
                log = log.Trim();                
                
                return ConfigurationManager.AppSettings["PrimeText"] + "\n"
                    + log + msg + "\ndibbr's response: ";
            }
            // Setup context, insert chat history
            var txt = MakeText();
            
            string r = await Q(txt,pp,fp,temp);

            // If dup, try again
            var split = r.Split('.');
            int percentMatch = 0;
            foreach (var s in split)
            {
                if (log.Contains(s))
                    percentMatch += s.Length;
            }
            // Ask new Q if our answer was too similar to the previous one
            if (percentMatch > log.Length / 2)
            {
                log = "";
                return await Q(MakeText(), pp, fp, 1);
            }                
       
            return r;

             async Task<string> Q(string txt, float pp, float tp, float temp)
            {
                var result = await api.Completions.CreateCompletionAsync(txt,
                                temperature: temp, top_p: 1,frequencyPenalty:tp,presencePenalty:pp, max_tokens: 1000, stopSequences: new string[] { Program.BotName + ":" });
                // var r = CleanText(result.ToString());
		var r = result.ToString();
                Console.WriteLine("GPT3 response: " + r);
                r = r.Trim(); ;
                return r;
            }
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
                var k = token;
                api = new OpenAI_API.OpenAIAPI(apiKeys: k, engine: eng);
            }
            var stops =
                new string[] { Program.BotName + ":" };
            var result = await api.Completions.CreateCompletionAsync(q,
                temperature: 0.8, top_p: 1, max_tokens: 1000, stopSequences: stops);

            var r = result.ToString();
            Console.WriteLine("GPT3 response: " + r);
            return r;
        }
    }
}
