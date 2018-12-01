using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using EvaluationBot.Data;
using System.Globalization;

namespace EvaluationBot.Commands
{
    public class CommandsModule : ModuleBase
    {
        [Command("help")]
        [Summary("Shows commands and descriptions. Syntax: !help")]
        public async Task Help()
        {
            await ReplyAsync(Program.Help);
        }

        [Command("ping"), Summary("Display bot ping. Syntax : !ping")]
        [Alias("pong")]
        private async Task Ping()

        {

            var message = await ReplyAsync($"Pong :blush:");
            var time = message.Timestamp.Subtract(Context.Message.Timestamp);
            await message.ModifyAsync(m => m.Content = $"Pong :blush: (**{time.TotalMilliseconds}** *ms*)");
            await message.DeleteAfterTime(minutes: 3);
            await Context.Message.DeleteAfterTime(minutes: 3);

        }
        [Command("quote"), Summary("Quote a message. Syntax : !quote messageid (optional subtitle) (#channelname)")]
        private async Task QuoteMessage(ulong id, string subtitle = null, IMessageChannel channel = null)
        {
            // If channel is null use Context.Channel, else use the provided channel
            channel = channel ?? Context.Channel;


            var message = await channel.GetMessageAsync(id);
            string messageLink = "https://discordapp.com/channels/" + Context.Guild.Id + "/" + (channel == null
                                     ? Context.Channel.Id
                                     : channel.Id) + "/" + id;


            var builder = new EmbedBuilder()
                .WithColor(new Color(200, 128, 128))
                .WithTimestamp(message.Timestamp)
                .WithFooter(footer =>
                {
                    footer
                        .WithText($"In channel {message.Channel.Name}");
                })
                .WithTitle("Linkback")
                .WithUrl(messageLink)
                .WithAuthor(author =>
                {
                    author
                        .WithName(message.Author.Username)
                        .WithIconUrl(message.Author.GetAvatarUrl());
                })
                .AddField("Original message", message.Content.Truncate(1020));
            if (subtitle != null)
            {
                builder.AddField($"{Context.User.Username}: ", subtitle);
            }

            var embed = builder.Build();

            await ReplyAsync((subtitle == null) ? "" : $"*{Context.User.Username}:* {subtitle}", false, embed);

            await Task.Delay(1000);

            await Context.Message.DeleteAsync();

        }

        [Command("intro"), Alias("introduction"), Summary("Gets a user's introduction message. Syntax : !intro user")]
        private async Task GetIntro(IGuildUser user)
        {
            var info = await DataBaseLoader.GetInfo(user);
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

            await Task.Delay(1000);

            await Context.Message.DeleteAsync();
        }

        [Command("coinflip"), Alias("flipcoin")]
        [Summary("Returns either Tails or Heads. Syntax: !coinflip")]
        public async Task CoinFlip()
        {
            switch (new Random().Next() % 2)
            {
                case 1:
                    await ReplyAsync("Tails");
                    break;
                default:
                    await ReplyAsync("Heads");
                    break;

            }
        }

        [Command("random")]
        [Summary("Returns an integer between 1 and the specified number. Syntax: !random max(exclusive) min(optional-inclusive)")]
        public async Task Random(int max, int min = 1)
        {
            await ReplyAsync(((new Random().Next() % max) + min).ToString());
        }

        [Command("profile"), Summary("Shows the user's data. Syntax : !profile")]
        private async Task Profile(IUser user = null)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithColor(Color.Gold);
            embedBuilder.Title = user == null ? Context.User.Username : user.Username;
            DataBaseLoader.UserInfo info = await DataBaseLoader.GetInfo(user ?? Context.User);
            embedBuilder.AddField("Level: ", info.Level()).AddInlineField("Xp: ", info.Xp);
            embedBuilder.AddField("Karma: ", info.Karma);
            await ReplyAsync("", embed: embedBuilder.Build());
        }
        [Command("birthday"), Summary("Coming soon"/*"Shows the user's birthday. Syntax : !birthday user"*/)]
        [Alias("bday")]
        private async Task Birthday(IGuildUser user)
        {
            await ReplyAsync("Coming soon");
            //DateTime date = (await DataBaseLoader.GetInfo(user)).Birthday;
            //if (date != default(DateTime)) await ReplyAsync($"{user.Nickname}'s birthday is at {date.ToLongDateString()}");
            //else await ReplyAsync($"{user.Nickname} didn't register his birthday in the database. He can do so by using the command !setbirthday DD-MM-YYYY");
        }

        [Command("setbirthday"), Summary("Coming soon"/*"Sets your birthday. Syntax : !birthday DD-MM-YYYY"*/)]
        [Alias("setbday")]
        private async Task SetBirthday(string date)
        {
            await ReplyAsync("Coming soon");
            //DataBaseLoader.SetBirthday(Context.User, new DateTime(2001, 7, 30)/*DateTime.ParseExact(date, "dd-MM-yy", CultureInfo.InvariantCulture)*/);
            //await ReplyAsync("Done!").DeleteAfterSeconds(30);
        }

