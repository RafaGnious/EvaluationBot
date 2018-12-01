using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using Discord;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace EvaluationBot.Data
{
    public static class DataBaseLoader
    {
        static MongoClient client;
        private static IMongoCollection<UserInfo> UserInfos;
        private static IMongoCollection<TimedAction> TimedActions;

        public static void LoadDatabase()
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
        public static void ReloadTimedActions()
        {
            IFindFluent<TimedAction, TimedAction> Mutes = TimedActions.Find(Builders<TimedAction>.Filter.Eq("Kind", "mute"));
            foreach (TimedAction mute in Mutes.ToEnumerable())
            {
                Muting.MutedUsers.Add(mute.GetDiscordId(), (mute.Start, mute.End));
                Muting.AwaitUnmute(Program.Guild.GetUser(mute.GetDiscordId()));
            }
        }
        static void GenerateNewInfo(IUser user) => UserInfos.InsertOneAsync(new UserInfo(user.Id));

        public static async Task AddOrUpdateTimedAction(string kind, IUser user, DateTime Start, DateTime End)
        {
            if (user.IsBot) return;
            string _id = $"{user.Id}{kind[0]}{kind[0]}{kind[0]}{kind[0]}{kind[0]}{kind[0]}";
            if ((await TimedActions.Find(Builders<TimedAction>.Filter.Eq("_id", _id)).CountDocumentsAsync())==0)
            {
                TimedActions.InsertOne(new TimedAction(_id, kind, Start, End));
                UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Inc("TimedActions", 1);
                UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
            }
            else
            {
                TimedActions.ReplaceOne(Builders<TimedAction>.Filter.Eq("_id", _id), new TimedAction(_id, kind, Start, End));
            }
        }

        public static void RemoveTimedAction(string kind, IUser user)
        {
            if (user.IsBot) return;
            TimedActions.DeleteOneAsync(Builders<TimedAction>.Filter.Eq("_id", $"{user.Id}{kind[0]}{kind[0]}{kind[0]}{kind[0]}{kind[0]}{kind[0]}"));
            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Inc("TimedActions", -1);
            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        public static async Task<UserInfo> GetInfo(IUser user)
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

        public static void AddXp(IUser user, int xp)
        {
            if (user.IsBot) return;
            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Inc("Xp", xp);
            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);

        }

        public static void AddKarma(IUser user, int karma)
        {
            if (user.IsBot) return;
            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Inc("Karma", karma);
            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        public static void AddKarma(IEnumerable<IUser> users, int karma)
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

        public static void AddWarning(IUser user, string Warning)
        {
            if (user.IsBot) return;
            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Push("Warnings", Warning);
            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        public static void RemoveWarning(IUser user, int index)
        {
            if (user.IsBot) return;
            //List<string> warnings = (GetInfo(user)).Warnings.ToList();
            //warnings.RemoveAt(index);
            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Unset($"Warnings.{index}");
            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        public static void ClearWarnings(IUser user)
        {
            if (user.IsBot) return;
            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Unset("Warnings");
            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        public static void SetBirthday(IUser user, DateTime date)
        {
            if (user.IsBot) return;
            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Set("Birthday", date);
            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        public static async Task UserJoined(IUser user)
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

        public static void PruneDatabase() => PruneDatabase(TimeSpan.FromDays(7));

        public static void PruneDatabase(TimeSpan leftTime)
        {
            UserInfos.DeleteManyAsync(Builders<UserInfo>.Filter.Lt("Leftdate", DateTime.Now - leftTime));
        }

        //public static (DateTime, ulong[]) NextBirthday()
        //{
        //    IFindFluent<UserInfo, UserInfo> finder = UserInfos.Find(Builders<UserInfo>.Filter.Ne("Birthday", default(DateTime)));
        //    finder.SortBy(x=>x.Birthday);
        //    finder.
        //}

        public static void Left(IUser user, DateTime joindate)
        {
            if (user.IsBot) return;
            UpdateDefinition<UserInfo> update = Builders<UserInfo>.Update.Set("Leftdate", DateTime.Now);
            UserInfos.UpdateOneAsync(Builders<UserInfo>.Filter.Eq("_id", $"{user.Id}aaaaaa"), update);
        }

        public static void SetIntro(IUser user, ulong messageId)
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
            void SetDiscordId(ulong value)
            {
                _id = $"{value}{Kind[0]}{Kind[0]}{Kind[0]}{Kind[0]}{Kind[0]}{Kind[0]}";
            }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public TimedAction(string id, string kind, DateTime start, DateTime end)
            {
                _id = id;
                Kind = kind;
                Start = start;
                End = end;
            }
        }
    }
}
