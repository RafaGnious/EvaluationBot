using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using EvaluationBot.Data;
using EvaluationBot.CommandServices;

namespace EvaluationBot
{
    public class Muting
    {
        public static Dictionary<ulong, (DateTime start, DateTime end)> MutedUsers = new Dictionary<ulong, (DateTime start, DateTime end)>();
        public static IRole Role;

        public Services services;

        public Muting(Services services)
        {
            this.services = services;
            Role = Program.Guild.Roles.First(x => !x.Permissions.SendMessages);
        }

        public async Task Mute(IGuildUser user, uint seconds, string reason, ICommandContext Context = null)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);

            string Author;

            if (Context == null)
                Author = "I";
            else
                Author = Context.User.Mention;
            
            if (MutedUsers.ContainsKey(user.Id))
            {
                (DateTime start, DateTime end) tuple = MutedUsers[user.Id];
                tuple.end = tuple.start + (tuple.end - tuple.start).Add(time);
                MutedUsers[user.Id] = tuple;
                await user.DM($"Mute time increased by {time.ToString()}. You now have to wait more {tuple.end - DateTime.Now}. Reason: {reason}.");
                await Program.LogChannel.SendMessageAsync($"{Author} increased {user.Mention}'s mute time  by {time.ToString()} for \"{reason}\". {user.Mention} now will be muted for {Muting.MutedUsers[user.Id]}");
                await DataBaseLoader.AddOrUpdateTimedAction("mute", user, MutedUsers[user.Id].start, MutedUsers[user.Id].end);
            }
            else
            {
                MutedUsers[user.Id] = (DateTime.Now, DateTime.Now + time);
                await user.AddRoleAsync(Muting.Role);
                await user.DM($"You have been muted for {time.ToString()}. Reason: {reason} \n Please do not try to go around this.");
                await Program.LogChannel.SendMessageAsync($"{Author} muted {user.Mention} for \"{reason}\" for {time.ToString()}");
                await DataBaseLoader.AddOrUpdateTimedAction("mute", user, MutedUsers[user.Id].start, MutedUsers[user.Id].end);
                AwaitUnmute(user);
            }

        }

        public static async Task AwaitUnmute(IGuildUser user)
        {
            while (MutedUsers.ContainsKey(user.Id) && DateTime.Now < MutedUsers[user.Id].end)
            {
                await Task.Delay(MutedUsers[user.Id].end - DateTime.Now);
            }
            await Unmute(user);
        }

        public static async Task Unmute(IGuildUser user)
        {
            if (MutedUsers.ContainsKey(user.Id))
            {

                services.databaseLoader.RemoveTimedAction("mute", user);
                MutedUsers.Remove(user.Id);

                await user.RemoveRoleAsync(Role);
                await user.DM("You were unmuted, you are now allowed to speak in Evaluation Station.");

                await Program.LogChannel.SendMessageAsync($"{user.Mention} was unmuted."); 
            }
        }
    }
}