        [Command("warn"), Summary("Warns said user that his behaviour wasn't appropriate. Syntax: !warn user reason")]
        [Alias("badboy")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Warn(IGuildUser user, string reason)
        {
            await Context.Message.DeleteAsync();
            DataBaseLoader.AddWarning(user, $"\"{reason}\" {DateTime.UtcNow.ToString("g", CultureInfo.CreateSpecificCulture("en-US"))} (warning by {Context.User.Tag()})");
            await Program.LogChannel.SendMessageAsync($"{Context.User.Mention} warned {user.Mention} that \"{reason}\"");
            await ReplyAsync($"**{user.Mention} {reason}** \n Persisting with this behaviour will get you muted and eventually banned.");
        }

        [Command("warnings"), Summary("Gets all warnings to the specified user. Syntax: !warnings user ")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task WarningList(IGuildUser user)
        {
            var info = await DataBaseLoader.GetInfo(user);
            StringBuilder builder = new StringBuilder();
            builder.Append($"Warnings for {user.Tag()} (Count: {info.Warnings.Length}): \n");
            for (int i = 0; i < info.Warnings.Length; i++)
            {
                builder.Append($"{i}: {info.Warnings[i]} \n");
            }
            await ReplyAsync(builder.ToString());
        }

        [Command("clearwarnings"), Summary("Gets all warnings to the specified user. Syntax: !warnings user ")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task ClearWarnings(IGuildUser user)
        {
            DataBaseLoader.ClearWarnings(user);
            await ReplyAsync("Warnings cleared");
        }

        [Command("removewarning"), Summary("Gets all warnings to the specified user. Syntax: !warnings user ")]
        [Alias("deletewarning")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task RemoveWarnings(IGuildUser user, int index)
        {
            DataBaseLoader.RemoveWarning(user, index);
            await ReplyAsync("Warning removed");
        }

        [Command("kick")]
        [Summary("Kicks member. Syntax: !kick @user")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [Alias("bye")]
        public async Task Kick(IGuildUser user, string reason)
        {
            await Context.Message.DeleteAsync();
            await user.KickAsync(reason);
            await Program.LogChannel.SendMessageAsync($"{Context.User.Mention} kicked {user.Mention} for \"{reason}\"");
        }

        [Command("ban")]
        [Summary("Bans member. Syntax: !ban @user reason deleteMessages(true/false)")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Ban(IGuildUser user, string reason, bool purgeMessages = true)
        {
            await Context.Message.DeleteAsync();
            if (purgeMessages) await user.Guild.AddBanAsync(user, 7, reason: reason);
            else await user.Guild.AddBanAsync(user, reason: reason);
            await Program.LogChannel.SendMessageAsync($"{Context.User.Mention} banned {user.Mention} for \"{reason}\"");
        }

        [Command("unban")]
        [Summary("Unbans member. Syntax: !unban @user")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Unban(IGuildUser user)
        {
            await user.Guild.RemoveBanAsync(user);
            await ReplyAsync("User unbanned").DeleteAfterTime(minutes: 3);
            await Program.LogChannel.SendMessageAsync($"{Context.User.Mention} unbanned \"{user.Mention}\"");
        }

        [Command("softban")]
        [Summary("Bans and unbans member, deleting messages. Syntax: !softban @user reason")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task SoftBan(IGuildUser user, string reason)
        {
            await Context.Message.DeleteAsync();
            await user.Guild.AddBanAsync(user, reason: reason);
            await user.Guild.RemoveBanAsync(user);
            await Program.LogChannel.SendMessageAsync($"{Context.User.Mention} softbanned {user.Mention} for \"{reason}\"");
        }

        [Command("mute")]
        [Alias("stfu", "shutup", "hush", "silence")]
        [Summary("Mutes member for some time. Syntax: !mute @user time(seconds) reason")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task MuteCommand(IGuildUser user, uint seconds, string reason = "Not specified")
        {
            DataBaseLoader.AddWarning(user, $"\"{reason}\" {DateTime.UtcNow.ToString("g", CultureInfo.CreateSpecificCulture("en-US"))} (mute by {Context.User.Tag()})");
            await Context.Message.DeleteAsync();
            await Muting.Mute(user, seconds, reason, Context);
        }

        

        [Command("clear")]
        [Summary("Clears an amount of messages Syntax: !clear amount")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Clear(int amount)
        {
            if (amount < 1)
            {
                await ReplyAsync("cant delete less than one message").DeleteAfterSeconds(15);
                return;
            }
            await Context.Message.DeleteAsync();
            IMessageChannel channel = Context.Channel;
            List<IMessage> messages = (await channel.GetMessagesAsync(amount).Flatten()).ToList();
            messages.RemoveAll(x => x.Timestamp.Day > 14);
            await channel.DeleteMessagesAsync(messages);
            await ReplyAsync($"{messages.Count} messages deleted.");
        }
        [Command("clear")]
        [Summary("Clears messages until Syntax: !clear messageId")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Clear(ulong id)
        {

            await Context.Message.DeleteAsync();
            IMessageChannel channel = Context.Channel;
            List<IMessage> messages = (await channel.GetMessagesAsync(id, Direction.After).Flatten()).ToList();
            await channel.DeleteMessagesAsync(messages);
            await ReplyAsync($"{messages.Count} messages deleted.");


        }

        [Command("addrole")]
        [Summary("Adds specified role to the user requesting. Syntax: !addrole rolename")]
        public async Task AddRole(IRole role)
        {
            if (role.Permissions.BanMembers || role.Permissions.KickMembers)
            {
                await ReplyAsync("I'm sorry, you can't make yourself a mod...");
            }
            else if (!role.Permissions.SendMessages)
            {
                await ReplyAsync("You probably don't really want to do that");
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
        [Summary("Removes specified role from the user requesting. Syntax: !removerole rolename")]
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

        [Command("unmute")]
        [Summary("Unmutes member. Syntax: !unmute @user")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task UnmuteCommand(IGuildUser user) => await Muting.Unmute(user);

        
    }
}
