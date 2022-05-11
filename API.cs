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
            try
            {
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
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
        async static public Task<HttpResponseMessage> send_message(HttpClient client, string channel_id, string content, string msgid)
        {
            /*  if (lastMsg == content)
              {
                  return null;
              }
              lastMsg = content;*/
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
                var s = await response.Content.ReadAsStringAsync();
                JToken message;
                try { message = JsonConvert.DeserializeObject<JArray>(s)[0]; }
                catch (Exception)
                {
                    if (s.Contains("Invalid Recipient(s)"))
                    {
                        return null;
                    }
                    try
                    {
                        message = JsonConvert.DeserializeObject<JObject>(s);
                    }
                    catch (Exception) { return null; }
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

                if (s.Contains("Invalid Recipient(s)") || s.Contains("rate limited") || s.Contains("Unknown Channel"))
                {
                    Console.WriteLine(s);
                    await Task.Delay(5000);

                    return null;
                }

                JToken m = JsonConvert.DeserializeObject<JObject>(s);
                dm_map.Add(channel_id, m["id"].ToString());
            }
            try
            {
                HttpResponseMessage response = await send_request(client, "GET", $"channels/{dm_map[channel_id]}/messages?limit=1");///" +lastMsg);
                var str = await response.Content.ReadAsStringAsync();
                JToken message = JsonConvert.DeserializeObject<JArray>(str)[0];
                if (user_id != "" && user_id != message["author"]["id"].ToString())
                {
                    await Task.Delay(1000);
                    return await getLatestdm(client, channel_id, user_id);
                }
                return JsonConvert.DeserializeObject<JArray>(str).First();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets the newest message sent in the channel, user_id is optional
        /// Recursive Function
        /// </summary>
        /// <param name="client"></param>
        /// <param name="channel_id"></param>
        /// <param name="user_id"></param>
        /// <returns> Returns a JToken with the message data </returns>
        async static public Task<JToken> getLatestMessage(HttpClient client, string channel_id)
        {
            HttpResponseMessage response = await send_request(client, "GET", $"channels/{channel_id}/messages?limit=1");
            if (response.ReasonPhrase == "Forbidden")
            {
                Console.WriteLine("Forbidden to access " + channel_id);
                Console.Beep();
                await Task.Delay(10000);
                return null;
            }
            
            var str = await response.Content.ReadAsStringAsync();
            if (str.Contains("rate limited"))
            {
                Console.WriteLine("Rate limited");
                Console.Beep();
                await Task.Delay(10000);
                return null;
            }

            try
            {
                JToken message = JsonConvert.DeserializeObject<JArray>(str)[0];
                return JsonConvert.DeserializeObject<JArray>(str).First();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                try
                {
                    JToken message = JsonConvert.DeserializeObject<JObject>(str);
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
        /// Gets the newest message sent in the channel, user_id is optional
        /// Recursive Function
        /// </summary>
        /// <param name="client"></param>
        /// <param name="channel_id"></param>
        /// <param name="user_id"></param>
        /// <returns> Returns a JToken with the message data </returns>
        async static public Task<JArray> getLatestMessages(HttpClient client, string channel_id, string user_id = "")
        {
            HttpResponseMessage response = await send_request(client, "GET", $"channels/{channel_id}/messages?limit=5");
            if (response.ReasonPhrase == "Forbidden")
            {
                Console.Beep();
                Console.WriteLine("Forbidden to access " + channel_id);
                await Task.Delay(10000);
                return null;
            }
            var str = await response.Content.ReadAsStringAsync();
            if (str.Contains("rate limited"))
            {
                Console.WriteLine("Rate limited");
                Console.Beep();
                await Task.Delay(10000);
                return null;
            }
            
            try
            {
                var messages = JsonConvert.DeserializeObject<JArray>(str);
                return messages;
            }
            catch (Exception e)
            {
                    Console.WriteLine(e.Message);
                Console.Beep();
                await Task.Delay(10000);
                return null;
            }
        }
    }
}
