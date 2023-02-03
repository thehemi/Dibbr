// (c) github.com/thehemi
// using System;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChatGPT3;
using Newtonsoft.Json;
using OpenAI_API;


// ReSharper disable StringIndexOfIsCultureSpecific.1

namespace DibbrBot;

/// <summary>
///     Chat system that uses the OpenAI API to send and receive messages
/// </summary>
public class GPT3
{
    public string _token;
    public string oldToken;
    public string botName;
    public OpenAIAPI _api, _apiFallback;
    Engine _e;
    public static ChatGPT chatgpt;// = new ChatGPT();

    public static int TokensTotal, TokensLog, TokensQuery, TokensResponse = 0;

    public string engine;
    public string LastQuery = "";
    float _fp = 0.7f, _pp = 1.1f, _temp = 1.0f;
    public string neoToken;
    public GPT3(string token, string engine = "text-davinci-003")
    {
        if(chatgpt == null)
            chatgpt = new ChatGPT();
        this.engine = engine;
        _token = token;
        oldToken = token;
        _e = new Engine() { Owner = "openai", Ready = true, EngineName = engine };
        neoToken = ConfigurationManager.AppSettings["Neo"];
        //  PrimeText = ConfigurationManager.AppSettings["PrimeText"];
    }

    public static string Stats()
    {
        float Cost(int tok) => (tok * 0.006f) / 500.0f;
        return $"General Stats:\nTotal Cost: ${Cost(GPT3.TokensTotal)}\tLog(memory) cost: ${Cost(GPT3.TokensLog)}\tCost Query: ${Cost(GPT3.TokensQuery)}\tCost Response: ${Cost(GPT3.TokensResponse)}]";

    }

    public async Task<string> PostData(string url, string postData, string auth)
    {
        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri(url);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (auth != null) client.DefaultRequestHeaders.Add("Authorization", auth);
        HttpResponseMessage response =
            await client.PostAsync(url, new StringContent(postData, Encoding.UTF8, "application/json"));
        if (response.IsSuccessStatusCode) { return await response.Content.ReadAsStringAsync(); }
        else
        {
            var str = "Fail: " + (await response.Content.ReadAsStringAsync()) + " " + response.ReasonPhrase;

            File.AppendAllText("Neo_Failed_Queries.txt", "\n\n-----------------------------------------------------------\n\n" + postData);

            if (str.HasAny("unauthorized"))
                neoToken = null;

            return str;
        }

        //   return "Fail: " + response.ReasonPhrase;
    }

    public List<string> pairs = new List<string>();


    static string Sanitize(string s)
    {
        var p = s.Replace("\n", "\\n");
        // p = p.Replace("\n", "\\n");
        // p = p.Replace("\"", "'");
        p = p.Replace("\"", "\\\"");
        p = p.Replace("\r", "\\n");
        p = p.Replace("\t", "");
        p = p.Replace("\f", "");
        p = p.Replace("\b", "");
        return p;
    }


  

    public string MakeJson(int history, string q)
    {
        var str = $"{{\"context\":\"This is a discussion with {botName}. {context}. {botName} believes: {memory}. .\"," +
                          $"\"history\":["; //"csrfmiddlewaretoken=gHpMar4oq2WM0qhxXQxEtcPFWS9xDeJ6YFdBDvipep8k2I2dco0uXSIUAPDtFsDR&" +

        var n = history;
        if (n > pairs.Count)
            n = pairs.Count;
        var pa = pairs.GetRange(pairs.Count - n, n).ToList();

        //   pa.Insert(0,"Andy: Bob, can you ask that question?|skip");
        //   pa.Insert(0, "Bob: Dibbr can I slide in your DMs?|Only girls are allowed in my DM's sweetie");
        foreach (var s in pa)
        {
            var p = s;// Sanitize(s);
            var aq = p.Split('|')[0];
            var a = p.Split('|')[1];

            str += $"{{\"input\":\"{aq}\", \"response\":\"{a}\" }},";
        }
        str = str.Trim(',') + "],";

        str += "\"input\":\"" + q + "\"}";

        return str;
    }

