using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
        async static public Task<(string str,string err)> send_request(HttpClient client, string method, string request_url, HttpContent body = null)
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
                return (null,e.Message);
            }
            var s = await response.Content.ReadAsStringAsync(); 
            if (response.IsSuccessStatusCode)
            {
                return (s, null);
            }       
            else
            {
                    Console.Beep();
                Console.WriteLine(request_url +" "+response.ReasonPhrase);
                    return (null,response.ReasonPhrase);
               
            }
            return (s,null);
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

            // Discord limit without nitro
            if (content.Length > 2000)
                content = content[^2000..];
            
            var c = content;
            content = content.Replace("\n", "\\n");
            content = content.Replace("\n", "\\n");
            content = content.Replace("\"", "\\\"");
            content = content.Replace("\r", "");
            content = content.Replace("\t", "");
            content = content.Replace("\f", "");
            content = content.Replace("\b", "");

            for (int i = 0; i < 5; i++)
            {
                var str = $"{{\n\"content\": \"{content}\",\n\"message_reference\": {{ \"message_id\": \"{msgid}\" }} \n}}";

                string reply, err;
                (reply,err)= await send_request(
                client,
                "POST",
                $"channels/{channel_id}/messages",
                new StringContent(str, Encoding.UTF8, "application/json"));

                if (reply != null)
                {
                    return reply;
                }
                else
                {
                    // WTF!! Some mesages come back with bad request. I don't understand why or how to fix
                    if (i == 2)
                        content = Regex.Escape(c);
                    if (i == 3)
                        content = content[..(content.Length / 2)] + "...(snipped due to send failure)";
                    if (i == 4)
                        content = "The message I was trying to send failed for some reason. Maybe it was too big or had funny characters in? First 100 chars: "+c[..20].Replace("\n","");
                    await Task.Delay(3000);
                }
            }
            return null;

        }
        
        static Dictionary<string, string> dm_map = new Dictionary<string, string>();
        async static public Task<string> send_dm(HttpClient client, string dm_id, string content)
        {
            if (!dm_map.ContainsKey(dm_id))
            {
                var (s,e) = await send_request(
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

           var (s2,e2) = await send_request(
            client,
            "POST",
            $"channels/{dm_map[dm_id]}/messages",
            new StringContent($"{{\"content\":\"{content}\"}}", Encoding.UTF8, "application/json"));
            return s2;
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
                var (s,_) = await send_request(
                client,
                "POST",
                $"/users/@me/channels",
                new StringContent($"{{\"recipient_id\":\"{channel_id}\"}}", Encoding.UTF8, "application/json"));
                
                JToken m = JsonConvert.DeserializeObject<JObject>(s);
                dm_map.Add(channel_id, m["id"].ToString());
            }
            try
            {
                var (str,_)= await send_request(client, "GET", $"channels/{dm_map[channel_id]}/messages?limit=1");///" +lastMsg);
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
            var (str, _) = await send_request(client, "GET", $"channels/{channel_id}/messages?limit=1");
           
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
            var (str, _) = await send_request(client, "GET", $"channels/{channel_id}/messages?limit=5");
  
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
