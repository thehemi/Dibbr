using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Security.Policy;
using System.Text;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Filters;
using ServiceStack;
using DibbrBot;
using Twilio.Jwt.AccessToken;

namespace ChatGPT3;

public class ChatGPT
{
    public string cookie = "intercom-device-id-dgkjq2bp=9ab266e7-104f-46df-a8ec-2726d512e855; __Host-next-auth.csrf-token=16594dd4956288954395ba48c00687e3641cc9e09a5e1483efcadcb0d254168e%7C3647ca9d35790a9d43b74f29a045c2fbbe749691582d7319289c55bc2dc02efb; ajs_user_id=nfC9zQAtFYO2fZHxuR2LN67NFtT2; ajs_anonymous_id=2a42d869-61bc-4339-9fdc-b4c6dde89a40; __Secure-next-auth.callback-url=https%3A%2F%2Fchat.openai.com%2Fchat; mp_d7d7628de9d5e6160010b84db960a7ee_mixpanel=%7B%22distinct_id%22%3A%20%22user-Dl4ifhTmjdWafGAXNhyWPGpr%22%2C%22%24device_id%22%3A%20%2217d39fbf4095ed-0ad3e62e84e519-978183a-144000-17d39fbf40a65e%22%2C%22%24search_engine%22%3A%20%22google%22%2C%22%24initial_referrer%22%3A%20%22https%3A%2F%2Fwww.google.com%2F%22%2C%22%24initial_referring_domain%22%3A%20%22www.google.com%22%2C%22%24user_id%22%3A%20%22user-Dl4ifhTmjdWafGAXNhyWPGpr%22%7D; intercom-session-dgkjq2bp=UkZ2RjJkR051alppaWpJdHdETGdxZk5iQjFaRzFVd3NMRWVLV3AvT29zWHAzc2t3RHJmLzJRZGtGejlYODUwTy0tenhRYWtDRmlTOXpEWkNkOElNZmEyUT09--49ed3fda59cb3aa541ad13002d71283c78b8cd52; __Secure-next-auth.session-token=eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2R0NNIn0..TA2DCl1GOyS7GrxP.dt0fMS_xRY_WgfpBP7fL_In4ESGk8IO6VJ4n66AKtTSF_68CwJRnxrYrlrlxSgfnI6CZojhKhcGSqSzkRrGxCkdvrsk76BraHtDZBm82LwUHeQCslGubUtHeDyho2u7-irEQ2yfowktQ4p473CVUzvKmp9H9umJ71Ugu5Pg2ldNICJ3sJibVypsN9ngAgH82ayL3B1yg5jC4thzIx54B5u_w0fQ00wHFoz7aDaSL6Mqz-OtQa_lEkesExCPUwafi92fqefgJO2_yOocL0sYha0na7_WIsMl0P-LAgWpSHht2ZmvKc0vnivjtfXNLd7JXCk5F1XQ400sf6Bib1OZk_n-n6Br2gl445IalcGEBPwQa0jq83qMyxR4I5NasDvAz1reLGU_Xn6Pn6pp3AvO1D-B9cdVFewL5RHRf42we5Nw70CkThboTCwrZFn3n-ZnvmsDiv16AKPRtkwhbKYxdJBmmNltZYDU7fAq1YsPqEwNCQZQWrMKOGCCdjPHrVN8KK9HdzyynMt5RD0EfjyxII2Kc8vuPuUuWC2bEDSigvGNdB5K7ZYtHtqBJAhMN-OjFSvqbikm-8k8mTUWOB4VKoVZ5wwEf1DDwuKJWPw0gluTvfZcSZ3h7196q-2bTZrh3brdgTuay0Qym8vFjw1AKXrn0yd71mQbQoSsup5LEiL_tQn0MFahrGINT5UQcdH07VXceIXwyIcVaOS-Q3vKYIMFebO7tCQMcCIXPLA9wYVxE2pHnmS6nX8kc4PkAnol4-x6NUDB-xabAYDdc5WOTqaONBURLzfmounc0f3EKJdBdZxpJkb-xHPLA9K8UQthRNaMdqgynTaGiHhMkktAGp0pB0uyx0Zskz2ZhLuhFxgeYqdqpmjLOg3HesXa9BupGOEyyr_7BRpb0l_kAcalyrfqeUmnFxymZItQgOd-CnklmhVel0cTGPaBR_N7klRLiO0xO2Xfy7kB9Y9NMACXZcUpYqk0q2SlTlRV1rRFt8efqDm5md4hDfnV5LqebAZM-3Yu_raNehj9Vn4cXbRtIqL1qiTeu0vkf3yctRhPQSha_V26TwXWR7885iuEWMKX6pvQXYwU3u3lxfpv-y3me4hE7ZwpD_bqGC299jq_Bo0NkHfne7rmMn3Mb6yq77zzdbfwMP3Ppw0MPwGYnugg0Eu3Sdte-ibyThLq3iv2kcnMaY-vne5VW0H2Q8njgLzIEGqoL4dMAh733RoEcvbZ26c0IiFqP9AKE3610xUelV60DzYooCgT6rHXvhf3Yy3cMEQDXmltvgBrO5SUGrJE8h4ezEb1NLE5uobgKve6Mz3uGNT89ps0qyFSw5tl8NRA_1a_4dO5XbkEINhpDwAJ5RgkeWoWhJqOk5Oe_mBzMuGAJp5bYXuAlCiXIEcrS6vjrG_FKGa0L9W4smGX-XpGJvNrtOCQvi5yfBwUBoRCcDUH66tvS-0_sRNxKrwc8XyHA658FREuLKWIMDo4jduZjWW8Ap2qRse7DCp4hDyohLRdkljZodsD5_YWPgF_ZSZwwJebpMWAKxOOUaj6Bi6FVfawUZZIxgakNLHm71IJqwis6iPY5_a1DUnFseYCnbQKysy33zvd1so3JatNKY4z9jNppCf1feQSlMsXHtV7cTu6riqtytdNIl3FsX7H-_UxW-AaOLjGQuBmwPwV0pKuqz84v0rHKCahlhUn5Xwe_xkpf9R5OsTSXsvyr4JJN64jes8Pgh3B5VV8Tcao-qTYfG_AVVOwTa17p-nOUOdFCyPemlhRT9niSWsb7cYcw9YAQFhvXP3mgrUe4TXbecBe1o56aiU08jtSLcZ7SzJVZrsvhSo201XKMdbqPbj5CJI2zQmbEVdMO4-7Ed2Q4kfWoMDXvGDXfdiQ2jCHaLAO3OQolnPp9sYYa0yU1MyX403zfhVuOyuQP_9WAYzd8hoBI-2ewIEU0AnDZW3bcQISiRM_jlFrXjehmUhByaxhgryK80LiJ0vkMJwmwW90rrrkbvaDqDvJX6KTrlgFn73nfubl0zBsG8002lXXDMLLapO0yGIwtyiYrVINxM-P4usIhZFXYlJSZG5rlbCRHYOBX0nx1EVPm8zMaKGzp5GZDhxDtYeG7fQmDR0FJDCl0NC5xpnCnaXN35nhpzFg.bdisOcfk_gLnsScNUJc4EA";
    public Session Session { get; set; }
    ResponseMessage lastResponse = new ResponseMessage();
    public string url = "https://chat.openai.com/backend-api/conversation";
    public HttpClient client = new HttpClient();
    public HttpClient sessionClient = new HttpClient();
    public ChatGPT()
	{
        sessionClient.DefaultRequestHeaders.Add("cookie", cookie);
        
	}

