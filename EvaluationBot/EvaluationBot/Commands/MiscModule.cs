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
    [Name("Misc")]
    [Summary("Random commands that don't have a lot of use.")]
    public class MiscModule : ModuleBase
    {
        private Services services;

        public MiscModule(Services services)
        {
            this.services = services;
        }

        [Command("ping")]
        [Alias("pong")]
        [Summary("Display the bots ping. Syntax: ``!ping``")]
        private async Task Ping()
        {
            //Make a message which contains the clients latency to Discord.
            IUserMessage message = await ReplyAsync($"Pong :blush: *{ (Context.Client as DiscordSocketClient).Latency.ToString() } ms*");
        }

        [Command("coinflip")]
        [Alias("flipcoin")]
        [Summary("Flips a coin! Heads or tails? Syntax: ``!coinflip``")]
        public async Task CoinFlip()
        {
            string side = services.random.Next(0, 2) == 0 ? "Tails!" : "Heads!";

            await ReplyAsync(side);
        }

        [Command("random")]
        [Alias("rand")]
        [Summary("Returns an integer between 1 and the specified number. Syntax: ``!random (maximum exclusive) (optional minimum inclusive)``")]
        public async Task Random(int max, int min = 1)
        {
            await ReplyAsync(((new Random().Next() % max) + min).ToString());
        }
    }
}
