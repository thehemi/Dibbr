using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DibbrBot
{
    class Program
    {
        private static HttpClient client = new HttpClient();
        static void Main(string[] args)
        {
            // init stuff

            // How to find your token: https://youtu.be/YEgFvgg7ZPI
            // How to find server and channel id: https://youtu.be/NLWtSHWKbAI");
            string token = "MzM2NzcyMTY5ODkzOTM3MTU0.Yifu9A.ImnIoKIhcVVr_K1l1Jel9R5y0po";
  
            
            // set the headers
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US");
            client.DefaultRequestHeaders.Add("Authorization", token);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) discord/0.0.309 Chrome/83.0.4103.122 Electron/9.3.5 Safari/537.36");

            // SSL Certificate Bypass
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            // start the farm
            var s = new Thread(async () =>
            {
                // wait 1 seconds for the header to set
                Console.WriteLine("Starting...");
                await Task.Delay(1000);

                // mrgirl chat room bot
               var f1 = new Bot("937151566266384394", false,client);
                // DM bot
                var f2 = new Bot("556523373522583555",true,client);
                while (true)
                {
                    await f1.StartListening();
                   
                    await f2.StartListening();
                    await Task.Delay(500);
                }
            });
            s.Start();


            while (true) { }
        }
    }
}
