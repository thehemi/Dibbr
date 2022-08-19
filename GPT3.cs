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
using Newtonsoft.Json;
using OpenAI_API;
using ServiceStack;

// ReSharper disable StringIndexOfIsCultureSpecific.1

namespace DibbrBot;

/// <summary>
///     Chat system that uses the OpenAI API to send and receive messages
/// </summary>
public class Gpt3
{
    public string _token;
    OpenAIAPI _api, _apiFallback;
    Engine _e;

    public string engine;

    //string _engine = "text-davinci-002";
    float _fp = 0.7f, _pp = 1.1f, _temp = 1.0f;

    public Gpt3(string token, string engine)
    {
        this.engine = engine;
        this._token = token;
        _e = new Engine() {Owner = "openai", Ready = true, EngineName = engine};

        //  PrimeText = ConfigurationManager.AppSettings["PrimeText"];
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
            Console.WriteLine(url + " Fail: " + response.ReasonPhrase);
            return await response.Content.ReadAsStringAsync();
        }

        return "Fail: " + response.ReasonPhrase;
    }

    public List<string> pairs = new List<string>();


    static string Sanitize(string s)
    {
        var p = s.Replace("\n", "\\n");
        p = p.Replace("\n", "\\n");
        // p = p.Replace("\"", "'");
        p = p.Replace("\"", "\\\"");
        p = p.Replace("\r", "\\n");
        p = p.Replace("\t", "");
        p = p.Replace("\f", "");
        p = p.Replace("\b", "");
        return p;
    }

    /// <summary>
    /// NLP CLOUD NEO X 20B
    /// </summary>
    /// <param name="q"></param>
    /// <returns></returns>
    public async Task<string> Q2(string q)
    {
        q = Sanitize(q);
        string url = "https://api.nlpcloud.io/v1/gpu/finetuned-gpt-neox-20b/chatbot";
        string postData = "{\"input\":\"" + q +
                          "\"," +
                          "\"context\":\"This is a discussion between a human and an AI. The AI is named dibbr. Dibbr is woke, sarcastic, superintelligent.\","+
                          "\"history\":[{\"input\":\"Hello dibbr. Tell me about yourself, and the date\", \"response\":\"Hi there. I am a super-intelligent, woke, sarcastic AI (っ◕‿◕), what's up?. The date is " +
                          DateTime.Now.ToString("R") +
                          "\" }"; //"csrfmiddlewaretoken=gHpMar4oq2WM0qhxXQxEtcPFWS9xDeJ6YFdBDvipep8k2I2dco0uXSIUAPDtFsDR&" +

        //  if (wipe) pairs = new List<string>();
        if (pairs.Count > 10) pairs = pairs.ToArray()[^10..].ToList();
        foreach (var s in pairs)
        {
            var p = Sanitize(s);
            var aq = p.Split('|')[0];
            var a = p.Split('|')[1];

            postData += $",{{\"input\":\"{aq}\", \"response\":\"{a}\" }}";
        }

        postData += "]}";
        var str = await PostData(url, postData, ConfigurationManager.AppSettings["Neo"]);
        if (str == null || str.StartsWith("Fail")) return str;
        Root r = null;
        try
        {
            r = JsonConvert.DeserializeObject<Root>(str);
        }catch(Exception e) { return e.Message; }
        // Kill history on a fail or loop, and re-request
        if (r == null || (pairs.Count > 0 && pairs.Last().Contains(r.response)))
        {
            pairs.Clear();
            return await Q2(q);
        }

        if (r == null) return "Neo Failed to deserialize " + str;
        Console.Out.WriteLine("Neo Returned: " + r.response);
        pairs.Add(q + "|" + r.response);
        return r.response;
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

    public async Task<string> Query2(string prompt)
    {
        // C# code to create a post request to OpenAI's completion endpoint with json and read the results into a string
        while (Req > 0) await Task.Delay(100);
        Req++;

        #region completer

        string url = $"https://api.novelai.net/ai/generate";


        var req = (HttpWebRequest) HttpWebRequest.Create(url);
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

    public async Task<string> Query(string prompt)
    {
        // C# code to create a post request to OpenAI's completion endpoint with json and read the results into a string
        while (Req > 0) await Task.Delay(100);
        Req++;

        #region completer

        string url = $"https://api.openai.com/v1/engines/text-davinci-002/completions";


        var req = (HttpWebRequest) HttpWebRequest.Create(url);
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

        HttpWebResponse resp = (HttpWebResponse) req.GetResponse();
        Req--;
        if (resp.StatusCode != HttpStatusCode.OK) return ("Error: " + resp.StatusCode);
        StreamReader sr = new StreamReader(resp.GetResponseStream());
        string theResponse = await sr.ReadToEndAsync();
        return theResponse;

        #endregion
    }

    /// <summary>
    ///     Asks OpenAI
    /// </summary>
    /// <param name="q"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<string> Ask(string txt, string msg = "", int maxChars = 2000, bool code = false)
    {
        Console.WriteLine("GPT3 Query: " + (msg.IsNullOrEmpty() ? txt.Length > 30 ? txt[^30..] : txt : msg));
        var old = engine;
        if(msg.Contains("#1")) { _api = new(_token, new Engine() { Owner = "openai", Ready = true, EngineName = "davinci" }); }
        if (msg.Contains("#2")) { _api = new(_token, new Engine() { Owner = "openai", Ready = true, EngineName = "davinci-instruct-beta" }); }
        if (msg.Contains("#3")) { _api = new(_token, new Engine() { Owner = "openai", Ready = true, EngineName = "text-davinci-001" }); }
        if (msg.Contains("#4")) { _api = new(_token, new Engine() { Owner = "openai", Ready = true, EngineName = "text-davinci-002" }); }
        msg = msg.Remove("#");
        if (code || engine.Contains("code"))
        {
            if(txt.ToLower().StartsWith("write"))
            {
                txt = $"/* {txt} */";
            }
           // _e = new Engine() {Owner = "openai", Ready = true, EngineName = "code-davinci-002"};

           /// _api = new(_token, _e);
        }

        //    if (txt.EndsWith("X")) return null;
        // if (!txt.Contains(msg)) txt += msg;
        // Set variables like this
        // dibbr hey ?fp=1&pp=2
        var line = msg.Remove("dibbr").Trim();
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
        if (line.Contains("which engine") || line.Contains("what engine")) return "I am using " + engine;
        try
        {
            if (line.Contains("=") && line.Contains("?") && line.Contains("&"))
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
                            _e = new Engine() {Owner = "openai", Ready = true, EngineName = engine};
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
        var r = await Q(txt, code ? 0 : _pp, code ? 0 : _fp, code ? 0 : _temp);

        var ro = r;
        var (_, response) = r.Deduplicate(txt);
        if (response.StartsWith("\n") && response.Length == 2)
            response = "";
        if (response.Length == 0)
        {
            Console.WriteLine("Running GPT3 API call again!!");
            txt = txt.Remove(r) +" ";
            r = await Q(txt, _pp, _fp, _temp);
            (_, response) = r.Deduplicate(txt);
            if (response.Length <= 2) response = "Internal error. Try again.";
        }
        _e = new Engine() { Owner = "openai", Ready = true, EngineName = old };
        _api = new(_token, _e);
        if (engine.Contains("code"))
        {
            

            response = response.Replace("\n+", "\n");
            response = $"``{response}``";
        }

        //  if (ro.Length > 0 && response.Length == 0) r = await Q(msg, _pp, _fp, _temp);
        if (response == "") return r;
        return response;
    }

    public async Task<string> Q(string txt, float pp = 0, float tp = 0, float temp = 0.7f)
    {
        if (_api == null)
        {
            //   _e = new(_engine) {Owner = "openai", Ready = true};
            _api = new(_token, _e);
            _apiFallback = new(_token, new Engine(engine));
        }

        int i = 0;
        while (true)
        {
            if (i > 0 && txt.Length > 1000)
                txt = txt[^1000..];
            // string r = "";
            CompletionResult result = null;
            try
            {
                if (i > 1)
                    txt += "I";
                var derp = engine == "davinci";
                var ops = new[] { Program.NewLogLine, "B (from", "@@", $"{Program.BotName}:" };
                if (derp) ops = new[] { "[", "@@", $";" };
                if (txt.Length > 3000) txt = txt[^3000..]; // hrow new Exception("too big");
                 result = await _api.Completions.CreateCompletionAsync(txt, temperature: temp, top_p: 1,
                    frequencyPenalty: tp, presencePenalty: pp, max_tokens: derp?300:1400,
                    stopSequences: ops);

                
                if (i > 2)
                    return "Some error occured try agfain";
            }
            catch (Exception e) { 
                if(i>2)
                    return $"I have no credits left :-( Sign up for a free key at openai.com and DM to dibbr to fix me. DM dabbr for help. " + e.Message[..20]; }
            i++;
            if (result == null) continue;//
            //{ return $"{Program.BotName} had a server error "; }


            var r = result.ToString();
            //r.After(":").TrimExtra();


            if (txt.Length >= 3000)
                r +=
                    $" {Program.BotName} is using a lot of tokens. Maybe [bot admin] should be warned, unless you did this on purpose.";


            Console.WriteLine("GPT3 response: " + r);
            if(r.Trim().Trim('\n','\r').Length > 1)
            return r;
        }
    }
}

/// <summary>
///     LevenshteinDistance and other string functions
/// </summary>
static class StringHelpers
{
    public static string TrimExtra(this string str)
    {
        var r = str.Trim();
        if (r.StartsWith("\"")) r = r.Substring(1);
        if (r.EndsWith("\"")) r = r.Substring(0, r.Length - 1);
        if (r.StartsWith("@")) r = r.Remove("@").Trim();
        if (r.EndsWith("@")) r = r.Remove("@").Trim();
        str = r;
        return str;
    }

    public static string After(this string str, string s)
    {
        var idx = str.ToLower().LastIndexOf(s.ToLower());
        if (idx == -1) return str;
        return str.Substring(idx + s.Length).Trim();
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
            if (log.Contains(s) && s.Length > 10)
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

class algo
{
    public string prompt { get; set; }
    public int max_tokens { get; set; } = 2000;
    public double temperature { get; set; } = 0.7f;
    public int top_p { get; set; } = 1;
    public int frequency_penalty { get; set; } = 0;
    public int presence_penalty { get; set; } = 0;
    public int best_of { get; set; } = 1;
    public bool echo { get; set; } = true;
    public int logprobs { get; set; } = 0;
    public bool stream { get; set; } = false;
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