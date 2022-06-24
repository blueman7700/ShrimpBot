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
using TextCopy;


namespace DiscordBot.commands
{
    public class DebugCommandModule : BaseCommandModule
    {

        [Command("modules"), Hidden, RequireOwner]
        public async Task ModuleInfo(CommandContext ctx) 
        {

            if (ctx.User.Id == 323890187770003456)
            {
                var modules = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => t.IsSubclassOf(typeof(BaseCommandModule)));

                var response = "Loaded modules: \n";

                foreach (var module in modules)
                {
                    response += module.Name;
                    response += "\n";
                }

                await ctx.RespondAsync(response);
            }
        }
    }
}
