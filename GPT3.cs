// (c) github.com/thehemi
// using System;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
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
    public string PrimeText = "";

    string engine;

    //string _engine = "text-davinci-002";
    float _fp = 0.0f, _pp = 0.0f, _temp = 0.7f;

    public Gpt3(string token, string engine)
    {
        this.engine = engine;
        this._token = token;
        _e = new Engine() {Owner = "openai", Ready = true, EngineName = engine};

        PrimeText = ConfigurationManager.AppSettings["PrimeText"];
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
    public async Task<string> Ask(string msg, string log, string user = "", string endtxt = "dibbr's response: ",
                                  int maxChars = 2000)
    {
        if (_api == null)
        {
            //   _e = new(_engine) {Owner = "openai", Ready = true};
            _api = new(_token, _e);
            _apiFallback = new(_token, new Engine("text-davinci-002"));
        }


        Console.WriteLine("GPT3 Query: " + (msg.IsNullOrEmpty() ? log.Length > 30 ? log[^30..] : log : msg));

        // Set variables like this
        // dibbr hey ?fp=1&pp=2
        var line = msg;
        if (line.Contains("which engine") || line.Contains("what engine")) return "I am using " + engine;
        try
        {
            if (line.Contains("=") && line.Contains("?"))
            {
                var query = line.Split('?');
                if (query.Length == 2)
                {
                    var q = query[1];
                    if (!q.Contains("&")) q += "&";

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
                            var _engine = values[1];
                            //_engine = _engine.Contains("code") ? "code-davinci-002" : "text-davinci-002";

                            _e = new(_engine) {Owner = "openai", Ready = true};
                            _api = new(_token, _e);
                        }
                    }

                    return $"Changes made. Now, fp={_fp} pp={_pp} buffer={maxChars} temp={_temp} ";
                }
            }
        }
        catch (Exception e) { return $"Nice try mr hacker {user}, you can't give me a \"{e.Message}\" that easily"; }

        string MakeText()
        {
            if (log.Length > maxChars) log = log[^maxChars..].Trim();

            if (log == "") log += user + ": " + msg;

            //  if (_e.EngineName == "code-davinci-002") return log.TakeLastLines(1)[0] + "\n" + endtxt;

            // log = log.Replace(": " + Program.BotName, "");
            //log = Regex.Replace(log, ": " + Program.BotName, ": ", RegexOptions.IgnoreCase);

            return PrimeText + "\n\n" + log + endtxt;
        }

        // Setup context, insert chat history
        var txt = MakeText();

        //   txt = txt.Replace(Program.BotName + ":", "doobie" + ":");
        //  var idx = txt.LastIndexOf("doobie:");
        //  if (idx >= 0) txt = txt.Insert(idx, "X");
        //   txt = txt.Replace("Xdoobie:", Program.BotName);

        // Stop dibbr repeating answers
        // txt = txt.Replace($"{Program.BotName}:", "boodlebeep:");
        //  txt += user + ": " + msg + Program.NewLogLine;
        var r = await Q(txt, _pp, _fp, _temp);
        if (r == null) return "";
        if (r.Contains("filthy, woke")) { r = await Q(txt, _pp, _fp, _temp); }

        return r;


        async Task<string> Q(string txt, float pp, float tp, float temp)
        {
            while (true)
            {
                // string r = "";
                CompletionResult result = null;
                try
                {
                    if (txt.Length > 3000) throw new Exception("too big");
                    result = await _api.Completions.CreateCompletionAsync(txt, temperature: temp, top_p: 1,
                        frequencyPenalty: tp, presencePenalty: pp, max_tokens: 1000,
                        stopSequences: new[] {Program.NewLogLine});
                }
                catch (Exception e) { return $"{Program.BotName} has a server error: {e.Message}"; }

                if (result == null) { return "dibr had a server error "; }

                // var r = CleanText(result.ToString());
                var r = result.ToString().Trim().Rem($"{Program.BotUsername}:");
                //  try { r = await Query(txt); }
                //  catch (Exception e) { return $"{Program.BotName} has a server error: {e.Message}"; }

                Console.WriteLine("GPT3 response: " + r);
                // Remove quotes
                if (r.StartsWith("\"")) r = r.Substring(1);
                if (r.EndsWith("\"")) r = r.Substring(0, r.Length - 1);

                var (_, response) = r.Deduplicate(log);

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
                return response;
            }
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
        var idx = str.LastIndexOf(s);
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