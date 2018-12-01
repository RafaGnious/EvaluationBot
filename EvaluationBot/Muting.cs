using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using EvaluationBot.Data;

namespace EvaluationBot
{
    public static class Muting
    {
        public static Dictionary<ulong, (DateTime start, DateTime end)> MutedUsers = new Dictionary<ulong, (DateTime start, DateTime end)>();
        public static IRole Role;
        static Muting()
        {

            Role = Program.Guild.Roles.First(x => !x.Permissions.SendMessages);
        }
        public static async Task Mute(IGuildUser user, uint seconds, string reason, ICommandContext Context = null)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            string Author;
            if (Context == null) Author = "I";
            else Author = Context.User.Mention;
            
            if (MutedUsers.ContainsKey(user.Id))
            {
                (DateTime start, DateTime end) tuple = MutedUsers[user.Id];
                tuple.end = tuple.start + (tuple.end - tuple.start).Add(time);
                MutedUsers[user.Id] = tuple;
                await user.DM($"Mute time increased by {time.ToString()}. You now have to wait more {tuple.end - DateTime.Now}. Reason: {reason}.");
                await Program.LogChannel.SendMessageAsync($"{Author} increased {user.Mention}'s mute time  by {time.ToString()} for \"{reason}\". {user.Mention} now will be muted for {Muting.MutedUsers[user.Id]}");
                DataBaseLoader.AddOrUpdateTimedAction("mute", user, MutedUsers[user.Id].start, MutedUsers[user.Id].end);
            }
            else
            {
                MutedUsers[user.Id] = (DateTime.Now, DateTime.Now + time);
                await user.AddRoleAsync(Muting.Role);
                await user.DM($"You have been muted for {time.ToString()}. Reason: {reason} \n Please do not try to go around this.");
                await Program.LogChannel.SendMessageAsync($"{Author} muted {user.Mention} for \"{reason}\" for {time.ToString()}");
                DataBaseLoader.AddOrUpdateTimedAction("mute", user, MutedUsers[user.Id].start, MutedUsers[user.Id].end);
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
                DataBaseLoader.RemoveTimedAction("mute", user);
                MutedUsers.Remove(user.Id);
                await user.RemoveRoleAsync(Role);
                await user.DM("You were unmuted");
                await Program.LogChannel.SendMessageAsync($"{user.Mention} was unmuted."); 
            }
        }
    }
    //public static class Muting
    //{
    //    
    //    static Thread thread;
    //    static TimeSpan nextUpdate;
    //    

    //    public static bool IsMuted(IGuildUser user) => MutedUsers.ContainsKey(user);

    //    

    //    public static async Task Mute(IGuildUser user, TimeSpan time)
    //    {
    //        /*if (thread.ThreadState == ThreadState.WaitSleepJoin) */thread.Abort();
    //        MutedUsers.Add(user, DateTime.Now + time);
    //        await user.AddRoleAsync(role);
    //        thread.Start();
    //    }

    //    public static async Task Unmute(IGuildUser user)
    //    {
    //        lock (MutedUsers)
    //        {
    //            MutedUsers.Remove(user);
    //        }
    //        if(Program.Guild.Users.Contains(user)) await user.RemoveRoleAsync(role);
    //    }

    //    static async void MuteUpdateThread()
    //    {
    //        while (MutedUsers.Count > 0)
    //        {
    //            nextUpdate = new TimeSpan(60,60,60);
    //            DateTime current = DateTime.Now;
    //            HashSet<IGuildUser> toUnmute = new HashSet<IGuildUser>();
    //            foreach (KeyValuePair<IGuildUser, DateTime> pair in MutedUsers)
    //            {
    //                if (pair.Value <= current)
    //                {
    //                    toUnmute.Add(pair.Key);
    //                }
    //                else if (pair.Value - current < nextUpdate)
    //                {
    //                    nextUpdate = pair.Value - current;
    //                }
    //            }
    //            foreach (IGuildUser user in toUnmute) await Unmute(user);
    //            Thread.Sleep(nextUpdate);
    //        }
    //    }

    //}
}
