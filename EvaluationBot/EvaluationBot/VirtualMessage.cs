using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace EvaluationBot
{
    /// <summary>
    /// A message that never existed in Discord (was never sent)
    /// </summary>
    public class VirtualMessage : IUserMessage
    {
        public VirtualMessage()
        {

        }
        public VirtualMessage(IUserMessage copy)
        {

            Type = copy.Type;
            Source = copy.Source;
            IsTTS = copy.IsTTS;
            IsPinned = copy.IsPinned;
            Timestamp = copy.Timestamp;
            Content = copy.Content;
            EditedTimestamp = copy.EditedTimestamp;
            Channel = copy.Channel;
            Author = copy.Author;
            Attachments = copy.Attachments;
            Embeds = copy.Embeds;
            Tags = copy.Tags;
            MentionedChannelIds = copy.MentionedChannelIds;
            MentionedRoleIds = copy.MentionedRoleIds;
            MentionedUserIds = copy.MentionedUserIds;
            CreatedAt = copy.CreatedAt;
            Reactions = copy.Reactions;
            if (copy is VirtualMessage virtualMessage) Original = virtualMessage;
            else Original = copy;
        }

        public MessageType Type { get; set; }

        public MessageSource Source { get; set; }

        public bool IsTTS { get; set; }

        public bool IsPinned { get; set; }

        public string Content { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public DateTimeOffset? EditedTimestamp { get; set; }

        public IMessageChannel Channel { get; set; }

        public IUser Author { get; set; }

        public IReadOnlyCollection<IAttachment> Attachments { get; set; }

        public IReadOnlyCollection<IEmbed> Embeds { get; set; }

        public IReadOnlyCollection<ITag> Tags { get; set; }

        public IReadOnlyCollection<ulong> MentionedChannelIds { get; set; }

        public IReadOnlyCollection<ulong> MentionedRoleIds { get; set; }

        public IReadOnlyCollection<ulong> MentionedUserIds { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public IUserMessage Original { get; private set; }

        public ulong Id { get; set; }

        public async Task DeleteAsync(RequestOptions options = null)
        {
            if (Original != null) await Original.DeleteAsync(options);
        }


        public IReadOnlyDictionary<IEmote, ReactionMetadata> Reactions { get; set; }

        public async Task AddReactionAsync(IEmote emote, RequestOptions options = null)
        {
            if (Original != null) await Original.AddReactionAsync(emote, options);
        }

        public async Task<IReadOnlyCollection<IUser>> GetReactionUsersAsync(string emoji, int limit = 100, ulong? afterUserId = null, RequestOptions options = null)
        {
            if (Original != null) return await Original.GetReactionUsersAsync(emoji, limit, afterUserId, options);
            else return null;
        }

        public async Task ModifyAsync(Action<MessageProperties> func, RequestOptions options = null)
        {
            if (Original != null) await Original.ModifyAsync(func, options);
        }

        public async Task PinAsync(RequestOptions options = null)
        {
            if (Original != null) await Original.PinAsync(options);
            IsPinned = true;
        }

        public async Task RemoveAllReactionsAsync(RequestOptions options = null)
        {
            if (Original != null) await Original.RemoveAllReactionsAsync(options);
        }

        public async Task RemoveReactionAsync(IEmote emote, IUser user, RequestOptions options = null)
        {
            if (Original != null) await Original.RemoveReactionAsync(emote, user, options);
        }

        public string Resolve(TagHandling userHandling = TagHandling.Name, TagHandling channelHandling = TagHandling.Name, TagHandling roleHandling = TagHandling.Name, TagHandling everyoneHandling = TagHandling.Ignore, TagHandling emojiHandling = TagHandling.Name)
        {
            if (Original != null) return Original.Resolve(userHandling, channelHandling, roleHandling, everyoneHandling, emojiHandling);
            else return null;
        }

        public async Task UnpinAsync(RequestOptions options = null)
        {
            if (Original != null) await Original.UnpinAsync(options);
            IsPinned = false;
        }
    }
}
