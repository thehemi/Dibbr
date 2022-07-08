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

    public async Task<string> PostData(string url, string postData)
    {
        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri(url);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("Authorization", "Token e2b2ce888877e744a4a7a13f4b7ef62454612706");
        HttpResponseMessage response =
            await client.PostAsync(url, new StringContent(postData, Encoding.UTF8, "application/json"));
        if (response.IsSuccessStatusCode) { return await response.Content.ReadAsStringAsync(); }
        else
            Console.WriteLine("NEO-X Fail: " + response.ReasonPhrase);

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
                          "\",\"history\":[{\"input\":\"Hello dibbr. Tell me about yourself, and the date\", \"response\":\"Hi there. I am a super-intelligent, woke, sarcastic AI made by dabbr (っ◕‿◕). The date is " +
                          DateTime.Now.ToString("R") +
                          "\" }"; //"csrfmiddlewaretoken=gHpMar4oq2WM0qhxXQxEtcPFWS9xDeJ6YFdBDvipep8k2I2dco0uXSIUAPDtFsDR&" +

        // if (wipe) pairs = new List<string>();
        if (pairs.Count > 10) pairs = pairs.ToArray()[^10..].ToList();
        foreach (var s in pairs)
        {
            var p = Sanitize(s);
            var aq = p.Split('|')[0];
            var a = p.Split('|')[1];

            postData += $",{{\"input\":\"{aq}\", \"response\":\"{a}\" }}";
        }

        postData += "]}";
        var str = await PostData(url, postData);
        if (str == null || str.StartsWith("Fail")) return str;
        var r = JsonConvert.DeserializeObject<Root>(str);
        Console.Out.WriteLine("NEO X: " + r.response);
        pairs.Add(q + "|" + r.response);
        return r.response;
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
    public async Task<string> Ask(string txt, string msg = "", int maxChars = 2000)
    {
        Console.WriteLine("GPT3 Query: " + (msg.IsNullOrEmpty() ? txt.Length > 30 ? txt[^30..] : txt : msg));

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
        if (line == "help") return help;
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
                    return $"{help}\n{changes}. fp={_fp} pp={_pp} buffer={maxChars} temp={_temp} engine={engine}";
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
        if (engine.Contains("code")) txt = msg;
        var r = await Q(txt, _pp, _fp, _temp);
        var ro = r;
        var (_, response) = r.Deduplicate(txt);
        //  if (ro.Length > 0 && response.Length == 0) r = await Q(msg, _pp, _fp, _temp);
        return r;
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
            // string r = "";
            CompletionResult result = null;
            try
            {
                txt = txt.Replace("dabbr", "T1mothy").Trim();

                if (txt.Length > 3000) txt = txt[..^3000]; // hrow new Exception("too big");
                result = await _api.Completions.CreateCompletionAsync(txt, temperature: temp, top_p: 1,
                    frequencyPenalty: tp, presencePenalty: pp, max_tokens: 1400,
                    stopSequences: new[] {Program.NewLogLine, "\", said", "@@", "Hawking:"});
            }
            catch (Exception e)
            {
                i++;
                if (i > 1) return $"{Program.BotName} has no credits left maybe? error " + e.Message[..20];
                //  _token = ConfigurationManager.AppSettings["OpenAI" + i];
                // _api = new(_token, _e);
                await Task.Delay(4);
                continue;
                //  return
                return
                    $"{Program.BotName} has a server error:"; // {Regex.Escape(e.Message).Replace("\"", "").Replace("{", "").Replace("}", "").Replace("\r", "")}";
            }

            if (result == null) { return "dibr had a server error "; }

            // var r = CleanText(result.ToString());
            var r = result.ToString().Trim(); //.Rem($"{Program.BotUsername}:");
            r = r.Replace("T1mothy", "dabbr");
            r = r.After("conclusion:");
            if (txt.Length >= 3000)
                r += " Dibbr is using a lot of tokens. Maybe dabbr should be warned, unless you did this on purpose.";

            //  try { r = await Query(txt); }
            //  catch (Exception e) { return $"{Program.BotName} has a server error: {e.Message}"; }

            Console.WriteLine("GPT3 response: " + r);
            // Remove quotes
            if (r.StartsWith("\"")) r = r.Substring(1);
            if (r.EndsWith("\"")) r = r.Substring(0, r.Length - 1);
            if (r.StartsWith("@")) r = r.Remove("@").Trim();
            if (r.EndsWith("@")) r = r.Remove("@").Trim();

            /* if (response.Length < r.Length || r.Length == 0)
             {
                 log = "";
                 txt = MakeText();
                 response = (await _api.Completions.CreateCompletionAsync(txt, temperature: temp, top_p: 1,
                     frequencyPenalty: tp, presencePenalty: pp, max_tokens: 1000,
                     stopSequences: new[] {Program.NewLogLine})).ToString();
                 // var r = CleanText(result.ToString());
                 (_, response) = r.Deduplicate(log);
             }*/

            // If dup, try again                
            //if (percentDupe > r.Length / 3)
            // return await Q(MakeText(), pp, fp, 1);
            //  response = response.After(":");
            return r;
        }
    }

    /// <summary>
    ///     Asks OpenAI, but as a completion without history
    /// </summary>
    /// <param name="q"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<string> Ask2(string q, string user = "")
    {
        if (_api == null)
        {
            var eng = new Engine("text-davinci-002") {Owner = "openai", Ready = true};
            var k = _token;
            _api = new(k, eng);
        }

        var stops = new[] {Program.BotName + ":"};
        var result = await _api.Completions.CreateCompletionAsync(q, temperature: 0.8, top_p: 1, max_tokens: 1000,
            stopSequences: stops);

        var r = result.ToString();
        Console.WriteLine("GPT3 response: " + r);
        return r;
    }
}

/// <summary>
///     LevenshteinDistance and other string functions
/// </summary>
static class StringHelpers
{
    public static string After(this string str, string s)
    {
        var idx = str.ToLower().LastIndexOf(s.ToLower());
        if (idx == -1) return str;
        return str.Substring(idx + s.Length);
    }

    public static (int dupePercent, string uniquePart) Deduplicate(this string r, string log)
    {
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