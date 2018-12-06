using Discord.Commands;
using System;
using System.Threading.Tasks;
using Discord;
using EvaluationBot.Data;
using EvaluationBot.CommandServices;
using EvaluationBot.Extensions;
using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;

namespace EvaluationBot.Commands
{
    [Name("Help")]
    [Summary("Commands which help you get to where you need to be.")]
    public class HelpModule : ModuleBase
    {
        private Services services;

        public HelpModule(Services services)
        {
            this.services = services;
        }

        [Command("help")]
        [Alias("commands, modules")]
        [Summary("Gives you a quick run down of how to use the bot, the bots modules, and their descriptions. Syntax: ``!help``")]
        public async Task Help()
        {
            EmbedBuilder embed = new EmbedBuilder();
            EmbedFooterBuilder footer = new EmbedFooterBuilder();

            //Get the application info to get the bots icon
            IApplication application = await Context.Client.GetApplicationInfoAsync();

            embed.ThumbnailUrl = application.IconUrl;

            //Add a footer with the current UTC time
            footer.WithText(DateTime.UtcNow.ToString() + " UTC");
            embed.WithFooter(footer);
            embed.WithColor(new Color(0x4900ff));

            embed.Title = "Commands / Help";

            string prefixes = Program.PrivateSettings.Prefix;

            //List prefixes
            embed.AddField(y =>
            {
                y.Name = "Prefix(s)";
                y.Value = prefixes;
                y.IsInline = false;
            });

            embed.AddField(y =>
            {
                y.Name = "**Modules**";
                y.Value = "The command modules that are available. Use ``!module (module name)`` to see the modules commands.";
                y.IsInline = false;
            });

            //This try catch statement is here for debugging, it makes sure the embed sents in case something broke.
            try
            {
                IEnumerable<ModuleInfo> modules = services.GetModules();
                foreach (var module in modules)
                {
                    embed.AddField(y =>
                    {
                        y.Name = "**" + module.Name + "**";
                        y.Value = "*" + module.Summary + "*";
                        y.IsInline = false;
                    });
                }
            }
            catch (Exception e)
            {
                await Program.Log(new LogMessage(LogSeverity.Error, "Help Command, List Modules Error", e.Message + " - " + e.StackTrace));
            }

            await ReplyAsync("", embed: embed);
        }

        [Command("module")]
        [Alias("mod")]
        [Summary("Shows commands and their descriptions. Syntax: ``!module (module name)``")]
        public async Task Module([Remainder]string name)
        {
            ModuleInfo module = services.GetModules().First(n => (n.Name.ToLower() == name.ToLower()));

            if (module != null)
            {
                EmbedBuilder embed = new EmbedBuilder();
                EmbedFooterBuilder footer = new EmbedFooterBuilder();

                //Make the footer the current UTC time
                footer.WithText(DateTime.UtcNow.ToString() + " UTC");
                embed.WithFooter(footer);
                embed.WithColor(new Color(0x4900ff));

                //Make the title the modules name + some info.
                embed.Title = $"Module {module.Name}'s commands. Contains {module.Commands.Count} out of 25 max commands.";

                //Add each command in the module into its own field.
                foreach (var cmd in module.Commands)
                {
                    embed.AddField(y =>
                    {
                        y.Name = cmd.Name;
                        y.Value = cmd.Summary;
                        y.IsInline = false;
                    });
                }

                await ReplyAsync("", embed: embed);
            }
            else
            {
                //The module was an invalid name, tell the user
                IUserMessage msg = await ReplyAsync("A module with the name '" + name + "' does not exist.");
                msg.DeleteAfterSeconds(20);
            }
        }
    }
}