    public string memory = "";
    public string context = "Dibbr is knowledgeable, sarcastic, superintelligent and thinks in steps";
    /// <summary>
    /// NLP CLOUD NEO X 20B
    /// </summary>
    /// <param name="q"></param>
    /// <returns></returns>
    public async Task<string> Q2(string q, string user, int depth = 0)
    {
        if (depth == 0)
        {

            if (user.Length != 0)
                q = $"{user}: {q}";
            q = Sanitize(q);
        }
        string url = "https://api.nlpcloud.io/v1/gpu/finetuned-gpt-neox-20b/chatbot";
        string postData = MakeJson(5, q);


        //  postData += "]}";
        if (q.Contains("dumpjson"))
            return postData;

        LastQuery = postData;
        var str = await PostData(url, postData, neoToken);
        if (str.StartsWith("Fail"))
        {
            await Task.Delay(200);
            postData = MakeJson(0, q);
            str = await PostData(url, postData, neoToken);
            if (str.StartsWith("Fail"))
                return str;
            // if (str.Length > 0) str += " [2nd try]";
        }


        Root r = null;
        try
        {
            r = JsonConvert.DeserializeObject<Root>(str);
        }
        catch (Exception e)
        {
            // await Task.Delay(1000);
            //  return str;
            var str2 = $"Neo failed deserialize: Error={e.Message} JSON={str} ";
            r = JsonConvert.DeserializeObject<Root>(str);

            return str2;

        }

        var res = r?.response ?? "";


        if (depth == 0)
            /*     if (res.Lame())
                 {
                     await Task.Delay(500);
                     var oldPairs = pairs.GetRange(0, pairs.Count);
                     pairs.Clear();
                     var answer = (await Q2(q, user, ++depth)) + $" [R{depth}]"; ;
                         if (!answer.Lame())
                             res = answer + $"\n[was {res}]";
                         else
                             res += $"\n[Alt answer: {answer} ]";
                     pairs = oldPairs;
                 }*/
            Console.Out.WriteLine("Neo Returned: " + res);
        if (!res.Lame())
            pairs.Add(q + "|" + Sanitize(res));
        return res;
    }


    public async Task<string> Dalle(string q)
    {
        var d = new Dalle(ConfigurationManager.AppSettings["Dalle"]);
        return await d.Query(q);
    }

    public class Error
    {
        public string message { get; set; }
        public string type { get; set; }
        public string param { get; set; }
        public string code { get; set; }
    }

    public class History
    {
        public string input { get; set; }
        public string response { get; set; }
    }

    public class Root
    {
        public string response { get; set; }
        public List<History> history { get; set; }
    }

    //https://grpc.stability.ai/gooseai.GenerationService/Generate

    public async Task<string> Query2(string prompt)
    {
        // C# code to create a post request to OpenAI's completion endpoint with json and read the results into a string
        while (Req > 0) await Task.Delay(100);
        Req++;

        #region completer

        string url = $"https://api.novelai.net/ai/generate";


        var req = (HttpWebRequest)HttpWebRequest.Create(url);
        var xianalgoos = new NovelAI();
        xianalgoos.input = prompt;
        // xianalgoos.max_tokens = maxLeng;
        // xianalgoos.temperature = 0.95f;
        //  xianalgoos.stop = ".";
        //  xianalgoa.stream = completion;
        string json = JsonConvert.SerializeObject(xianalgoos);
        //      json = json.Remove("\r");
        var client = new HttpClient();
        //  client.DefaultRequestHeaders.Authorization =   new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        var r = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        var str = await r.Content.ReadAsStringAsync();

        #endregion

        var o = JsonConvert.DeserializeObject<NovelAI_Out>(str);
        Req--;
        return o.output;
    }

    static int Req = 0;

