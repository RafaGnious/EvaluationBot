using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using EvaluationBot.CommandServices;
using EvaluationBot.Extensions;

namespace EvaluationBot.CommandServices
{
    public class Timing
    {
        public Dictionary<ulong, (DateTime start, DateTime end)> MutedUsers = new Dictionary<ulong, (DateTime start, DateTime end)>();
        public IRole role;

        public Services services;

        public Timing(Services services)
        {
            this.services = services;
            role = Program.Guild.Roles.First(x => !x.Permissions.SendMessages);
        }

        #region MuteMethods
        public async Task Mute(IGuildUser user, uint seconds, string reason, ICommandContext Context = null) => await Mute(user, TimeSpan.FromSeconds(seconds), reason, Context);
        public async Task Mute(IGuildUser user, TimeSpan time, string reason, ICommandContext Context = null)
        {

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
                await Program.LogChannel.SendMessageAsync($"{Author} increased {user.Mention}'s mute time  by {time.ToString()} for \"{reason}\". {user.Mention} now will be muted for {services.time.MutedUsers[user.Id]}");
                await services.databaseLoader.AddOrUpdateMute( user, MutedUsers[user.Id].start, MutedUsers[user.Id].end);
            }
            else
            {
                MutedUsers[user.Id] = (DateTime.Now, DateTime.Now + time);
                await user.AddRoleAsync(services.time.role);
                await user.DM($"You have been muted for {time.ToString()}. Reason: {reason} \n Please do not try to go around this.");
                await Program.LogChannel.SendMessageAsync($"{Author} muted {user.Mention} for \"{reason}\" for {time.ToString()}");
                await services.databaseLoader.AddOrUpdateMute( user, MutedUsers[user.Id].start, MutedUsers[user.Id].end);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                AwaitUnmute(user);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

        }

        public async Task AwaitUnmute(IGuildUser user)
        {
            while (MutedUsers.ContainsKey(user.Id) && DateTime.Now < MutedUsers[user.Id].end)
            {
                await Task.Delay(MutedUsers[user.Id].end - DateTime.Now);
            }
            await Unmute(user);
        }

        public async Task Unmute(IGuildUser user)
        {
            if (MutedUsers.ContainsKey(user.Id))
            {
                services.databaseLoader.RemoveTimedAction("mute", 0, user);
                MutedUsers.Remove(user.Id);

                await user.RemoveRoleAsync(role);
                await user.DM("You were unmuted, you are now allowed to speak in Evaluation Station.");

                await Program.LogChannel.SendMessageAsync($"{user.Mention} was unmuted."); 
            }
        }
        #endregion

        public async Task AwaitRemind(IUser user, int databaseIndex, string message, TimeSpan time)
        {
            await Task.Delay(time);
            await user.DM($"You've set a reminder for this time. It said: \n **{message}**");
            services.databaseLoader.RemoveTimedAction("reminder", databaseIndex, user);
        }
    }
}
