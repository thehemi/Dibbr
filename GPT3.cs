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
        private int MAX_CHARS = 2000; // GPT-3 working memory (chat log)

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
        public async Task<string> Ask(string msg, string log, string user = "", string endtxt = "dibbr's response: ", int MAX_CHARS = 4000)
        {
            if (api == null)
            {
                e = new Engine(engine) { Owner = "openai", Ready = true };
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

                if (log == "")
                    log += user + ": " + msg + "\r\n";

                return ConfigurationManager.AppSettings["PrimeText"] + "\n"
                    + log + "\n" + endtxt;
            }
            // Setup context, insert chat history
            var txt = MakeText();
            string r = await Q(txt, pp, fp, temp);



            return r;
            // Ask new Q if our answer was too similar to the previous one
            /*   if (percentMatch > log.Length / 3)
               {
                   log = "";
                   return null;// await Q(MakeText(), pp, fp, 1)+"\nDev Note: This is the second message "+Program.BotName+" came up with. The first was too repeptitive";
               }                

               return r;*/
            string Last(string log, int MAX_CHARS)
            {
                if (log.Length > MAX_CHARS)
                    log = log[^MAX_CHARS..];
                log = log.Trim();
                return log;
            }

            (int dupePercent, string uniquePart) Deduplicate(string r)
            {
                var split = r.Split(new char[] { '.' });
                int percentMatch = 0;
                var response = "";
                foreach (var s in split)
                {
                    if (Last(log, 2000).Contains(s))
                        percentMatch += s.Length;
                    else
                        response += s + ". ";
                }
                if (!r.EndsWith(".") && response.Length > 0)
                    response = response.Substring(0, response.Length - 2);

                return (percentMatch, response);
            }
            async Task<string> Q(string txt, float pp, float tp, float temp)
            {
                var result = await api.Completions.CreateCompletionAsync(txt,
                                temperature: temp, top_p: 1, frequencyPenalty: tp, presencePenalty: pp, max_tokens: 1000, stopSequences: new string[] { Program.BotName + ":" });
                // var r = CleanText(result.ToString());
                var r = result.ToString();
                Console.WriteLine("GPT3 response: " + r);
                r = r.Trim();

                // If dup, try again
                var (percentDupe, response) = Deduplicate(r);
                if (percentDupe > r.Length / 3)
                {
                    log = "";
                    return await Q(MakeText(), pp, fp, 1);
                }
                if (response == "")
                    return null;

                return response;
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


    static class LevenshteinDistance
    {
        public static float Get(string s, string t)
        {
            if (t == null || s == null)
                return 1;
            float len = Math.Max(s.Length, t.Length);
            // normalize by length, high score wins
            return (len - Compute(s, t)) / (len);
        }
        public static int Compute(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (string.IsNullOrEmpty(t))
                    return 0;
                return t.Length;
            }

            if (string.IsNullOrEmpty(t))
            {
                return s.Length;
            }

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // initialize the top and right of the table to 0, 1, 2, ...
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 1; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    int min1 = d[i - 1, j] + 1;
                    int min2 = d[i, j - 1] + 1;
                    int min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }
            return d[n, m];
        }
    }
}
