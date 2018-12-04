using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using EvaluationBot.CommandServices;
using EvaluationBot.Extensions;

namespace EvaluationBot
{
    public class Muting
    {
        public Dictionary<ulong, (DateTime start, DateTime end)> mutedUsers = new Dictionary<ulong, (DateTime start, DateTime end)>();
        public IRole role;

        public Services services;

        public Muting(Services services)
        {
            this.services = services;
            role = Program.Guild.Roles.First(x => !x.Permissions.SendMessages);
        }

        public async Task Mute(IGuildUser user, uint seconds, string reason, ICommandContext Context = null)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);

            string Author;

            if (Context == null)
                Author = "I";
            else
                Author = Context.User.Mention;
            
            if (mutedUsers.ContainsKey(user.Id))
            {
                (DateTime start, DateTime end) tuple = mutedUsers[user.Id];
                tuple.end = tuple.start + (tuple.end - tuple.start).Add(time);
                mutedUsers[user.Id] = tuple;
                await user.DM($"Mute time increased by {time.ToString()}. You now have to wait more {tuple.end - DateTime.Now}. Reason: {reason}.");
                await Program.LogChannel.SendMessageAsync($"{Author} increased {user.Mention}'s mute time  by {time.ToString()} for \"{reason}\". {user.Mention} now will be muted for {services.silence.mutedUsers[user.Id]}");
                await services.databaseLoader.AddOrUpdateTimedAction("mute", user, mutedUsers[user.Id].start, mutedUsers[user.Id].end);
            }
            else
            {
                mutedUsers[user.Id] = (DateTime.Now, DateTime.Now + time);
                await user.AddRoleAsync(services.silence.role);
                await user.DM($"You have been muted for {time.ToString()}. Reason: {reason} \n Please do not try to go around this.");
                await Program.LogChannel.SendMessageAsync($"{Author} muted {user.Mention} for \"{reason}\" for {time.ToString()}");
                await services.databaseLoader.AddOrUpdateTimedAction("mute", user, mutedUsers[user.Id].start, mutedUsers[user.Id].end);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                AwaitUnmute(user);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

        }

        public async Task AwaitUnmute(IGuildUser user)
        {
            while (mutedUsers.ContainsKey(user.Id) && DateTime.Now < mutedUsers[user.Id].end)
            {
                await Task.Delay(mutedUsers[user.Id].end - DateTime.Now);
            }
            await Unmute(user);
        }

        public async Task Unmute(IGuildUser user)
        {
            if (mutedUsers.ContainsKey(user.Id))
            {
                services.databaseLoader.RemoveTimedAction("mute", user);
                mutedUsers.Remove(user.Id);

                await user.RemoveRoleAsync(role);
                await user.DM("You were unmuted, you are now allowed to speak in Evaluation Station.");

                await Program.LogChannel.SendMessageAsync($"{user.Mention} was unmuted."); 
            }
        }
    }
}
