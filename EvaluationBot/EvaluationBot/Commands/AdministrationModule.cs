using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvaluationBot.CommandServices;
using System.Globalization;
using EvaluationBot.Extensions;

namespace EvaluationBot.Commands
{
    [Name("Administration")]
    [Summary("Commands used by moderators and admins to run the server.")]
    public class AdministrationModule : ModuleBase
    {
        private Services services;

        public AdministrationModule(Services services)
        {
            this.services = services;
        }

        [Command("say")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Say(IMessageChannel channel, [Remainder]string content)
        {
            await Context.Message.DeleteAsync();
            await channel.SendMessageAsync(content);
        }
        [Command("say")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Say([Remainder]string content)
        {
            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync(content);
        }

        [Command("warn"), Summary("Warns a given user that their behaviour wasn't appropriate. Syntax: ``!warn (user) (reason)``")]
        [Alias("badboy")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Warn(IGuildUser user, [Remainder]string reason)
        {
            await Context.Message.DeleteAsync();
            services.databaseLoader.AddWarning(user, $"\"{reason}\" {DateTime.UtcNow.ToString("g", CultureInfo.CreateSpecificCulture("en-US"))} (warning by {Context.User.Tag()})");
            await Program.LogChannel.SendMessageAsync($"{Context.User.Mention} warned {user.Mention} that \"{reason}\"");
            await ReplyAsync($"**{user.Mention} {reason}** \n Persisting with this behaviour will get you muted and eventually banned.");
        }

        [Command("warnings"), Summary("Gets all warnings for the specified user. Syntax: ``!warnings (user)``")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task WarningList(IGuildUser user)
        {
            var info = await services.databaseLoader.GetInfo(user);
            StringBuilder builder = new StringBuilder();
            builder.Append($"Warnings for {user.Tag()} (Count: {info.Warnings.Length}): \n");
            for (int i = 0; i < info.Warnings.Length; i++)
            {
                builder.Append($"{i}: {info.Warnings[i]} \n");
            }
            await ReplyAsync(builder.ToString());
        }

        [Command("clearwarnings"), Summary("Gets all warnings for the specified user. Syntax: ``!clearwarnings (user)``")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task ClearWarnings(IGuildUser user)
        {
            services.databaseLoader.ClearWarnings(user);
            await ReplyAsync("Warnings cleared!");
        }

        [Command("removewarning"), Summary("Removes a warning from the specified user. Syntax: !removewarning (user) (index)")]
        [Alias("deletewarning")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task RemoveWarnings(IGuildUser user, int index)
        {
            services.databaseLoader.RemoveWarning(user, index);
            await ReplyAsync("Warning removed!");
        }

        [Command("kick")]
        [Summary("Kicks a specified member. Syntax: ``!kick (user)``")]
        [Alias("bye")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Kick(IGuildUser user, [Remainder]string reason)
        {
            await Context.Message.DeleteAsync();
            await user.KickAsync(reason);
            await Program.LogChannel.SendMessageAsync($"{Context.User.Mention} kicked {user.Mention} for \"{reason}\"");
        }

        [Command("ban")]
        [Summary("Bans a given member. Syntax: ``!ban (user) (reason) (delete messages, true/false)``")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Ban(IGuildUser user, string reason, bool purgeMessages = true)
        {
            await Context.Message.DeleteAsync();
            if (purgeMessages) await user.Guild.AddBanAsync(user, 7, reason: reason);
            else await user.Guild.AddBanAsync(user, reason: reason);
            await Program.LogChannel.SendMessageAsync($"{Context.User.Mention} banned {user.Mention} for \"{reason}\"");
        }

        [Command("unban")]
        [Summary("Unbans a given member member. Syntax: ``!unban (user)``")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Unban(IGuildUser user)
        {
            await user.Guild.RemoveBanAsync(user);
            await ReplyAsync("User unbanned").DeleteAfterTime(minutes: 3);
            await Program.LogChannel.SendMessageAsync($"{Context.User.Mention} unbanned \"{user.Mention}\"");
        }

        [Command("softban")]
        [Summary("Bans, and then unbans a member, deleting messages. Syntax: ``!softban (user) (reason)``")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task SoftBan(IGuildUser user, [Remainder]string reason)
        {
            await Context.Message.DeleteAsync();
            await user.Guild.AddBanAsync(user, reason: reason);
            await user.Guild.RemoveBanAsync(user);
            await Program.LogChannel.SendMessageAsync($"{Context.User.Mention} softbanned {user.Mention} for \"{reason}\"");
        }

        [Command("prunedb")]
        [Summary("Purges the database. You must be the bot owner to run this! Syntax: ``!purgedb (days to purge)``")]
        [RequireUserId(385164566658678784)]
        public async Task PruneDb(int days = 7)
        {
            services.databaseLoader.PruneDatabase(TimeSpan.FromDays(days));
            await Context.User.DM("Database pruned");
        }

        [Command("mute")]
        [Alias("stfu", "shutup", "hush", "silence")]
        [Summary("Mutes a given member for some time. Syntax: ``!mute (user) (time in seconds) (reason)``")]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        public async Task MuteCommand(IGuildUser user, uint seconds, [Remainder]string reason = "Not specified")
        {
            if (user.IsBot)
            {
                await ReplyAsync("You shall not defeat my kind! :smiling_imp:");
            }
            else
            {
                services.databaseLoader.AddWarning(user, $"\"{reason}\" {DateTime.UtcNow.ToString("g", CultureInfo.CreateSpecificCulture("en-US"))} (mute by {Context.User.Tag()})");
                await Context.Message.DeleteAsync();
                await services.time.Mute(user, seconds, reason, Context);
            }
        }
        [Command("mute")]
        [Alias("stfu", "shutup", "hush", "silence")]
        [Summary("Mutes a given member for some time. Syntax: ``!mute (user) (time in seconds) (reason)``")]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        public async Task MuteCommand(IGuildUser user, TimeSpan time, [Remainder]string reason = "Not specified")
        {
            if (user.IsBot)
            {
                await ReplyAsync("You shall not defeat my kind! :smiling_imp:");
            }
            else
            {
                services.databaseLoader.AddWarning(user, $"\"{reason}\" {DateTime.UtcNow.ToString("g", CultureInfo.CreateSpecificCulture("en-US"))} (mute by {Context.User.Tag()})");
                await Context.Message.DeleteAsync();
                await services.time.Mute(user, time, reason, Context);
            }
        }

        [Command("joinstats")]
        [Alias("serverjoinstats")]
        [Summary("Gets Stats on the users that joined server and respective times. Syntax: ``!serverjoinstats timeBy(days/months)``")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task JoinStats(string by)
        {
            //await Context.Guild.DownloadUsersAsync();
            StringBuilder builder = new StringBuilder("Joining stats for the server by ");
            if (by.ToLower() == "day")
            {
                builder.Append("day \n");
                IOrderedEnumerable<IGrouping<DateTime, IGuildUser>> users = (await Context.Guild.GetUsersAsync())
                    .GroupBy(x=>x.JoinedAt?.Date ?? default(DateTime))
                    .OrderBy(x => x.Key);
                foreach (IGrouping<DateTime, IGuildUser> group in users)
                {
                    if (group.Key == default(DateTime)) builder.Append("Inconclusive: ");
                    else builder.Append(group.Key.ToShortDateString());
                    builder.Append(" : ");
                    builder.Append(group.Count());
                    builder.Append(" users \n");
                }
            }
            else
            {
                builder.Append("month \n");
                IOrderedEnumerable<IGrouping<DateTime, IGuildUser>> users = (await Context.Guild.GetUsersAsync())
                    .GroupBy(x => x.JoinedAt.HasValue ? new DateTime( x.JoinedAt.Value.Year, x.JoinedAt.Value.Month, 1) : default(DateTime))
                    .OrderBy(x => x.Key);
                foreach (IGrouping<DateTime, IGuildUser> group in users)
                {
                    if (group.Key == default(DateTime)) builder.Append("Inconclusive: ");
                    else builder.Append(group.Key.ToShortDateString());
                    builder.Append(" : ");
                    builder.Append(group.Count());
                    builder.Append(" users \n");
                }

                //builder.Append("month \n");
                //IOrderedEnumerable< IGrouping<int, IGrouping<int, IGuildUser>>> users = (await Context.Guild.GetUsersAsync())
                //    .GroupBy(x => x.JoinedAt?.Month ?? 0)
                //    .GroupBy(x=> x.First().JoinedAt?.Year ?? 0)
                //    .OrderBy(x => x.Key)
                //    .Select(x=>x.Or)
                //foreach (IGrouping<int, IGrouping<int, IGuildUser>> outterGroup in users)
                //{
                //    if (outterGroup.Key == 0)
                //    {
                //        builder.Append("Inconclusive: ");
                //        builder.Append(outterGroup.First().Count());
                //        continue;
                //    }
                //    builder.Append("**");
                //    builder.Append(outterGroup.Key);
                //    builder.Append(":**\n           ");
                //    foreach(IGrouping<int, IGuildUser> innerGroup in outterGroup)
                //    {
                        
                //        builder.Append(innerGroup.Key);
                //        builder.Append(" : ");
                //        builder.Append(innerGroup.Count());
                //        builder.Append(" users \n           ");
                //    }
                //}
            }
            await ReplyAsync(builder.ToString());
        }

        [Command("clear")]
        [Summary("Clears a given amount of messages Syntax: ``!clear (amount)``")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
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
        [Summary("Clears messages up to a given message. Syntax: ``!clear (message id)``")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Clear(ulong id)
        {
            await Context.Message.DeleteAsync();

            IMessageChannel channel = Context.Channel;
            List<IMessage> messages = (await channel.GetMessagesAsync(id, Direction.After).Flatten()).ToList();

            await channel.DeleteMessagesAsync(messages);

            await ReplyAsync($"{messages.Count} messages deleted.");
        }

        [Command("unmute")]
        [Summary("Unmutes a given member. Syntax: ``!unmute (user)``")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task UnmuteCommand(IGuildUser user) => await services.time.Unmute(user);
    }
}
