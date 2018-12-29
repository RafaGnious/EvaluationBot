using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using Discord;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

using EvaluationBot.CommandServices;

namespace EvaluationBot.Data
{
    //Class not commented, add comments as you work on this! :)

    public class DataBaseLoader
    {
        private static MongoClient client;
        private static IMongoCollection<UserInfo> UserInfos;
        private static IMongoCollection<TimedAction> TimedActions;

        private Services services;

        public DataBaseLoader(Services services)
        {
            this.services = services;
        }

        public void LoadDatabase()
        {
            try
            {
                client = new MongoClient(Program.PrivateSettings.DatabaseConnectionString);
            }
            catch (Exception e)
            {
                Program.LogChannel.SendMessageAsync("Failed to connect with database. /n" + e.Message);
            }

            IMongoDatabase db = client.GetDatabase("admin");
            UserInfos = db.GetCollection<UserInfo>(Program.PrivateSettings.UserCollection);
            TimedActions = db.GetCollection<TimedAction>(Program.PrivateSettings.TimedActionsCollection);

            PruneDatabase();
        }

        public void ReloadTimedActions()
        {
            IFindFluent<TimedAction, TimedAction> Finder = TimedActions.Find(Builders<TimedAction>.Filter.Eq("Kind", "mute"));
            foreach (TimedAction mute in Finder.ToEnumerable())
            {
                services.time.MutedUsers.Add(mute.GetDiscordId(), (mute.Start, mute.End));
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                services.time.AwaitUnmute(Program.Guild.GetUser(mute.GetDiscordId()));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            Finder = TimedActions.Find(Builders<TimedAction>.Filter.Eq("Kind", "reminder"));
            foreach (TimedAction reminder in Finder.ToEnumerable())
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                if(reminder.End>DateTime.Now)
                services.time.AwaitRemind(Program.Guild.GetUser(reminder.GetDiscordId()), int.Parse(reminder._id[reminder._id.Length-1].ToString()), reminder.AdditionalArg, reminder.End - DateTime.Now);
                else
                    services.time.AwaitRemind(Program.Guild.GetUser(reminder.GetDiscordId()), int.Parse(reminder._id[reminder._id.Length - 1].ToString()), reminder.AdditionalArg, TimeSpan.FromSeconds(1));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

        }

        private void GenerateNewInfo(IUser user) => UserInfos.InsertOneAsync(new UserInfo(user.Id));

        public async Task AddOrUpdateMute(IUser user, DateTime Start, DateTime End)
        {
            if (user.IsBot) return;
            string _id = $"{user.Id}m0";

            if ((await TimedActions.Find(Builders<TimedAction>.Filter.Eq("_id", _id)).CountDocumentsAsync()) == 0)
            {
                TimedActions.InsertOne(new TimedAction(_id, "mute", Start, End));
                UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Inc("TimedActions", 1);
                await UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
            }
            else
            {
                TimedActions.ReplaceOne(Builders<TimedAction>.Filter.Eq("_id", _id), new TimedAction(_id, "mute", Start, End));
            }
        }
        public async Task<int> AddReminder(IUser user, string message, DateTime Start, DateTime End)
        {
            if (user.IsBot) return 0;
            int amount = (int)(await TimedActions.Find(Builders<TimedAction>.Filter.Where(x => x._id.StartsWith($"{user.Id}r"))).CountDocumentsAsync());
            if (amount > 9) return -1;
            string _id = $"{user.Id}r{amount}";

            TimedActions.InsertOne(new TimedAction(_id, "reminder", Start, End, message));
            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Inc("TimedActions", 1);
            await UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
            return amount;

        }

        public void RemoveTimedAction(string kind, int index, IUser user)
        {
            if (user.IsBot) return;
            TimedActions.DeleteOneAsync(Builders<TimedAction>.Filter.Eq("_id", $"{user.Id}{kind[0]}{index}"));
            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Inc("TimedActions", -1);
            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        public async Task<UserInfo> GetInfo(IUser user)
        {

            if (user.IsBot) return null;

            IFindFluent<UserInfo, UserInfo> finder = UserInfos.Find(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"));

            if (await finder.CountDocumentsAsync() == 0)
            {
                UserInfo newInfo = new UserInfo(user.Id);
                await UserInfos.InsertOneAsync(newInfo);
                return newInfo;
            }
            else
            {
                return await finder.FirstAsync();
            }
        }

        public void AddXp(IUser user, int xp)
        {
            if (user.IsBot) return;
            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Inc("Xp", xp);
            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);

        }

        public void AddKarma(IUser user, int karma)
        {
            if (user.IsBot) return;
            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Inc("Karma", karma);
            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        public void AddKarma(IEnumerable<IUser> users, int karma)
        {
            if (users.Count() == 1)
            {
                AddKarma(users.First(), karma);
                return;
            }

            if (users.Any(x => x.IsBot)) return;

            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Inc("Karma", karma);

            UserInfos.UpdateManyAsync(Builders<UserInfo>.Filter.Where(x => users.Any(y => y.Id == x.GetDiscordId())), update);
        }

        public void AddWarning(IUser user, string Warning)
        {
            if (user.IsBot) return;

            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Push("Warnings", Warning);

            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        public void RemoveWarning(IUser user, int index)
        {
            if (user.IsBot) return;

            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Unset($"Warnings.{index}");

            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        public void ClearWarnings(IUser user)
        {
            if (user.IsBot) return;

            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Unset("Warnings");

            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        public void SetBirthday(IUser user, DateTime date)
        {
            if (user.IsBot) return;

            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Set("Birthday", date);

            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        public async Task UserJoined(IUser user)
        {
            if (user.IsBot) return;

            IFindFluent<UserInfo, UserInfo> finder = UserInfos.Find(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"));

            if (await finder.CountDocumentsAsync() == 0)
            {
                GenerateNewInfo(user);
            }
            else
            {
                UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Unset("Leftdate");

                await UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
            }
        }

        public IEnumerable<UserInfo> GetTopXp(int users)
        {
           return UserInfos.Find(Builders<UserInfo>.Filter.Empty)
                .Sort(Builders<UserInfo>
                .Sort
                .Descending("Xp"))
                .Limit(users)
                .ToEnumerable();
        }

        public IEnumerable<UserInfo> GetTopKarma(int users)
        {
            return UserInfos.Find(Builders<UserInfo>.Filter.Empty).Sort(Builders<UserInfo>.Sort.Descending("Karma")).Limit(users).ToEnumerable();
        }

        public void PruneDatabase() => PruneDatabase(TimeSpan.FromDays(7));

        public void PruneDatabase(TimeSpan leftTime)
        {
            UserInfos.DeleteManyAsync(Builders<UserInfo>.Filter.Lt("Leftdate", DateTime.Now - leftTime));
        }

        //Database loading for upcoming "Birthday" feature.
        /*
        public static (DateTime, ulong[]) NextBirthday()
        {
            IFindFluent<UserInfo, UserInfo> finder = UserInfos.Find(Builders<UserInfo>.Filter.Ne("Birthday", default(DateTime)));
            finder.SortBy(x=>x.Birthday);
            finder.
        }
        */

        public void Left(IUser user, DateTime joindate)
        {
            if (user.IsBot) return;

            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Set("Leftdate", DateTime.Now);

            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        public void SetIntro(IUser user, ulong messageId)
        {
            if (user.IsBot) return;

            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Set("IntroMessage", messageId);

            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        [BsonIgnoreExtraElements]
        public class UserInfo
        {
            public string _id { get; set; }

            public ulong GetDiscordId()
            {
                return ulong.Parse(_id.Remove(18));
            }
            void SetDiscordId(ulong value)
            {
                _id = $"{value}aaaaaa";
            }

            public int Xp { get; set; }
            public int Karma { get; set; }
            public string[] Warnings { get; set; }
            public int TimedActions { get; set; }
            public ulong IntroMessage { get; set; }
            [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
            public DateTime Birthday { get; set; }

            public int Level()
            {
                return (int)Math.Floor(0.2 * Math.Pow(Xp, 0.5));
            }

            internal UserInfo(string id)
            {
                _id = id;
                Warnings = new string[0];
            }

            internal UserInfo(ulong id)
            {
                SetDiscordId(id);
                Warnings = new string[0];
            }
        }

        public class TimedAction
        {
            public string _id { get; set; }
            public string Kind { get; set; }

            public ulong GetDiscordId()
            {
                return ulong.Parse(_id.Remove(18));
            }

            void SetDiscordId(ulong value, int amount)
            {
                _id = $"{value}{Kind[0]}{amount}";
            }

            [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
            public DateTime Start { get; set; }
            [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
            public DateTime End { get; set; }

            public string AdditionalArg { get; set; }

            public TimedAction(string id, string kind, DateTime start, DateTime end, string arg = "")
            {
                _id = id;
                Kind = kind;
                Start = start;
                End = end;
                AdditionalArg = arg;
            }

            public TimedAction(IUser user, int amount, string kind, DateTime start, DateTime end, string arg = "")
            {
                _id = $"{user.Id}{kind[0]}{amount}";
                Kind = kind;
                Start = start;
                End = end;
                AdditionalArg = arg;
            }
        }
    }
}
