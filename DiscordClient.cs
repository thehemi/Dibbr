﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

//using DSharpPlus.CommandsNext;

namespace DibbrBot;


/// <summary>
///     API Client based discord access. More features than DiscordV1, notably you can use with bots
///     Doesn't support selfbots
/// </summary>


public static class StringHelp
{
    public static int Inc<T>(this IDictionary<T, int> input, T key)
    {
        if (input.TryGetValue(key, out var val))
        {
            input[key] = val + 1;
            return val + 1;
        }
        else
        {
            if(!input.TryAdd(key, 1))
                input[key]++;
            return 1;
        }

    }
    public static string Sub(this string input, int start, int end=-1)
    {
        if (start == -1) return "";// start = 0;
        if (end+start > input.Length || end == -1) end = Math.Max(0,input.Length - start);
        
        var result = input.Substring(start, end);
        return result;
    }


    public static string MakeValidFileName(this string name)
    {
        string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

        return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
    }

    public static string Rem(this string str, params string[] values)
    {
        var strl = str;
        foreach (string value in values)
        {
            strl = strl.Replace(value, "", StringComparison.InvariantCultureIgnoreCase);
        }

        return strl;
    }

    public static string Rem(this string input, string search, string replacement = "")
    {
        return input.Replace(search, replacement, StringComparison.InvariantCultureIgnoreCase);
    }
    public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue x = default(TValue))
    {
        dict.TryGetValue(key, out var value);
        if (value == null)
        {
            value = x;
            dict.Add(key, value);
        }
        return value;
        
    }

    public static TValue GetOrCreate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, TValue x = default(TValue))
    {
        dict.TryGetValue(key, out var value);
        if (value == null)
        {
            value = x;
            dict.GetOrAdd(key, value);
        }
        return value;

    }
    // When bot replies "Don't know", etc, we'll consider that a lame response
    public static bool Lame(this string xt)
    {
        var lame = (xt is null or "") || xt.Contains("dibbr is a superintelligent") || ((xt.Length < "I don't know the answer to this, but ".Length && xt.HasAny("skip","rate limit","I don't understand", "not sure", "didn't understand", "please try again", "no idea", "I don't remember", "i don't know", "'m sorry I can't", "don't know", "not sure", "can't answer","inappropriate","another topic")));
        if (lame)
            lame = lame;

        return lame;
    }
    public static string Remove(this string str, string s) =>
        Regex.Replace(str, Regex.Escape(s), "", RegexOptions.IgnoreCase);

    public static string Trim(this string str, string s)
    {
        if(str.StartsWith(s,StringComparison.InvariantCultureIgnoreCase))
            return str.Substring(s.Length);
        if(str.EndsWith(s, StringComparison.InvariantCultureIgnoreCase))
            return str.Substring(0, str.Length - s.Length);
        return str;
    }
    public static List<string> Last(this List<string> list, int count) => (count >= list.Count) ? list : list.ToArray()[^count..].ToList();
    public static List<string> TakeLastLines(this string text, int count)
    {
        var lines = Enumerable.ToArray(text.Split(Program.NewLogLine));
        if (lines.Last().Trim() == "") lines = lines[..^1];

        var newLines = new List<string>();
        for (var index = 0; index < lines.Length; index++)
        {
            var l = lines[index];
            if (l.Length < 4) continue;
            lines[index] = "@@" + l;
            newLines.Add(lines[index]);
        }

        if (newLines.Count > count) return newLines.ToArray()[^count..].ToList();

        return lines.ToList();
    }
}