using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using TextCopy;

namespace DiscordBot.commands
{
    public class ServerCommandModule : BaseCommandModule
    {

        int timeDelay = 60;
        static bool enabled = false;
        static bool ipUpdated = false;
        static Timer timer;

        static List<DiscordChannel> registeredChannels = new List<DiscordChannel>();

        public static DiscordClient discordClient { private get; set; }
        static string ip = "";

        
        TimerCallback tb = async _ => 
        {
            await UpdateIP();
            await PrintServerInfo();
        };

        /// <summary>
        /// Greet command handler.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("greet")]
        [Description("test command")]
        public async Task GreetCommand(CommandContext ctx)
        {
            await ctx.RespondAsync("Greetings!");
        }

        /// <summary>
        /// SetChannel command handler.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("register")]
        [Description("Sets the channel where server updates will be posted.")]
        public async Task RegisterChannel(CommandContext ctx)
        {
            if (!registeredChannels.Contains(ctx.Channel))
            {
                registeredChannels.Add(ctx.Channel);
                await ctx.RespondAsync($"channel \"{ctx.Channel.Name}\" successfully registered!");
            }
            else
            {
                await ctx.RespondAsync("Channel already registered!");
            }
        }

        [Command("unregister")]
        public async Task UnregisterChannel(CommandContext ctx)
        {
            var res = registeredChannels.Remove(ctx.Channel);
            if (res)
            {
                await ctx.RespondAsync("Channel successfuly unregistered.");
            }
            else
            {
                await ctx.RespondAsync("Channel already unregistered!");
            }
        }

        /// <summary>
        /// IP command handler.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("ip")]
        [Description("")]
        public async Task GetIP(CommandContext ctx)
        {
            discordClient = ctx.Client;
            await UpdateIP();
            await PrintServerInfo(true, ctx);
        }

        /// <summary>
        /// Start command handler.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("start")]
        [Description("")]
        public async Task StartTimer(CommandContext ctx) 
        {
            if (registeredChannels.Count <= 0)
            {
                await ctx.RespondAsync("No channels registered for server updates!");
            }
            else
            {
                await ctx.RespondAsync("Starting timed events...");
                timer = new Timer(new TimerCallback(tb), null, TimeSpan.Zero, TimeSpan.FromMinutes(timeDelay));
            }
        }

        /// <summary>
        /// Stop command handler.
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [Command("stop")]
        [Description("")]
        public async Task StopTimer(CommandContext ctx)
        {
            if (timer != null)
            {
                timer.Dispose();
                await ctx.RespondAsync("Timer stopped");
            }
        }

        /// <summary>
        /// Timer command handler.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="action"></param>
        /// <param name="var1"></param>
        /// <returns></returns>
        [Command("timer")]
        [Description("description")]
        public async Task Timer(CommandContext ctx, string action, int? var1 = null)
        {
            switch (action)
            {
                case "set":
                    if (var1 != null)
                    {
                        await ctx.RespondAsync(SetTimeDelay((int)var1));
                    }
                    else
                    {
                        await ctx.RespondAsync("No time delay argument found.");
                    }
                    break;
                case "info":
                    await ctx.RespondAsync(GetTimerInfo());
                    break;
                default:
                    break;
            }
        }

        [Command("list_channels"), Hidden, RequireOwner]
        public async Task DebugChannels(CommandContext ctx)
        {
            var response = "Registered Channels:\n";

            foreach (var ch in registeredChannels)
            {
                response += "   -   ";
                response += ch.Name;
                response += "\n";
            }

            await ctx.RespondAsync(response);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="force"></param>
        /// <returns></returns>
        private static async Task PrintServerInfo(bool force = false, CommandContext? ctx = null)
        {
            //ensure that the discordClient is set
            if (discordClient == null)
            {
                return;
            }

            if (force || ipUpdated)
            {
                var mBuilder = new DiscordMessageBuilder()
                    .WithContent("Server Details Updated!")
                    .AddComponents(new DiscordComponent[]
                {
                    new DiscordButtonComponent(ButtonStyle.Primary, "btn_copy_address", "Copy Address"),
                    new DiscordLinkButtonComponent($"http://{ip}:8123", "Server Map")
                });

                DiscordEmbedBuilder eBuilder = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.CornflowerBlue,
                    Title = "Server IP",
                    Description = $"Prawn Land Info:\n\n`ip`  -  `{ip}`\n\n`port`  -  `25565`",
                    ImageUrl = "https://external-content.duckduckgo.com/iu/?u=https%3A%2F%2Ftse4.mm.bing.net%2Fth%3Fid%3DOIP.Q2g3nQWkrbYvYF5EWIOUBwHaE8%26pid%3DApi&f=1"
                };

                mBuilder.WithEmbed(eBuilder);

                discordClient.ComponentInteractionCreated += async (s, e) =>
                {
                    if (e.Id == "btn_copy_address")
                    {
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                        await ClipboardService.SetTextAsync($"{ip}:25565");
                    }

                };

                //if a CommandContext is passed then just send the message to the associated channel, otherwise notify all.
                if (ctx != null)
                {
                    await ctx.Channel.SendMessageAsync(mBuilder);
                }
                else
                {
                    foreach (var ch in registeredChannels)
                    {
                        await ch.SendMessageAsync(mBuilder);
                    }
                }
            }
        }

        /// <summary>
        /// Get the server IP from ipinfo.io and check if it 
        /// has changed since the last update.
        /// </summary>
        /// <returns></returns>
        private static async Task UpdateIP()
        {
            string url = "https://ipinfo.io/json";
            var client = new HttpClient();
            var response = await client.GetStringAsync(url);
            var newIP = JObject.Parse(response.ToString())["ip"]!.ToString();

            if (newIP.ToString() != ip)
            {
                ip = newIP.ToString();
                ipUpdated = true;
            }
            else 
            {
                ipUpdated = false;
            }
        }

        /// <summary>
        /// Set the timer period.
        /// </summary>
        /// <param name="timeInMinutes">Timer period in minutes</param>
        /// <returns>Returns a string result.</returns>
        private string SetTimeDelay(int timeInMinutes)
        {
            string response;
            if (timeInMinutes <= 0)
            {
                response = "Time delay needs to be a non-negative integer";
                return response;
            }

            timeDelay = timeInMinutes;
            response = $"Timer set for {timeDelay} minute(s).";
            if (timer != null)
            {
                timer.Dispose();
                timer = new Timer(new TimerCallback(tb), null, TimeSpan.FromMinutes(timeDelay), TimeSpan.FromMinutes(timeDelay));
            }
            return response;
        }

        /// <summary>
        /// Return a string containing the timer info. 
        /// </summary>
        /// <returns></returns>
        private string GetTimerInfo()
        {
            return $"Time delay currently set for {timeDelay} minutes.";
        }
    }
}
