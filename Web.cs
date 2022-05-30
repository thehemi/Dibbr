using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;


namespace DibbrBot;

class Web
{
    public static List<ChatSystem> Clients = new();
    static WebApplication _app;

    public static void Run()
    {
        var builder = WebApplication.CreateBuilder();
        var txt = File.ReadAllText("index.html");
        _app = builder.Build();
        _app.UseStaticFiles();
        //app.MapGet("/", () => "Hello World!");

        string MakePage(string openai = "", string discord = "", string channel = "")
        {
            var page = txt;
            page = page.Replace("{Discord}", discord);
            page = page.Replace("{OpenAI}", openai);
            page = page.Replace("{Channel}", channel);

            if (discord.Length > 0 && openai.Length > 0)
            {
                var gpt3 = new Gpt3(openai);
                if (channel == "")
                {
                    foreach (var c in Clients)
                    {
                        if ((c as DiscordChatV2).Id != discord) continue;

                        page = page.Replace("{Status}", $"{Program.BotName} is already running with your key");
                        return page;
                    }


                    Program.NewClient(new DiscordChatV2(), discord, gpt3);
                }
                else
                    Program.NewClient(new DiscordChat(false, ConfigurationManager.AppSettings["BotName"], null),
                        discord, gpt3);

                page = page.Replace("{Status}",
                    $"{Program.BotName} initialized with provided keys! Try summoning him in your server chat with {Program.BotName}, hi!");
            }
            else
                page = page.Replace("{status}", "Dibbr not initialized, please provide keys for dibbr");

            return page;
        }
        // app.MapGet("/index.html", () => Results.Text(txt, "text/html"));

        _ = _app.MapGet("/", () => Results.Text(MakePage(), "text/html"));
        _ = _app.MapPost("/", (HttpContext ctx) =>
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

        new Thread(() => { _app.Run(); }).Start();
    }
}