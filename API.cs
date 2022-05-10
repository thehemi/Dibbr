using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DibbrBot
{
    class API
    {

        private static readonly string API_URL = "https://discord.com/api/v9/";

        /// <summary>
        /// Sends a request to the url using a http client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="method"></param>
        /// <param name="request_url"></param>
        /// <param name="body"></param>
        /// <returns> Returns a HttpResponseMessage </returns>
        async static public Task<HttpResponseMessage> send_request(HttpClient client, string method, string request_url, HttpContent body = null)
        {
            HttpResponseMessage response = null;
            switch (method.ToUpper())
            {
                case "GET":
                    {
                        response = await client.GetAsync(API_URL + request_url);
                        break;
                    }
                case "POST":
                    {
                        response = await client.PostAsync(API_URL + request_url, body);
                        break;
                    }
            }
            return response;
        }
    

    static string lastMsg = "";
        /// <summary>
        /// Sends a message to the specified channel
        /// </summary>
        /// <param name="client"></param>
        /// <param name="channel_id"></param>
        /// <param name="content"></param>
        /// <returns> Returns a HttpResponseMessage </returns>
        async static public Task<HttpResponseMessage> send_message(HttpClient client, string channel_id, string content,string msgid)
        {
            if (lastMsg == content)
            {
                return null;
            }
            lastMsg = content;
            var str = $"{{\"content\":\"{content}\",\"message_reference\": {{ \"message_id\": \"{msgid}\" }} }}";
            
            var r = await send_request(
            client,
            "POST",
            $"channels/{channel_id}/messages",
            new StringContent(str, Encoding.UTF8, "application/json"));

            return r;
      
        }

        static Dictionary<string, string> dm_map = new Dictionary<string, string>();
        async static public Task<HttpResponseMessage> send_dm(HttpClient client, string dm_id, string content)
        {
            if (!dm_map.ContainsKey(dm_id))
            {
                var response = await send_request(
                client,
                "POST",
                $"/users/@me/channels",
                new StringContent($"{{\"recipient_id\":\"{dm_id}\"}}", Encoding.UTF8, "application/json"));
                JToken message;
                   try { message = JsonConvert.DeserializeObject<JArray>(await response.Content.ReadAsStringAsync())[0]; }
                catch(Exception e)
                {
                    message = JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync());
                }
                dm_map.Add(dm_id, message["id"].ToString());
            }
            
            return await send_request(
            client,
            "POST",
            $"channels/{dm_map[dm_id]}/messages",
            new StringContent($"{{\"content\":\"{content}\"}}", Encoding.UTF8, "application/json")
        );
        }


        /// <summary>
        /// Gets the newest message sent in the channel, user_id is optional
        /// Recursive Function
        /// </summary>
        /// <param name="client"></param>
        /// <param name="channel_id"></param>
        /// <param name="user_id"></param>
        /// <returns> Returns a JToken with the message data </returns>
        async static public Task<JToken> getLatestdm(HttpClient client, string channel_id, string user_id = "")
        {
            if (!dm_map.ContainsKey(channel_id))
            {
                var r2 = await send_request(
                client,
                "POST",
                $"/users/@me/channels",
                new StringContent($"{{\"recipient_id\":\"{channel_id}\"}}", Encoding.UTF8, "application/json"));
                var s = await r2.Content.ReadAsStringAsync();

                if(s.Contains("Invalid Recipient(s)"))
                {
                    return null;
                }
                
                JToken m = JsonConvert.DeserializeObject<JObject>(s);
                dm_map.Add(channel_id, m["id"].ToString());
            }

            HttpResponseMessage response = await send_request(client, "GET", $"channels/{dm_map[channel_id]}/messages?limit=1");///" +lastMsg);
            JToken message = JsonConvert.DeserializeObject<JArray>(await response.Content.ReadAsStringAsync())[0];
            if (user_id != "" && user_id != message["author"]["id"].ToString())
            {
                await Task.Delay(1000);
                return await getLatestdm(client, channel_id, user_id);
            }
            return JsonConvert.DeserializeObject<JArray>(await response.Content.ReadAsStringAsync()).First();
        }

        /// <summary>
        /// Gets the newest message sent in the channel, user_id is optional
        /// Recursive Function
        /// </summary>
        /// <param name="client"></param>
        /// <param name="channel_id"></param>
        /// <param name="user_id"></param>
        /// <returns> Returns a JToken with the message data </returns>
        async static public Task<JToken> getLatestMessage(HttpClient client, string channel_id, string user_id = "")
        {
            HttpResponseMessage response = await send_request(client, "GET", $"channels/{channel_id}/messages?limit=1");
            JToken message = JsonConvert.DeserializeObject<JArray>(await response.Content.ReadAsStringAsync())[0];
            if (user_id != "" && user_id != message["author"]["id"].ToString())
            {
                await Task.Delay(1000);
                return await getLatestMessage(client, channel_id, user_id);
            }
            return JsonConvert.DeserializeObject<JArray>(await response.Content.ReadAsStringAsync()).First();
        }
    }
}
