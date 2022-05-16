using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Funq;
using ServiceStack;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using ServiceStack.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Diagnostics;

namespace DibbrBot;

public class BotRequest
{
    // [Required]
    public string Discord { get; set; } = "";
    public string OpenAI { get; set; } = "";
}

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

        string MakePage(string openai = "", string discord = "")
        {
            var page = txt;
            page = page.Replace("{Discord}", discord);
            page = page.Replace("{OpenAI}", openai);
             
            if (discord.Length > 0 && openai.Length > 0)
            {
                foreach (var c in clients)
                    if ((c as DiscordChatV2).id == discord)
                    {
                        page = page.Replace("{Status}", "Dibbr is already running with your key");
                        return page;
                    }

                page = page.Replace("{Status}", "Dibbr initialized with provided keys! Try summoning him in your server chat with dibbr, hi!");
                var gpt3 = new GPT3(openai);
                var client = new DiscordChatV2() { };
                _ = client.Initialize(async (msg, user) => { return await Program.OnMessage(client, msg, user, gpt3); }, discord);
                clients.Add(client);
            }
            else
            {
                page = page.Replace("{status}", "Dibbr not initialized, please provide keys for dibbr");
            }
            return page;

        }
        // app.MapGet("/index.html", () => Results.Text(txt, "text/html"));

        _ = app.MapGet("/", () => Results.Text(MakePage(), "text/html"));
        _ = app.MapPost("/", (HttpContext ctx) => {
            var openai = ctx.Request.Form["OpenAI"];
            var discord = ctx.Request.Form["Discord"];
            return Results.Text(MakePage(openai, discord), "text/html"); });

            // Configure the HTTP request pipeline.
            //  if (!app.Environment.IsDevelopment())
            //   {
            //  app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            //  app.UseHsts();
            //   app.UseHttpsRedirection();
            //  }

        new Thread(()=> { app.Run(); }).Start();
    }
}