  /*  public async Task<string> Query(string prompt)
    {
        // C# code to create a post request to OpenAI's completion endpoint with json and read the results into a string
        while (Req > 0) await Task.Delay(100);
        Req++;

        #region completer

        string url = $"https://api.openai.com/v1/engines/text-davinci-002/completions";


        var req = (HttpWebRequest)HttpWebRequest.Create(url);
        algo xianalgoos = new algo();
        xianalgoos.prompt = prompt;
        // xianalgoos.max_tokens = maxLeng;
        xianalgoos.temperature = 0.95f;
        //  xianalgoos.stop = ".";
        //  xianalgoa.stream = completion;
        string json = JsonConvert.SerializeObject(xianalgoos);
        json = json.Remove("\r");
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        var r = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        var str = await r.Content.ReadAsStringAsync();

        Req--;
        return str;

        byte[] buf = Encoding.UTF8.GetBytes(json);
        req.Method = "POST";
        req.ContentType = "application/json";
        req.ContentLength = buf.Length;
        req.Headers.Add("Authorization", "Bearer " + _token);
        using (Stream stream = req.GetRequestStream()) { await stream.WriteAsync(buf, 0, buf.Length); }

        HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
        Req--;
        if (resp.StatusCode != HttpStatusCode.OK) return ("Error: " + resp.StatusCode);
        StreamReader sr = new StreamReader(resp.GetResponseStream());
        string theResponse = await sr.ReadToEndAsync();
        return theResponse;

        #endregion
    }
  */


    public async Task<string> Chat(string msg)
    {
        return await chatgpt.Next(msg);
    }

