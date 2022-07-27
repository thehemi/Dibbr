
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

/// <summary>
/// Wrapper around https://labs.openai.com/ DALL-E 2 txt2image image generation network
/// </summary>
public class Dalle
{
    string Key;
    int RequestsRemaining;
    DateTime Start;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="token">Token or key to access API, starts with sk or sess</param>
    public Dalle(string token)
    {
        Key = token;
        Start = DateTime.Now;
    }


    /// <summary>
    /// Generiuc wrapper around POST
    /// </summary>
    /// <param name="url"></param>
    /// <param name="postData"></param>
    /// <param name="auth"></param>
    /// <returns></returns>
    public async Task<string> PostData(string url, string postData)
    {
        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri(url);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("Authorization", "Bearer "+Key);
        HttpResponseMessage response =
            await client.PostAsync(url, new StringContent(postData, Encoding.UTF8, "application/json"));
        if (response.IsSuccessStatusCode) { return await response.Content.ReadAsStringAsync(); }
        else
        {
            Console.WriteLine(url + " Fail: " + response.ReasonPhrase);
            return await response.Content.ReadAsStringAsync();
        }

        return "Fail: " + response.ReasonPhrase;
    }

    protected async Task<string> Get(string url)
    {
        HttpClient client = new HttpClient();
        client.BaseAddress = new Uri(url);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Key);
        HttpResponseMessage response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode) { return await response.Content.ReadAsStringAsync(); }
        else
        {
            Console.WriteLine(url + " Fail: " + response.ReasonPhrase);
            return await response.Content.ReadAsStringAsync();
        }

        return "Fail: " + response.ReasonPhrase;
    }

    /// <summary>
    /// Query DALLE, returns a string with images, or an error string
    /// </summary>
    /// <param name="q"></param>
    /// <returns></returns>
    public async Task<string> Query(string q)
    {
        if (DateTime.Now < Start) return "Credits available in " + (DateTime.Now - Start).ToString("g");
        string url = "https://labs.openai.com/api/labs/tasks";
        Console.Out.WriteLine("DALLE:"+q);
        int batch_size = 4; // How many images we get back

        string postData = $@"{{
  ""task_type"": ""text2im"",
  ""prompt"": {{
    ""caption"": ""{q}"",
    ""batch_size"": {batch_size}
  }}
}}";

        var str = await PostData(url, postData);
        if (str == null || str.StartsWith("Fail") || str.Contains("error"))
            return str;
        var d = JsonConvert.DeserializeObject<DalleResponse>(str);

        // Now we have a task to check
        var taskUrl = url +"/"+ d.id;
        while (true)
        {
            str = await Get(taskUrl);
            try { d = JsonConvert.DeserializeObject<DalleResponse>(str); }
            catch (Exception e)
            {
                // DALLE server is down
                return "DALLE server is down";
            }

            if (d.status == "succeeded") break;
            if (d.status == "rejected")
            {
                return "Your request violated the terms of service";
            }
            await Task.Delay(2000);
        }

        var log = "";
        var images = "";
     
        var client = new WebClient();
        foreach (var data in d.generations.data)
        {
            data.url = await Share(data.id);
            // var img = data.generation.image_path;
            images += data.url + "\r\n";
            log += data.url + "\r\n";
            
        }

        // Download files in another thread
        new Thread(async () =>
        {
            int i = 0;
            await Task.Delay(1000); // Give it a sec
            foreach (var data in d.generations.data)
            {
                var clip = q.Length > 14 ? 14 : q.Length;
                var client = new WebClient();
                try
                {
                    await client.DownloadFileTaskAsync(data.url, q.Substring(0, clip).Replace(" ", "_") + i++ + ".webp");
                }catch(Exception e) { }
            }
        }).Start();
    
        return images;//r.response;
    }

    /// <summary>
    /// Publicly share the images, so we can get urls to them that don't require auth
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<string> Share(string id)
    {
        var url = $"https://labs.openai.com/api/labs/generations/{id}/share";        
        var share = await PostData(url, "");
        if (share.Contains("DOCTYPE")) return "";
        var datum = JsonConvert.DeserializeObject<Datum>(share);
        // 
        var share_url = "https://labs.openai.com/s/"+datum.id.Replace("generation-","");
      
        var pic_url =
            $"https://openai-labs-public-images-prod.azureedge.net/user-YEKzBkXR8DYzHUkozy2oj5Ry/generations/{datum.id}/image.webp";
        try
        {
            var client = new WebClient();
            await client.DownloadFileTaskAsync(pic_url, id.Replace(" ", "_") + ".webp");
        }
        catch (Exception e)
        {
            return share_url;
        }
        return pic_url;

    }
    public class Error
    {
        public string message { get; set; }
        public string type { get; set; }
        public string param { get; set; }
        public string code { get; set; }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Datum
    {
        public string id { get; set; }
        public string @object { get; set; } // Generation
        public int created { get; set; }
        public string generation_type { get; set; } // ImageGeneration
        public Generation generation { get; set; }
        public string task_id { get; set; }
        public string prompt_id { get; set; }
        public bool is_public { get; set; }
        public string url { get; set; }
    }

    public class Generation
    {
        public string image_path { get; set; }
    }

    public class Generations
    {
        public string @object { get; set; }
        public List<Datum> data { get; set; }
    }

    public class Prompt
    {
        public string id { get; set; } // prompt-5553
        public string @object { get; set; } // generation, prompt
        public int created { get; set; }
        public string prompt_type { get; set; } // CaptionPrompt
        public Prompt prompt { get; set; }
        public object parent_generation_id { get; set; }
        public string caption { get; set; }
    }

    public class DalleResponse
    {
        public string @object { get; set; }
        public string id { get; set; } // /api/labs/tasks/id is next thing to check
        public int created { get; set; }
        public string task_type { get; set; }
        public string status { get; set; } // pending until succeeded

        public StatusInformation status_information { get; set; }
        public string prompt_id { get; set; }
        public Generations generations { get; set; }
        public Prompt prompt { get; set; }
    }

    public class StatusInformation
    {
    }

}
