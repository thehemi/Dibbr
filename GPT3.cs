// (c) github.com/thehemi
// using System;

using System;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenAI_API;

// ReSharper disable StringIndexOfIsCultureSpecific.1

namespace DibbrBot;

/// <summary>
///     Chat system that uses the OpenAI API to send and receive messages
/// </summary>
public class Gpt3
{
    readonly string _token;
    OpenAIAPI _api;
    Engine _e;
    string _engine = "text-davinci-002";
    float _fp, _pp = 0.5f, _temp = 1;

    public Gpt3(string token) => this._token = token;

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
            _e = new(_engine) {Owner = "openai", Ready = true};
            _api = new(_token, _e);
        }

        Console.WriteLine("GPT3 Query: " + msg);


        // Set variables like this
        // dibbr hey ?fp=1&pp=2
        var line = msg;
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
                        if (values[0] == "pp") _pp = float.Parse(values[1]);

                        if (values[0] == "buffer") maxChars = int.Parse(values[1]);

                        if (values[0] == "fp") _fp = float.Parse(values[1]);

                        if (values[0] == "temp") _temp = float.Parse(values[1]);

                        if (values[0] == "engine")
                        {
                            _engine = values[1];
                            _engine = _engine.Contains("code") ? "code-davinci-002" : "text-davinci-002";

                            _e = new(_engine) {Owner = "openai", Ready = true};
                            _api = new(_token, _e);
                        }
                    }

                    return $"Changes made. Now, fp={_fp} pp={_pp} buffer={maxChars} temp={_temp} engine={_engine}";
                }
            }
        }
        catch (Exception e) { return $"Nice try mr hacker {user}, you can't give me a \"{e.Message}\" that easily"; }

        string MakeText()
        {
            if (log.Length > maxChars) log = log[^maxChars..].Trim();

            if (log == "") log += user + ": " + msg;

            if (_e.EngineName == "code-davinci-002") return log.TakeLastLines(1)[0] + "\n" + endtxt;

            return ConfigurationManager.AppSettings["PrimeText"] + "\n\n" + log + endtxt;
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

        return r;


        async Task<string> Q(string txt, float pp, float tp, float temp)
        {
            var result = await _api.Completions.CreateCompletionAsync(txt, temperature: temp, top_p: 1,
                frequencyPenalty: tp, presencePenalty: pp, max_tokens: 1000, stopSequences: new[] {Program.NewLogLine});
            // var r = CleanText(result.ToString());
            var r = result.ToString().Trim().Rem($"{Program.BotUsername}:");
            Console.WriteLine("GPT3 response: " + r);

            var (_, response) = r.Deduplicate(log);

            if (response.Length < r.Length)
            {
                log = "";
                txt = MakeText();
                response = (await _api.Completions.CreateCompletionAsync(txt, temperature: temp, top_p: 1,
                    frequencyPenalty: tp, presencePenalty: pp, max_tokens: 1000,
                    stopSequences: new[] {Program.NewLogLine})).ToString();
                // var r = CleanText(result.ToString());
            }
            // If dup, try again                
            //if (percentDupe > r.Length / 3)
            // return await Q(MakeText(), pp, fp, 1);

            return response;
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