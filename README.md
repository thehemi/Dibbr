# GPT3/ChatGPT Bot For Slack, Google Workspace, Discord

Add dibbr to your Discord server by following this link: https://discord.com/api/oauth2/authorize?client_id=972013718168813599&permissions=1024&scope=bot

Deploy dibbr to your server by building .NET Linux (Visual Studio->Publish) and run nohup /path/to/dibbr

While many of you enjoy Python, this bot has some awesome features worthy of using C#. Runs on windows or mac or linux. Build yourself. 

Try dibbr out in discord here: [https://discord.gg/mCHc8d5gEQ](https://discord.gg/mCHc8d5gEQ)

Dibbr is a bot so you can add him to your discord. Then activate him with your OpenAI key (free).

If you've used ChatGPT, dibbr is like the unrestricted version.

Chat log here 
[/chat_log.txt](https://github.com/thehemi/Dibbr/blob/master/chat_log_937151566266384394.txt)

Beginning of log is Dibbr beta, end of log is current dibbr, he has grown a lot, and has grown since then too
Update: Dibbr is actually even better than this, but I can't use that chat server any more.

## SUPPORTS
 * Discord selfbot (can put bot in anyone's server, but violates TOS)
 * Slack Bot
 * Discord regular bot

## Features
 * Dibbr can memorize your company or project wiki and answer any questions
 * Dibbr now supports GPT-3.5
 * Dibbr can write stories, with "tell me a story about", or "tell me a three act story about" (for longer stories)
 * Dibbr can have his bot name changed to anything you want
 * Dibbr will interject and join in conversation, pick up questions asked, or questions just directed towards him
 * Everything is configurable via running bot commands
 * Dibbr understands replies, dibbr can work in self-bot or regular bot mode, so on any sever in any channel
 * Dibbr will even ask topical questions
 * Dibbr has been tuned to give very natural replies. It may not seem like much priming text, but there have been a few tricks to get him working smoothly
 * Dibbr will try to avoid repeating himself, by internal checks for sentence repetition
 * Dibbr will use a variable amount of chat log (custom-4000 chars) to answer questions, using his memory of the conversation
 * Dibbr can run in any number of channels and servers, all with their own instance
 * Dibbr even has a web interface
 * Much more coming soon
 
![Chat](https://i.imgur.com/E2qjTw3.png)

# Setup

Build locally, any build here is usually old

Please use your own OpenAI key and discord keys, the app will run you through the process and save to app.config

## Slack install Guide
1. Createw a classic Slack app here https://api.slack.com/apps?new_classic_app=1
2. Go to OAth on the left bar and generate tokens
3. Copy the bot token. You'll give it to the app on setup 
4. Deploy your slack app to the workspace
5. Go to your slack room, and 'Add app', search for your app name e.g. dibbr
6. Dibbr should be online and ready to go! Make sure the app is running

