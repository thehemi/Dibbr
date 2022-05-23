﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading;

namespace DibbrBot;

class Web
{
    public static List<IChatSystem> clients = new List<IChatSystem>();
    static WebApplication app;
    public static void Run()
    {
        var builder = WebApplication.CreateBuilder();
        var txt = System.IO.File.ReadAllText("index.html");
        app = builder.Build();
        app.UseStaticFiles();
        //app.MapGet("/", () => "Hello World!");

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
                    foreach (var c in clients)
                        if ((c as DiscordChatV2).id == discord)
                        {
                            page = page.Replace("{Status}", $"{Program.BotName} is already running with your key");
                            return page;
                        }


                    Program.NewClient(new DiscordChatV2(), discord, gpt3);
                }
                else
                {

                    Program.NewClient(new DiscordChat(false, false, channel/*Channel id, room or dm*/), discord, gpt3);
                }

                page = page.Replace("{Status}", $"{Program.BotName} initialized with provided keys! Try summoning him in your server chat with {Program.BotName}, hi!");

            }
            else
            {
                page = page.Replace("{status}", "Dibbr not initialized, please provide keys for dibbr");
            }
            return page;

        }
        // app.MapGet("/index.html", () => Results.Text(txt, "text/html"));

        _ = app.MapGet("/", () => Results.Text(MakePage(), "text/html"));
        _ = app.MapPost("/", (HttpContext ctx) =>
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

        new Thread(() => { app.Run(); }).Start();
    }
}