    public async Task UpdateSession()
    {
        var tok = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6Ik1UaEVOVUpHTkVNMVFURTRNMEZCTWpkQ05UZzVNRFUxUlRVd1FVSkRNRU13UmtGRVFrRXpSZyJ9.eyJodHRwczovL2FwaS5vcGVuYWkuY29tL2F1dGgiOnsidXNlcl9pZCI6InVzZXItRGw0aWZoVG1qZFdhZkdBWE5oeVdQR3ByIn0sImlzcyI6Imh0dHBzOi8vYXV0aDAub3BlbmFpLmNvbS8iLCJzdWIiOiJhdXRoMHw2MmY4NGM2NzRlNmExOGZkNmEwNmE1YzkiLCJhdWQiOlsiaHR0cHM6Ly9hcGkub3BlbmFpLmNvbS92MSIsImh0dHBzOi8vb3BlbmFpLmF1dGgwLmNvbS91c2VyaW5mbyJdLCJpYXQiOjE2NzAxMTQwNDEsImV4cCI6MTY3MDEyMTI0MSwiYXpwIjoiVGRKSWNiZTE2V29USHROOTVueXl3aDVFNHlPbzZJdEciLCJzY29wZSI6Im9wZW5pZCBlbWFpbCBwcm9maWxlIG1vZGVsLnJlYWQgbW9kZWwucmVxdWVzdCBvcmdhbml6YXRpb24ucmVhZCBvZmZsaW5lX2FjY2VzcyJ9.IpLuxMpgEfkeaFzNhmbMyltqBehSRJJZthTnI-6zv_mAM8Mi9KBF5OSR8AdM6tTq6YMTKZFMEDGkDAb-4a-WpIxpZN8sBljlvnfo-on0IUoBM7Oz-D6xDP_ILdTOEHPl8Q5TIMrlxTE-3z5-pkw2gFKDnHmgp9jc5pk8HtFO1rSBpHwWsfze_TzPrwDZG0LQESoFunuVS5_pVpgEPDUAPKK19RUGDWP1UKVdFwpOvtqNdfroKuT2pp46qjQR_QzkjdFRVtZYuf5dAiJC5hHB1AIRnrxeux10QNeW4V08H8K_HhG64aHWVUe2RHhTaBHaDCYtafGlUOWMgcWh7ZG7hQ";
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + tok);
        return;
       var r = await sessionClient.GetAsync("https://chat.openai.com/api/auth/session");
        if(r!=null && r.IsSuccessStatusCode)
        {
            var session = await r.Content.ReadAsStringAsync();
            Session = JsonConvert.DeserializeObject<Session>(session);
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", "Bearer "+Session.accessToken);
            client.DefaultRequestHeaders.Remove("cookie");
            client.DefaultRequestHeaders.Add("cookie", r.GetHeader("set-cookie"));
        }
    }

    /// <summary>
    /// Sends a chat and gets one back
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<string> Next(string message)
    {
        if (Session == null)
            await UpdateSession();
        var chat = new ChatMessage("\""+message+"\" he asked.");
        chat.parent_message_id = lastResponse.id;
        var response = await client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(chat),Encoding.UTF8, "application/json"));
        var result = response.IsSuccessStatusCode?await response.Content.ReadAsStringAsync():response.ReasonPhrase;
        if(!result.Contains("data: [DONE]") || !response.IsSuccessStatusCode)
        {
            // Probably need new session code
            await UpdateSession();
            response = await client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(chat), Encoding.UTF8, "application/json"));
            result = response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : response.ReasonPhrase;
            if (!result.Contains("data: [DONE]") || !response.IsSuccessStatusCode)
            {
                Console.WriteLine("ERROR ");
                return "Failure";
            }
        }
        result = result.Substring(0, result.IndexOf("data: [DONE]"));
        result = result.Substring(result.LastIndexOf("data:") + 6).Trim();
  //      result = result.Remove("{\"message\": ");
        //result = result[0..^1];
         lastResponse = JsonConvert.DeserializeObject<ResponseMessage>(result);
        return lastResponse.message.content.parts.Join(" ");
    }


}




