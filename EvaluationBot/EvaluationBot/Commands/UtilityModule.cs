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
    [Name("Utility")]
    [Summary("Useful commands that anyone can use.")]
    public class UtilityModule : ModuleBase
    {
        private Services services;

        public UtilityModule(Services services)
        {
            this.services = services;
        }
        
        [Command("quote")]
        [Summary("Quote a users message. Syntax: ``!quote messageid (optional subtitle) (optional channel Name)``")]
 
        private async Task QuoteMessage(ulong id, string subtitle = null, IMessageChannel channel = null)
        {
            //If the channel is null, use the message context's channel.
            channel = channel ?? Context.Channel;

            //Find the message that was requested and make a link.
            IMessage message = await channel.GetMessageAsync(id);
            string messageLink = "https://discordapp.com/channels/" + Context.Guild.Id + "/" 
                + (channel == null ? Context.Channel.Id : channel.Id) + "/" + id;

            //Build an embed which contains the quote
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = new Color(200, 128, 128),
                Timestamp = message.Timestamp,

                Footer = new EmbedFooterBuilder()
                {
                    Text = $"In channel {message.Channel.Name}"
                },

                Title = new EmbedBuilder()
                {
                    Title = "Linkback",
                    Url = messageLink
                }.ToString(),

                Author = new EmbedAuthorBuilder()
                {
                    Name = message.Author.Username,
                    IconUrl = message.Author.GetAvatarUrl()
                }
            };

            //Add the original message
            builder.AddField("Original Message:", message.Content.Truncate(1024));

            //Add a subtitle if one was given
            string subtitleText = null;
            if (subtitle != null)
            {
                subtitleText = $"{Context.User.Username}: {subtitle}";
                builder.AddField($"{Context.User.Username}: ", subtitle);
            }

            //Build, and send the embed in a message.
            Embed embed = builder.Build();
            await ReplyAsync(subtitleText, false, embed);

            //Delete the requesters command
            await Context.Message.DeleteAsync();
        }

        [Command("intro")]
        [Alias("introduction")]
        [Summary("Gets a user's introduction message. Syntax: ``!intro (user)``")]
        private async Task GetIntro(IGuildUser user)
        {
            if(user.Id == Program.Client.CurrentUser.Id)
            {
                EmbedBuilder builder = new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = $"Introduction for {user.Username}"
                };

                builder.Description = "I'm the Evaluation Station server's personal assistant! Type ``!help`` to get an idea of what I can do.";

                Embed embed = builder.Build();

                await ReplyAsync(null, false, embed);
            }
            else if (user.IsBot)
            {
                await ReplyAsync("Introduction unavailable for bot");
            }
            else
            {
                var info = await services.databaseLoader.GetInfo(user);
                if (info.IntroMessage == 0)
                {
                    await ReplyAsync($"No intro for that user found.{user.Mention} send a message to <#{Program.PrivateSettings.IntrosChannel}> introducing yourself");
                    return;
                }
                IMessage message = await Program.IntrosChannel.GetMessageAsync(info.IntroMessage);
                if (message == null)
                {
                    await ReplyAsync($"Intro message for {user.Mention} seems to have been deleted. Try sending another message to <#{Program.PrivateSettings.IntrosChannel}>").DeleteAfterSeconds(20);
                    await Context.Message.DeleteAsync();
                    return;
                }
                string messageLink = "https://discordapp.com/channels/" + Context.Guild.Id + "/" + Program.PrivateSettings.IntrosChannel.ToString() + "/" + info.IntroMessage;
                var builder = new EmbedBuilder()
                        .WithColor(Color.LightOrange)
                        .WithTitle($"Introduction for {user.Username}")
                        .WithUrl(messageLink);
                builder.Description = message.Content;

                var embed = builder.Build();

                await ReplyAsync("", false, embed);
            }

            await Context.Message.DeleteAsync();
        }

        [Command("profile")]
        [Summary("Shows a user's profile. Syntax: ``!profile (optional user)``")]
        private async Task Profile(IUser user = null)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Gold);
            embedBuilder.Title = user == null ? Context.User.Username : user.Username;
            if (user != null && user.IsBot)
            {
                embedBuilder.AddField("Level: ", int.MaxValue).AddInlineField("Xp: ", int.MaxValue);
                embedBuilder.AddField("Karma: ", int.MaxValue);
            }
            else
            {
                DataBaseLoader.UserInfo info = await services.databaseLoader.GetInfo(user ?? Context.User);
                embedBuilder.AddField("Level: ", info.Level()).AddInlineField("Xp: ", info.Xp);
                embedBuilder.AddField("Karma: ", info.Karma); 
            }

            await ReplyAsync("", embed: embedBuilder.Build());
        }

        [Command("addrole")]
        [Summary("Adds a given role to the user requesting. Syntax: ``!addrole (rolename)``")]
        [BotCommandsChannel]
        public async Task AddRole(IRole role)
        {
            if ((role.Permissions.BanMembers || role.Permissions.KickMembers))
            {
                await ReplyAsync("I'm sorry, you can't make yourself a mod...");
            }
            else if (!role.Permissions.SendMessages)
            {
                await ReplyAsync("You probably don't really want to do that.");
            }
            else
            {
                IGuildUser user = await Context.Guild.GetUserAsync(Context.User.Id);
                if (user.RoleIds.Contains(role.Id)) await ReplyAsync("You already had that role");
                else
                {
                    await user.AddRoleAsync(role);
                    await ReplyAsync($"You now have the {role.Name} role");
                }
            }
        }

        [Command("removerole")]
        [Summary("Removes a given role from the user requesting. Syntax: ``!removerole (rolename)``")]
        [BotCommandsChannel]
        public async Task RemoveRole(IRole role)
        {
            if (role.Permissions.BanMembers || role.Permissions.KickMembers)
            {
                await ReplyAsync("Why would you do such thing?");
            }
            else if (!role.Permissions.SendMessages)
            {
                await ReplyAsync("Ha, you thought! Wait, how did you even call this command if you are muted?");
            }
            else
            {
                IGuildUser user = await Context.Guild.GetUserAsync(Context.User.Id);
                if (user.RoleIds.Contains(role.Id))
                {
                    await user.RemoveRoleAsync(role);
                    await ReplyAsync($"You no longer have the {role.Name} role");
                }
                else await ReplyAsync("You didnt actually have that role");
            }
        }

        [Command("remindme")]
        [Alias("reminder", "setreminder")]
        [Summary("Reminds you of something after some time has elapsed. Usage !remindme reminder time")]
        public async Task Reminder(string reminder, TimeSpan time)
        {
            int index = await services.databaseLoader.AddReminder(Context.User, reminder, DateTime.Now, DateTime.Now + time);
            if (index == -1)
                await ReplyAsync("You exceeded the limit of 10 reminders").DeleteAfterSeconds(15);
            else
            {
                await ReplyAsync("Reminder added :thumbsup:").DeleteAfterSeconds(15);
                await services.time.AwaitRemind(Context.User, index, reminder, time);
            }
        }

        //[Command("birthday")]
        //[Alias("bday")]
        //[Summary("Coming soon... Syntax: ``!birthday (optional user)``"/*"Shows the user's birthday. "*/)]
        //private async Task Birthday(IGuildUser user)
        //{
        //    await ReplyAsync("Coming soon...");
        //    //DateTime date = (await DataBaseLoader.GetInfo(user)).Birthday;
        //    //if (date != default(DateTime)) await ReplyAsync($"{user.Nickname}'s birthday is at {date.ToLongDateString()}");
        //    //else await ReplyAsync($"{user.Nickname} didn't register his birthday in the database. He can do so by using the command !setbirthday DD-MM-YYYY");
        //}

        //[Command("setbirthday")]
        //[Alias("setbday")]
        //[Summary("Coming soon... Syntax: ``!birthday DD-MM-YYYY``"/*"Sets your birthday."*/)]
        //private async Task SetBirthday(string date)
        //{
        //    await ReplyAsync("Coming soon...");
        //    //DataBaseLoader.SetBirthday(Context.User, new DateTime(2001, 7, 30)/*DateTime.ParseExact(date, "dd-MM-yy", CultureInfo.InvariantCulture)*/);
        //    //await ReplyAsync("Done!").DeleteAfterSeconds(30);
        //}
    }
}
