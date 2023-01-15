using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


using System.Net.Http.Headers;
using DibbrBot;
using System.IO;

namespace dibbr;

public class Infer
{
    public partial class Temperatures
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("score")]
        public double Score { get; set; }
    }

    public class User
    {
        public int Messages;
        public string Name;
        public Dictionary<string, double> Temperatures { get; set; } = new Dictionary<string,double>();
    }

    public Infer()
    {
        if (File.Exists("infer.txt") && Users == null)
        {
            Users = JsonConvert.DeserializeObject<Dictionary<string, User>>
                (File.ReadAllText("infer.txt"));
            Console.WriteLine($"Loaded {Users.Keys.Count} infer keys!");
        }
        else if(Users == null)
            Users = new Dictionary<string, User>();
    }

    public string GetTop(string key)
    {
        key = key.ToLower();
        if (key.HasAny("happiest", "happy","joyful"))
            key = "joy";
        if (key.HasAny("saddest", "sad"))
            key = "sadness";
        if (key.HasAny("angriest"))
            key = "anger";
        var str = $"{key} rank by user:\n";
        try
        {
            var top = from entry in Users
                      where entry.Value.Temperatures.Values.Count() > 0 && entry.Value.Messages > 0
                      orderby (entry.Value.Temperatures[key] / entry.Value.Messages) descending
                      select entry.Value;

        
            var t = top.ToList();

           
            for (int i = 0; i < t.Count; i++)
            {
                str += $"{i+1}. {t[i].Name}:\t\t {((t[i].Temperatures[key] / t[i].Messages) * 100).ToString("F0")}%\n";


                // Show First 5, then last 5
                if (i >= 5)
                {
                    var i2 = t.Count - 5;
                    if (i2 > i)
                    {
                        i = i2;
                        str += "...\n";
                    }

                }
            }
        }catch(Exception e) { str = e.Message; }
        return str;
    }

    public string GetScores(string user)
    {
        var usr = Users.GetOrCreate(user, new User() { Name = user });
        if (usr.Messages == 0)
        {
            usr = Users.Where(u=>u.Value.Name.ToLower().Contains(user.ToLower()) ).FirstOrDefault().Value;
            if(usr == null)
             return "You have no score";
        }
        var str = $"{user}'s emotional state based on {usr.Messages} messages is ";
        var temps = usr.Temperatures;
        var sortedDict = from entry in temps orderby entry.Value descending select entry;
        foreach (var t in sortedDict)
        {
            str += ((t.Value / usr.Messages)*100.0f).ToString("F0") + $"% {t.Key}, ";
        }
        return str.Trim(new char[] {' ',','});
    }
    Dictionary<string, User> Users;
    public async Task Emotion(string user, string chat, string apiToken = "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")
    {
        HttpClient client = new HttpClient();

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api-inference.huggingface.co/models/j-hartmann/emotion-english-distilroberta-base");

        request.Headers.Add("Authorization", "Bearer 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");

        request.Content = new StringContent($"{{\"inputs\": \"{chat}\"}}");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        HttpResponseMessage response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        responseBody = responseBody.Replace("[[", "[").Replace("]]", "]");
        var temps = JsonConvert.DeserializeObject<List<Temperatures>>(responseBody);

        var usr = Users.GetOrCreate(user, new User() { Name=user });
        usr.Messages++;
        foreach(var temp in temps)
        {
            usr.Temperatures.Increment(temp.Label, temp.Score);
        }

        if((usr.Messages%3)==0)
        {
            File.WriteAllText("infer.txt", JsonConvert.SerializeObject(Users));
        }
    }
}