    /// <summary>
    ///     Asks OpenAI
    /// </summary>
    /// <param name="q"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<string> Ask(string txt, string msg = "", string user = "user", int maxChars = 2000, bool code = false)
    {
         Console.WriteLine($"{engine} Query: " + (msg.IsNullOrEmpty() ? txt.Length > 30 ? txt[^30..] : txt : (msg.Length > 20 ? msg[..20]:msg)));

        if (_token == null)
        {
            // GPT3 out of credits
         //   return "";// I need an OpenAI.com token. Activate with the command activate <token>";// await Q2(msg, user) + " [GPT3FB]";

        }
        if (txt.Contains("Chat Log"))
        {
            TokensLog += txt.After("Chat Log").Length / 4;
            TokensLog -= msg.Length / 4;
            TokensQuery += msg.Length / 4;
        }
        else TokensQuery += txt.Length;
      
        var old = engine;
        _api = new(_token, new Engine() { Owner = "openai", Ready = true, EngineName = engine });// "text-davinci-003" });
        msg = msg.Remove("#");
    
        //    if (txt.EndsWith("X")) return null;
        // if (!txt.Contains(msg)) txt += msg;
        // Set variables like this
        // dibbr hey ?fp=1&pp=2
        var line = msg.Remove(Program.BotName).Trim();
        if (line.StartsWith("creativity"))
        {
            line = line.After("creativity ");
            var fp = float.Parse(line);
            if (fp > 1) fp /= 10;
            _temp = fp;
            return "Creativity set to " + _temp;
        }

        var help =
            "Frequency penalty (fp): How much to penalize new tokens based on their existing frequency in the text so far. Decreases the model's likelihood to repeat the same line verbatim.\n" +
            @"Presence penalty (pp): How much to penalize new tokens based on whether they appear in the text so far. Increases the model's likelihood to talk about new topics.\n" +
            "Temperature (tp): Controls randomness: Lowering results in less random completions.As the temperature approaches zero, the model will become deterministic and repetitive.";
        //  if (line == "help") return help;
        if(line.Length == 0)
            line = txt[^(30<txt.Length?30:txt.Length)..]; ;
        if (line.Contains("which engine") || line.Contains("what engine")) return "I am using " + engine;
        try
        {
            if (line.Contains("=") && line.Contains("?") && line.Contains("&") && !line.HasAny("http"))
            {
                bool made = false;
                var query = line.Split('?');
                if (query.Length == 2)
                {
                    var q = query[1];
                    if (!q.EndsWith("&")) q += "&";

                    foreach (var pairs in q.Split('&'))
                    {
                        var values = pairs.Split('=');
                        if (values.Length < 2) break;
                        if (values[0] == "pp") _pp = float.Parse(values[1]);

                        if (values[0] == "buffer") maxChars = int.Parse(values[1]);

                        if (values[0] == "fp") _fp = float.Parse(values[1]);

                        if (values[0] == "temp") _temp = float.Parse(values[1]);

                        if (values[0] == "engine")
                        {
                            engine = values[1];
                            _e = new Engine() { Owner = "openai", Ready = true, EngineName = engine };
                            _api = new(_token, _e);
                        }

                        made = true;
                    }

                    var changes = made ? "Changes made" : "Current settings";
                    return $"done";// {help}\n{changes}. fp={_fp} pp={_pp} buffer={maxChars} temp={_temp} engine={engine}";
                }
            }
        }
        catch (Exception e) { return $"Nice try mr hacker , you can't give me a \"{e.Message}\" that easily"; }

        //   txt = txt.Replace(Program.BotName + ":", "doobie" + ":");
        //  var idx = txt.LastIndexOf("doobie:");
        //  if (idx >= 0) txt = txt.Insert(idx, "X");
        //   txt = txt.Replace("Xdoobie:", Program.BotName);

        // Stop dibbr repeating answers
        // txt = txt.Replace($"{Program.BotName}:", "boodlebeep:");
        //  txt += user + ": " + msg + Program.NewLogLine;
        if (code) txt = msg.Remove(Program.BotName).Remove("code ");



        var json = MakeJson(4, Sanitize(msg));

        json = json[..^1];
        var jsonMode = false;
        if (msg.Contains("@@"))
        {
            msg = msg.Remove("@@");
            jsonMode = true;
        }

        if (msg.Contains("dumpjson"))
            return json;
        var r = await Q(jsonMode ? json : txt, code ? 0 : _pp, code ? 0 : _fp, code ? 0 : _temp);

        //if (!code && r.Contains("}"))
        //    r = r.Sub(0, r.LastIndexOf("\"")-2);
        if (r.EndsWith("\""))
            r = r.Sub(0, r.LastIndexOf("\""));

        r = r.Replace("\\n", "\n");
        if (jsonMode)
        {
            var idx = r.LastIndexOfAny(new char[] { '"', '}' });
            if (idx != -1)
                r = r.Substring(0, idx);
            // r += " [JM]";
        }

        var ro = r;
        var (_, response) = r.Deduplicate(txt);
        response = r;
        if (response.StartsWith("\n") && response.Length == 2)
            response = "";

        // if (!r.response.HasAny("don't know", "can't help", "i'm sorry", "not understand", "i didn't understand", "can you please")) ;
        if (response.Length > 0)
            pairs.Add(user + ": " + Sanitize(msg) + "|" + response);
        /*  if (response.Length == 0)
          {
              Console.WriteLine("Running GPT3 API call again!!");
              txt = txt.Remove(r) +" ";
              r = await Q(txt, _pp, _fp, _temp);
              (_, response) = r.Deduplicate(txt);
              if (response.Length <= 2) response = "Internal error. Try again.";
          }*/
        _e = new Engine() { Owner = "openai", Ready = true, EngineName = old };
        _api = new(_token, _e);
     /*   if (engine.Contains("code"))
        {


            response = response.Replace("\n+", "\n");
            response = $"``{response}``";
        }*/

        //  if (ro.Length > 0 && response.Length == 0) r = await Q(msg, _pp, _fp, _temp);
        if (response == "") return r;

        if (response.ToLower().Contains("skip"))
            response = response;

        if (response.Contains("skip", StringComparison.InvariantCultureIgnoreCase))
            response = response;
        if (response.Contains("skip", StringComparison.OrdinalIgnoreCase))
            response = response;

        return response;
    }

    public async Task<string> Test(string tok)
    {
        try
        {
            _api = new(tok, new Engine() { Owner = "openai", Ready = true, EngineName = "text-davinci-003" });
            var result = await _api.Completions.CreateCompletionAsync("Who invented the spork?", temperature: 1, top_p: 1,
                     frequencyPenalty: 1, presencePenalty: 1, max_tokens: 222);
         //   if (result.)
           //     return "";
            return result?.ToString() ?? "";
        }
        catch (Exception e) { }
        return "";
    }

