using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DiscordBot.commands;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity.Enums;
using TextCopy;
using DSharpPlus.CommandsNext.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot
{
    internal class Bot
    {
        private IConfigurationRoot? config;
        private DiscordClient discord;

        static async Task Main(string[] args) => await new Bot().InitBot();

        async Task InitBot() 
        {

            //load config file
            Console.WriteLine("[info] Loading config file...");
            config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();

            //create discord client
            Console.WriteLine("[info] Creating discord client...");
            discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = config.GetValue<string>("discord:token"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged,
                MinimumLogLevel = LogLevel.Debug,
                LogTimestampFormat = "dd MMM yyyy - hh:mm:ss tt"
            });
            discord.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromSeconds(30),
                ResponseBehavior = InteractionResponseBehavior.Ack
            });

            //cheeky bit of dependency injection
            /*var services = new ServiceCollection()
                .AddSingleton<Bot>(this)
                .BuildServiceProvider();*/

            Console.WriteLine("[info] Loading command modules...");
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration() 
            {
                StringPrefixes = new[] { config.GetValue<string>("discord:prefix") },
                //Services = services
            });

            //use some reflection to load all class types that inherit from BaseCommandModule.
            var modules = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => t.IsSubclassOf(typeof(BaseCommandModule)));
            var typeList = modules as Type[] ?? modules.ToArray();

            foreach (var type in typeList)
            {
                try
                {
                    commands.RegisterCommands(type);
                    Console.WriteLine($"[info] Loaded Module: {type.Name}.");
                }
                catch (DuplicateCommandException)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[WARNING] Duplicate command module, \"{type.Name}\", skipping...");
                    Console.ResetColor();
                }
            }

            Console.WriteLine("[info] Connecting to discord service...");
            await discord.ConnectAsync();
            await Task.Delay(-1);
            Console.WriteLine("[info] Connected!");
        }
    }
}