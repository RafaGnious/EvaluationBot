using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvaluationBot.CommandServices;
using System.Globalization;

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

        [Command("warn"), Summary("Warns a given user that their behaviour wasn't appropriate. Syntax: ``!warn (user) (reason)``")]
        [Alias("badboy")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Warn(IGuildUser user, string reason)
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
            await ReplyAsync("Warnings cleared");
        }

        [Command("removewarning"), Summary("Removes a warning from the specified user. Syntax: !removewarning (user) (index)")]
        [Alias("deletewarning")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task RemoveWarnings(IGuildUser user, int index)
        {
            services.databaseLoader.RemoveWarning(user, index);
            await ReplyAsync("Warning removed");
        }

        [Command("kick")]
        [Summary("Kicks a specified member. Syntax: ``!kick (user)``")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [Alias("bye")]
        public async Task Kick(IGuildUser user, string reason)
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
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task SoftBan(IGuildUser user, string reason)
        {
            await Context.Message.DeleteAsync();
            await user.Guild.AddBanAsync(user, reason: reason);
            await user.Guild.RemoveBanAsync(user);
            await Program.LogChannel.SendMessageAsync($"{Context.User.Mention} softbanned {user.Mention} for \"{reason}\"");
        }

        [Command("purgedb")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        [Summary("Purges the database. You must be the bot owner to run this! Syntax: ``!purgedb (days to purge)``")]
        public async Task PurgeDb(int days = 7)
        {
            if (Context.User.Id == 385164566658678784)
            {
                services.databaseLoader.PruneDatabase(TimeSpan.FromDays(days));
                await Context.User.DM("Database purged");
            }
        }

        [Command("mute")]
        [Alias("stfu", "shutup", "hush", "silence")]
        [Summary("Mutes a given member for some time. Syntax: ``!mute (user) (time in seconds) (reason)``")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task MuteCommand(IGuildUser user, uint seconds, string reason = "Not specified")
        {
            if (user.Id == Program.Client.CurrentUser.Id)
            {
                await ReplyAsync("I'm unmutable :smiling_imp:");
            }
            else
            {
                services.databaseLoader.AddWarning(user, $"\"{reason}\" {DateTime.UtcNow.ToString("g", CultureInfo.CreateSpecificCulture("en-US"))} (mute by {Context.User.Tag()})");
                await Context.Message.DeleteAsync();
                await Muting.Mute(user, seconds, reason, Context);
            }
        }



        [Command("clear")]
        [Summary("Clears a given amount of messages Syntax: ``!clear (amount)``")]
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
        [Summary("Clears messages up to a given message. Syntax: ``!clear (message id)``")]
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
        [Summary("Adds a given role to the user requesting. Syntax: ``!addrole (rolename)``")]
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
        [Summary("Removes a given role from the user requesting. Syntax: ``!removerole (rolename)``")]
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
        [Summary("Unmutes a given member. Syntax: ``!unmute (user)``")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task UnmuteCommand(IGuildUser user) => await Muting.Unmute(user);
    }
}
