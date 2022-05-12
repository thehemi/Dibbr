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
        async static public Task<string> send_request(HttpClient client, string method, string request_url, HttpContent body = null)
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
            var s = await response.Content.ReadAsStringAsync(); 
            if (response.IsSuccessStatusCode)
            {
                return s;
            }       
            else
            {

                    Console.Beep();
                Console.WriteLine(request_url +" "+response.ReasonPhrase);
                    await Task.Delay(10000);
                    return null;
               
            }
            return s;
        }


       // static string lastMsg = "";
        /// <summary>
        /// Sends a message to the specified channel
        /// </summary>
        /// <param name="client"></param>
        /// <param name="channel_id"></param>
        /// <param name="content"></param>
        /// <returns> Returns a HttpResponseMessage </returns>
        async static public Task<string> send_message(HttpClient client, string channel_id, string content, string msgid)
        {
            /*  if (lastMsg == content)
              {
                  return null;
              }
              lastMsg = content;*/
            //  content = Regex.Escape(content);
            content = content.Replace("\n", "\\n");
            var str = $"{{\"content\":\"{content}\",\"message_reference\": {{ \"message_id\": \"{msgid}\" }} }}";

            return await send_request(
            client,
            "POST",
            $"channels/{channel_id}/messages",
            new StringContent(str, Encoding.UTF8, "application/json"));

        }
        
        static Dictionary<string, string> dm_map = new Dictionary<string, string>();
        async static public Task<string> send_dm(HttpClient client, string dm_id, string content)
        {
            if (!dm_map.ContainsKey(dm_id))
            {
                var s = await send_request(
                client,
                "POST",
                $"/users/@me/channels",
                new StringContent($"{{\"recipient_id\":\"{dm_id}\"}}", Encoding.UTF8, "application/json"));

                JToken message;
                try { message = JsonConvert.DeserializeObject<JArray>(s)[0]; }
                catch (Exception)
                {
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
                var s = await send_request(
                client,
                "POST",
                $"/users/@me/channels",
                new StringContent($"{{\"recipient_id\":\"{channel_id}\"}}", Encoding.UTF8, "application/json"));
                JToken m = JsonConvert.DeserializeObject<JObject>(s);
                dm_map.Add(channel_id, m["id"].ToString());
            }
            try
            {
                var str = await send_request(client, "GET", $"channels/{dm_map[channel_id]}/messages?limit=1");///" +lastMsg);
                JToken message = JsonConvert.DeserializeObject<JArray>(str)[0];
               
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
            var str = await send_request(client, "GET", $"channels/{channel_id}/messages?limit=1");
           
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
            var str = await send_request(client, "GET", $"channels/{channel_id}/messages?limit=5");
  
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