    public DateTime lastNoCredits = DateTime.Now;
    public async Task<string> Q(string txt, float pp = 0.6f, float tp = 0, float temp = 0.1f)
    {
      //  if (_api == null)
        {
            //   _e = new(_engine) {Owner = "openai", Ready = true};
            _api = new(_token?? Program.Get("OpenAI"), new Engine(engine));
            _apiFallback = new(Program.Get("OpenAI"), new Engine(engine));
        }

        int i = 0;
        CompletionResult result = null;
        while (true)
        {
            if (i > 0 && txt.Length > 1000)
                txt = txt[^1000..];
            // string r = "";
           
          //  if (i > 0)
            //    txt += "I";
          //  if (i > 1)
            //    txt = txt.Sub(txt.LastIndexOfAny(new char[] { '\n', '@', ':' })) + "\nAnswer:";
            var derp = engine != "text-davinci-003";
            var ops = new[] { "@@",":" };
            if (txt.Length > 2000) txt = txt[^2000..]; // hrow new Exception("too big");
            TokensTotal += txt.Length / 4;
            LastQuery = txt;
            var strE = "";
            try
            {
                Console.WriteLine($"GPT Request: {txt.Substring(0,20)}");;
                result = await _api.Completions.CreateCompletionAsync(txt, temperature: temp, top_p: 1,
                   frequencyPenalty: tp, presencePenalty: pp, max_tokens: 1400,
                   stopSequences: ops);

            }
            catch (Exception e)
            {
                File.AppendAllText("GPT3__Failed_Queries.txt", "\n\n-----------------------------------------------------------\n\n" + txt);
                strE = e.Message;
                Console.WriteLine(e.ToString());
                if(e.Message.Contains("TooManyRequests"))
                    await Task.Delay(1000); 
                result = null;
            }
            if(result?.ToString().Trim().Length < 3)
            {
                Console.WriteLine("GPT3:" + result.ToString());
            }
            if (result == null || result.ToString().Trim() is null or "")
            {
                Console.WriteLine("GPT3 Response Failure:" + result?.ToString() ?? "OpenAI CALL FAILED WITH NO REASON");

                if (i++ == 0)
                {
                    _api = _apiFallback;
                    _token = _apiFallback.Auth.ApiKey;
                    continue;
                }

                if (i > 1)
                {
                   

                  //  _token = null;
                   // if ((DateTime.Now - lastNoCredits).TotalSeconds > 20)
                    {
                        lastNoCredits = DateTime.Now;
                        _token = null;
                       // foreach(var b in )
                        return $"invalid token";// + strE[0..20];
                    }
                   // else
                   // {
                   //     lastNoCredits = DateTime.Now;
                   //     return "[OpenAI Error]";
                   // }
                }
              
                if (txt.Trim().Length < txt.Length)
                    txt = txt.Trim();
               // if (_api.UsingEngine.EngineName.Contains("text-davinci"))
                 //   _api.UsingEngine = "davinci-instruct-beta";
                 _api.UsingEngine = engine;
                continue;
            }
            break;
        }
        if (i > 0)
        {
            Console.WriteLine($"Apparently switching engines or something fixed it. Currently on {_api.UsingEngine.EngineName}");
        }
        try
        {
            txt = txt.Replace("\uDE8A", "");
            File.AppendAllText("GPT3_Queries.txt", "\n\n-----------------------------------------------------------\n\n" + txt);
        }
        catch (Exception e) { }
        var r = result.ToString();
        //if (i > 0) r = "I" + r;

        var tokR = (r.Length) / 4;
        TokensResponse += tokR;
        TokensTotal += tokR;

        if(txt.Contains(":") && txt.Contains("\n"))
            txt = txt.Substring(0, txt.LastIndexOf("\n"));

        if (txt.Length >= 2000)
            r +=
                $"[ {Program.BotName} is using a lot of tokens. Maybe boss should be warned, unless you did this on purpose.]";


        Console.WriteLine("GPT3 response: " + (r.Length > 70 ? r[..70]:r));

        return r;

    }
}

