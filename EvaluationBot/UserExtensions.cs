using System;
using Discord;
using System.Threading.Tasks;

public static class UserExtensions
{
	public async static Task DM(this IUser user, string message)
    {
        IDMChannel channel = await user.GetOrCreateDMChannelAsync();
        await channel.SendMessageAsync(message);
    }

    public static string Tag(this IUser user)
    {
        return $"{user.Username}#{user.Discriminator}";
    }

}
