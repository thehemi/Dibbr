using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DibbrBot;

class Api
{
    static readonly string ApiUrl = "https://discord.com/api/v9/";

    static readonly Dictionary<string, string> DmMap = new();

    /// <summary>
    ///     Sends a request to the url using a http client
    /// </summary>
    /// <param name="client"></param>
    /// <param name="method"></param>
    /// <param name="requestUrl"></param>
    /// <param name="body"></param>
    /// <returns> Returns a HttpResponseMessage </returns>
    public static async Task<(string str, string err)> send_request(HttpClient client, string method,
                                                                    string requestUrl, HttpContent body = null)
    {
        HttpResponseMessage response = null;
        try
        {
            switch (method.ToUpper())
            {
                case "GET":
                {
                    response = await client.GetAsync(ApiUrl + requestUrl);

                    break;
                }
                case "DELETE":
                {
                    response = await client.DeleteAsync(ApiUrl + requestUrl);

                    break;
                }
                case "POST":
                {
                    response = await client.PostAsync(ApiUrl + requestUrl, body);
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return (null, e.Message);
        }

        var s = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode) return (s, null);

        //  Console.Beep();
        Console.WriteLine(requestUrl + " " + response.ReasonPhrase);
        return (null, response.ReasonPhrase);
        return (s, null);
    }


    // static string lastMsg = "";
    /// <summary>
    ///     Sends a message to the specified channel
    /// </summary>
    /// <param name="client"></param>
    /// <param name="channelId"></param>
    /// <param name="content"></param>
    /// <returns> Returns a HttpResponseMessage </returns>
    public static async Task<string> send_message(HttpClient client, string channelId, string content, string msgid)
    {
        /*  if (lastMsg == content)
          {
              return null;
          }
          lastMsg = content;*/
        //  content = Regex.Escape(content);

        // Discord limit without nitro
        if (content.Length > 2000) content = content[^2000..];

        var c = content;
        content = content.Replace("\n", "\\n");
        content = content.Replace("\n", "\\n");
        content = content.Replace("\"", "\\\"");
        content = content.Replace("\r", "");
        content = content.Replace("\t", "");
        content = content.Replace("\f", "");
        content = content.Replace("\b", "");

        for (var i = 0; i < 5; i++)
        {
            var msgRef = "";
            if (msgid is {Length: > 0}) msgRef = $",\n\"message_reference\": {{ \"message_id\": \"{msgid}\" }}";

            var str = $"{{\n\"content\": \"{content}\"{msgRef} \n}}";

            string reply, err;
            (reply, err) = await send_request(client, "POST", $"channels/{channelId}/messages",
                new StringContent(str, Encoding.UTF8, "application/json"));

            if (reply != null) return reply;

            // WTF!! Some mesages come back with bad request. I don't understand why or how to fix
            if (i == 2) content = Regex.Escape(c);

            if (i == 3) content = content[..(content.Length / 2)] + "...(snipped due to send failure)";

            if (i == 4)
            {
                content =
                    "The message I was trying to send failed for some reason."; // Maybe it was too big or had funny characters in? First 100 chars: " + c[..20].Replace("\n", "");
            }

            await Task.Delay(4000);
        }

        return null;
    }

    public static async Task<string> send_dm(HttpClient client, string dmId, string content, string msgid)
    {
        if (!DmMap.ContainsKey(dmId))
        {
            var (s, _) = await send_request(client, "POST", "/users/@me/channels",
                new StringContent($"{{\"recipient_id\":\"{dmId}\"}}", Encoding.UTF8, "application/json"));

            JToken message;
            try { message = JsonConvert.DeserializeObject<JArray>(s)[0]; }
            catch (Exception)
            {
                try { message = JsonConvert.DeserializeObject<JObject>(s); }
                catch (Exception) { return null; }
            }

            DmMap.Add(dmId, message["id"].ToString());
        }

        // Discord limit without nitro
        if (content.Length > 2000) content = content[^2000..];


        content = content.Replace("\n", "\\n");
        content = content.Replace("\n", "\\n");
        content = content.Replace("\"", "\\\"");
        content = content.Replace("\r", "\\n");
        content = content.Replace("\t", "");
        content = content.Replace("\f", "");
        content = content.Replace("\b", "");
        var (s2, _) = await send_request(client, "POST", $"channels/{DmMap[dmId]}/messages",
            new StringContent($"{{\"content\":\"{content}\"}}", Encoding.UTF8, "application/json"));
        return s2;
    }


    /// <summary>
    ///     Gets the newest message sent in the channel, user_id is optional
    ///     Recursive Function
    /// </summary>
    /// <param name="client"></param>
    /// <param name="channelId"></param>
    /// <param name="userId"></param>
    /// <returns> Returns a JToken with the message data </returns>
    public static async Task<JToken> GetLatestdm(HttpClient client, string channelId, string userId = "")
    {
        if (!DmMap.ContainsKey(channelId))
        {
            var (s, _) = await send_request(client, "POST", "/users/@me/channels",
                new StringContent($"{{\"recipient_id\":\"{channelId}\"}}", Encoding.UTF8, "application/json"));

            JToken m = JsonConvert.DeserializeObject<JObject>(s);
            DmMap.Add(channelId, m["id"].ToString());
        }

        try
        {
            var (str, _) =
                await send_request(client, "GET", $"channels/{DmMap[channelId]}/messages?limit=1"); ///" +lastMsg);
            _ = JsonConvert.DeserializeObject<JArray>(str)[0];

            return JsonConvert.DeserializeObject<JArray>(str).First();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    /// <summary>
    ///     Gets the newest message sent in the channel, user_id is optional
    ///     Recursive Function
    /// </summary>
    /// <param name="client"></param>
    /// <param name="channelId"></param>
    /// <param name="user_id"></param>
    /// <returns> Returns a JToken with the message data </returns>
    public static async Task<JToken> GetLatestMessage(HttpClient client, string channelId)
    {
        var (str, _) = await send_request(client, "GET", $"channels/{channelId}/messages?limit=1");

        try
        {
            _ = JsonConvert.DeserializeObject<JArray>(str)[0];
            return JsonConvert.DeserializeObject<JArray>(str).First();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            try
            {
                JsonConvert.DeserializeObject<JObject>(str);
                return JsonConvert.DeserializeObject<JArray>(str);
            }
            catch (Exception e2)
            {
                Console.WriteLine(e2.Message);
                return null;
            }
        }
    }


    /// <summary>
    ///     Gets the newest message sent in the channel, user_id is optional
    ///     Recursive Function
    /// </summary>
    /// <param name="client"></param>
    /// <param name="channelId"></param>
    /// <param name="userId"></param>
    /// <returns> Returns a JToken with the message data </returns>
    public static async Task<List<Message>> GetLatestDMs(HttpClient client, string channelId, string userId = "")
    {
        if (!DmMap.ContainsKey(channelId))
        {
            var (s, _) = await send_request(client, "POST", "/users/@me/channels",
                new StringContent($"{{\"recipient_id\":\"{channelId}\"}}", Encoding.UTF8, "application/json"));

            JToken m = JsonConvert.DeserializeObject<JObject>(s);
            DmMap.Add(channelId, m["id"].ToString());
        }

        var (str, _) = await send_request(client, "GET", $"channels/{DmMap[channelId]}/messages?limit=5");

        try
        {
            var messages = JsonConvert.DeserializeObject<List<Dm>>(str);
            var dms = new List<Message>();
            foreach (var m in messages)
            {
                var dm = new Message {Author = m.Author, Content = m.Content, Id = m.Id, Timestamp = m.Timestamp};
                dms.Add(dm);
            }

            //if (messages != null)
            return dms;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            //  Console.Beep();
            await Task.Delay(3000);
            return null;
        }
    }


    /// <returns> Returns a JToken with the message data </returns>
    public static async void Typing(HttpClient client, string channelId, bool start)
    {
        if (start) { var (_, _) = await send_request(client, "POST", $"channels/{channelId}/typing"); }
    }


    /// <param name="channelId"></param>
    /// <param name="userId"></param>
    /// <returns> Returns a JToken with the message data </returns>
    public static async Task<List<Message>> GetLatestMessages(HttpClient client, string channelId, string userId = "",
                                                              int numMessages = 5)
    {
        while (true)
        {
            var (str, _) = await send_request(client, "GET", $"channels/{channelId}/messages?limit={numMessages}");
            if (str == null)
            {
                Console.WriteLine("Probably channel " + channelId + " does not exist");
                await Task.Delay(2000);
            }

            try
            {
                var messages = JsonConvert.DeserializeObject<List<Message>>(str);
                if (messages != null) return messages;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                // Console.Beep();
                await Task.Delay(2000);
                // return null;
            }
        }
    }


    /// <param name="channel_id"></param>
    /// <param name="userId"></param>
    /// <returns> Returns a JToken with the message data </returns>
    public static async Task<JArray> GetNewDMs(HttpClient client, string userId = "")
    {
        while (true)
        {
            var (str, _) = await send_request(client, "GET", $"users/{userId}/channels");
            if (str == null)
            {
                // Console.WriteLine("Probably channel " + channel_id + " does not exist");
                await Task.Delay(2000);
            }

            try
            {
                var messages = JsonConvert.DeserializeObject<JArray>(str);
                return messages;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                // Console.Beep();
                await Task.Delay(2000);
                // return null;
            }
        }
    }
}

public class Dm
{
    public string Id { get; set; }
    public int Type { get; set; }
    public string Content { get; set; }
    public string ChannelId { get; set; }
    public Author Author { get; set; }
    public List<object> Attachments { get; set; }
    public List<object> Embeds { get; set; }
    public List<object> Mentions { get; set; }
    public List<object> MentionRoles { get; set; }
    public bool Pinned { get; set; }
    public bool MentionEveryone { get; set; }
    public bool Tts { get; set; }
    public DateTime Timestamp { get; set; }
    public object EditedTimestamp { get; set; }
    public int Flags { get; set; }
    public List<object> Components { get; set; }
}

public class Author
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Avatar { get; set; }
    public object AvatarDecoration { get; set; }
    public string Discriminator { get; set; }
    public int PublicFlags { get; set; }
}

public class Mention
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Avatar { get; set; }
    public object AvatarDecoration { get; set; }
    public string Discriminator { get; set; }
    public int PublicFlags { get; set; }
}

public class MessageReference
{
    public string ChannelId { get; set; }
    public string GuildId { get; set; }
    public string MessageId { get; set; }
}

public class ReferencedMessage
{
    public string Id { get; set; }
    public int Type { get; set; }
    public string Content { get; set; }
    public string ChannelId { get; set; }
    public Author Author { get; set; }
    public List<object> Attachments { get; set; }
    public List<object> Embeds { get; set; }
    public List<object> Mentions { get; set; }
    public List<object> MentionRoles { get; set; }
    public bool Pinned { get; set; }
    public bool MentionEveryone { get; set; }
    public bool Tts { get; set; }
    public DateTime Timestamp { get; set; }
    public object EditedTimestamp { get; set; }
    public int Flags { get; set; }
    public List<object> Components { get; set; }
}

public class Message
{
    public string Id { get; set; }
    public int Type { get; set; }
    public string Content { get; set; }
    public string ChannelId { get; set; }
    public Author Author { get; set; }
    public List<object> Attachments { get; set; }
    public List<object> Embeds { get; set; }
    public List<Mention> Mentions { get; set; }
    public List<object> MentionRoles { get; set; }
    public bool Pinned { get; set; }
    public bool MentionEveryone { get; set; }
    public bool Tts { get; set; }
    public DateTime Timestamp { get; set; }
    public object EditedTimestamp { get; set; }
    public int Flags { get; set; }
    public List<object> Components { get; set; }
    public MessageReference MessageReference { get; set; }
    public ReferencedMessage ReferencedMessage { get; set; }
}