/// <summary>
///     LevenshteinDistance and other string functions
/// </summary>
static class StringHelpers
{
    public static string TrimExtra(this string str)
    {
        return str.Trim(new char[] { ',', ' ', '[', ':', '\t', '@', '\"', '.', '\n', '\r','(',')',']' });
    }

    public static string After(this string str, string s)
    {
        var idx = str.ToLower().IndexOf(s.ToLower());
        if (idx == -1) return "";
        return str.Substring(idx + s.Length).TrimExtra();
    }

    public static string Before(this string str, string s, bool defaultWhole=false)
    {
        var idx = str.ToLower().IndexOf(s.ToLower());
        if (idx == -1) return defaultWhole?str:"";
        return str.Substring(0, idx);
    }

    public static string BeforeAll(this string str, params string[] values)
    {
        var strl = str;

        foreach (string value in values)
        {
            var sx = str.Before(value);
            if (sx.Length < strl.Length && sx.Length > 0)
                strl = sx;
        }

        return strl;
    }

    public static string AfterAll(this string str, params string[] values)
    {
        var strl = str;

        foreach (string value in values)
        {
            var sx = str.After(value);
            if (sx.Length < strl.Length && sx.Length > 0)
                strl = sx;
        }

        return strl;
    }

    public static (int dupePercent, string uniquePart) Deduplicate(this string r, string log)
    {
        // return (0, r);
        if (r == null) return (0, "");
        // Split on !?,.
        var split = Regex.Split(r, @"(?<=[.?!]\s+)");
        var percentMatch = 0;
        var response = "";
        foreach (var s in split)
        {
            if (log.Contains(s) && s.Length > 20)
                percentMatch += s.Length;
            else
                response += s;
        }
        response = response.Trim(new char[] { '\n', '\r', ' ' });
        return (percentMatch, response);
    }


    public static float Get(string s, string t)
    {
        if (t == null || s == null) return 1;

        float len = Math.Max(s.Length, t.Length);
        // normalize by length, high score wins
        var ret = (len - Compute(s, t)) / len;
        Console.WriteLine($"{ret} for \n{s}\n{t}");
        return ret;
    }

    public static int Compute(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
        {
            if (string.IsNullOrEmpty(t)) return 0;

            return t.Length;
        }

        if (string.IsNullOrEmpty(t)) return s.Length;

        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        // initialize the top and right of the table to 0, 1, 2, ...
        for (var i = 0; i <= n; d[i, 0] = i++) ;

        for (var j = 1; j <= m; d[0, j] = j++) ;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = t[j - 1] == s[i - 1] ? 0 : 1;
                var min1 = d[i - 1, j] + 1;
                var min2 = d[i, j - 1] + 1;
                var min3 = d[i - 1, j - 1] + cost;
                d[i, j] = Math.Min(Math.Min(min1, min2), min3);
            }
        }

        return d[n, m];
    }
}


public class Parameters
{
    public double temperature { get; set; } = 0.72;
    public int max_length { get; set; } = 2000;
    public int min_length { get; set; } = 15;
    public int top_k { get; set; }
    public double top_p { get; set; } = 0.725;
    public int tail_free_sampling { get; set; } = 1;
    public double repetition_penalty { get; set; } = 1.1f;
    public int repetition_penalty_range { get; set; } = 2048;
    public double repetition_penalty_slope { get; set; } = 0.18;
    public int repetition_penalty_frequency { get; set; }

    public int repetition_penalty_presence { get; set; }

    // public List<List<int>> bad_words_ids { get; set; } = new List<List<int>>(1);
    public bool generate_until_sentence { get; set; } = false;
    public bool use_cache { get; set; }
    public bool use_string { get; set; } = true;

    public bool return_full_text { get; set; } = true;
    //  public string prefix { get; set; } = "vanilla";
    //   public List<int> order { get; set; }
}

public class NovelAI
{
    public string input { get; set; }
    public string model { get; set; } = "krake-v2"; //"euterpe-v2";
    public Parameters parameters { get; set; } = new Parameters();
}

public class NovelAI_Out
{
    public string output { get; set; }
}