public class ResponseMessage
{
    public string id { get; set; } = "6e22a68c-1f31-4e7a-9bad-be22f05b9542";
    public string role { get; set; }
    public object user { get; set; }
    public object create_time { get; set; }
    public object update_time { get; set; }
    public Message message { get; set; }
    public Content content { get; set; }
    public object end_turn { get; set; }
    public double weight { get; set; }
    public Metadata metadata { get; set; }
    public string recipient { get; set; } = "all";
}

public class Metadata
{
}

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Content
{
    public string content_type { get; set; } = "text";
    public List<string> parts { get; set; } = new List<string>(); // the message
}

public class Message
{
    public string id { get; set; } = "3bed02ae-07af-4d33-8eca-316f3c2651b5";
    public string role { get; set; } = "user"; // or assistant or system or tool or unknown
    public Content content { get; set; } = new Content();
}

public class ChatMessage
{
    public string action { get; set; } = "next";
    public List<Message> messages { get; set; } = new List<Message>();
    public string conversation_id { get; set; } = "4c699ef3-8403-4c61-aaee-0f5cce56a0d5";
    public string parent_message_id { get; set; } = "6e22a68c-1f31-4e7a-9bad-be22f05b9542";
    public string model { get; set; } = "text-davinci-002-render";
    public object error { get; set; }
    public ChatMessage(string msg) { messages.Add(new Message() { content = new Content() { parts = new List<string>() { msg } } }); }
}



// Session stuff
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Session
{
    public User user { get; set; }
    public DateTime expires { get; set; }
    public string accessToken { get; set; }
}

public class User
{
    public string id { get; set; }
    public string name { get; set; }
    public string email { get; set; }
    public string image { get; set; }
    public string picture { get; set; }
    public List<string> groups { get; set; }
    public List<object> features { get; set; }
}

