using Discord;
using System.Threading.Tasks;
using System.Text;

namespace EvaluationBot.Extensions
{
    public static class UserExtensions
    {
        public async static Task DM(this IUser user, string message, bool PublicIfCantDM = true)
        {
            try
            {
                IDMChannel channel = await user.GetOrCreateDMChannelAsync();

                await channel.SendMessageAsync(message);
            }
            catch
            {
                StringBuilder builder = new StringBuilder();

                builder.Append($"Couldn't DM {user.Mention}.");

                if (PublicIfCantDM)
                {
                    builder.Append("Message was \n");
                    builder.Append(message);
                }

                await Program.CommandsChannel.SendMessageAsync(builder.ToString());
            }
        }

        public static string Tag(this IUser user)
        {
            return $"{user.Username}#{user.Discriminator}";
        }
    }
}
