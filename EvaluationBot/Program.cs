using Discord;
using System;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using EvaluationBot.Serialization;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using EvaluationBot;
using EvaluationBot.Data;

public class Program
{
    public static SocketGuild Guild;
    public static PrivateSettings PrivateSettings;

    public static DiscordSocketClient _client;
    static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();
    private CommandService commandService;
    public static string Help;
    public static ISocketMessageChannel LogChannel;
    public static ISocketMessageChannel IntrosChannel;
    public static ISocketMessageChannel CommandsChannel;
    static ISocketMessageChannel WelcomeChannel;

    public async Task MainAsync()
    {
        DeserializeSettings();
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info
        });
        commandService =
            new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async
            });
        DataBaseLoader.LoadDatabase();


        _client.Log += Log;

        await _client.LoginAsync(TokenType.Bot, PrivateSettings.Token);
        await _client.StartAsync();

        _client.Ready += _client_Ready;

        await InstallCommands();


        await Task.Delay(-1);
    }

    static bool Initialized = false;

    private Task _client_Ready()
    {
        if (!Initialized)
        {
            Guild = _client.GetGuild(PrivateSettings.ServerId);

            LogChannel = Guild.GetTextChannel(PrivateSettings.LogChannel);
            WelcomeChannel = Guild.GetTextChannel(PrivateSettings.WelcomeChannel);
            IntrosChannel = Guild.GetTextChannel(PrivateSettings.IntrosChannel);
            CommandsChannel = Guild.GetTextChannel(PrivateSettings.CommandsChannel);
            DataBaseLoader.ReloadTimedActions();
            Initialized = true;
            
        }
        return Task.CompletedTask; 
    }

    public async Task InstallCommands()
    {

        _client.MessageReceived += HandleCommand;
        _client.MessageReceived += XpHandler;
        _client.MessageReceived += KarmaHandler;
        _client.MessageReceived += Introductions;
        _client.MessageUpdated += IntroductionsEdit;

        _client.UserLeft += UserLeftAsync;
        _client.UserJoined += Welcome;
        _client.UserJoined += CheckForMuteEvasionAsync;

        await commandService.AddModulesAsync(Assembly.GetEntryAssembly());
        StringBuilder commandList = new StringBuilder();
        commandList.Append("Commands: \n");
        foreach (CommandInfo c in commandService.Commands.Where(x => x.Preconditions.Count == 0).OrderBy(c => c.Name))
        {
            commandList.Append($"**{c.Name}** : {c.Summary}\n");
        }
        Help = commandList.ToString();
    }



    private Task IntroductionsEdit(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
    {
        IntroductionsEditAsync(arg1, arg2, arg3);
        return Task.CompletedTask;
    }

    private async Task IntroductionsEditAsync(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
    {
        if (!arg2.Author.IsBot && arg2.Channel.Id == PrivateSettings.IntrosChannel)
        {
            DataBaseLoader.SetIntro(arg2.Author, arg2.Id);
            await arg2.Channel.SendMessageAsync($"{arg2.Author} I've updated your intro :thumbsup:").DeleteAfterSeconds(15);
        }
    }



    private Task Introductions(SocketMessage message)
    {
        IntroductionsAsync(message);
        return Task.CompletedTask;
    }

    async Task IntroductionsAsync(SocketMessage message)
    {
        if (!message.Author.IsBot && message.Channel.Id == PrivateSettings.IntrosChannel)
        {
            DataBaseLoader.SetIntro(message.Author, message.Id);
            await message.Channel.SendMessageAsync($"{message.Author} I've updated your intro :thumbsup:").DeleteAfterSeconds(15);
        }
    }

    public HashSet<ulong> XpCooldowns = new HashSet<ulong>();

    public HashSet<ulong> KarmaCooldowns = new HashSet<ulong>();

    private Task XpHandler(SocketMessage messageParam)
    {
        XpHandlerAsync(messageParam);
        return Task.CompletedTask;
    }

    private async Task XpHandlerAsync(SocketMessage messageParam)
    {
        if (!messageParam.Author.IsBot && !XpCooldowns.Contains(messageParam.Author.Id))
        {
            XpCooldowns.Add(messageParam.Author.Id);
            var info = await DataBaseLoader.GetInfo(messageParam.Author);
            int oldLevel = info.Level();
            info.Xp += PrivateSettings.XpPerMessage;
            DataBaseLoader.AddXp(messageParam.Author, PrivateSettings.XpPerMessage);
            if (info.Level() != oldLevel)
            {
                await messageParam.Channel.SendMessageAsync($"{messageParam.Author.Username} leveled up!");
            }
            await Task.Delay(PrivateSettings.XpCooldown);
            XpCooldowns.Remove(messageParam.Author.Id);

        }
    }

    private Task KarmaHandler(SocketMessage messageParam)
    {
        KarmaHandlerAsync(messageParam);
        return Task.CompletedTask;
    }
    private async Task KarmaHandlerAsync(SocketMessage messageParam)
    {
        if (!messageParam.Author.IsBot && messageParam.MentionedUsers.Count > 0)
        {
            string lower = messageParam.Content.ToLower();
            List<SocketUser> users = messageParam.MentionedUsers.ToList();
            if (PrivateSettings.KarmaTriggers.Any(x => lower.Contains(x)))
            {
                if (users.Any(x => x.Id == messageParam.Author.Id))
                {
                    await messageParam.Channel.SendMessageAsync(":expressionless:").DeleteAfterSeconds(30);
                }
                else if (KarmaCooldowns.Contains(messageParam.Author.Id))
                {
                    await messageParam.Channel.SendMessageAsync($"You have to wait {PrivateSettings.KarmaCooldown / 1000} seconds before giving another karma.");
                }
                else
                {
                    users.RemoveAll(x => x.IsBot);
                    if (users.Count == 0)
                    {
                        await messageParam.Channel.SendMessageAsync("Bots need no Karma...").DeleteAfterSeconds(20);
                        return;
                    }
                    KarmaCooldowns.Add(messageParam.Author.Id);
                    DataBaseLoader.AddKarma(users, 1);
                    StringBuilder builder = new StringBuilder();
                    builder.Append($"{messageParam.Author.Username} gave karma to ");


                    builder.Append($"{users[0]}");
                    for (int i = 1; i < users.Count - 1; i++)
                    {
                        builder.Append($", {users[i]}");
                    }
                    if (users.Count > 1) builder.Append($" and {users[users.Count - 1]}");
                    builder.Append(".");
                    await messageParam.Channel.SendMessageAsync(builder.ToString()).DeleteAfterSeconds(20);
                    await Task.Delay(PrivateSettings.KarmaCooldown);
                    KarmaCooldowns.Remove(messageParam.Author.Id);
                }
            }
        }
    }

    Task UserLeft(SocketGuildUser arg)
    {
        UserLeftAsync(arg);
        return Task.CompletedTask;
    }

    private async Task UserLeftAsync(SocketGuildUser user)
    {

        await LogChannel.SendMessageAsync($"{user.Mention} left after {DateTime.Now - user.JoinedAt}");
    }

    public async Task Welcome(SocketGuildUser user) =>
        await WelcomeChannel.SendMessageAsync(string.Format(PrivateSettings.WelcomeString, user.Mention));

    public Task CheckForMuteEvasion(SocketGuildUser user)
    {
        CheckForMuteEvasionAsync(user);
        return Task.CompletedTask;
    }

    public async Task CheckForMuteEvasionAsync(SocketGuildUser user)
    {
        if (Muting.MutedUsers.ContainsKey(user.Id))
        {
            await user.AddRoleAsync(Muting.Role);
            await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync("I said please :sadface:");
            await LogChannel.SendMessageAsync($"{user.Mention} tried to evade mute.");
            //await EvaluationBot.Commands.CommandsModule.Mute(user, (uint)Muting.MutedUsers[user.Id].TotalSeconds, "Attempting to go around mute");
        }
    }

    public Task HandleCommand(SocketMessage messageParam)
    {
        HandleCommandAsync(messageParam);
        return Task.CompletedTask;
    }

    public async Task HandleCommandAsync(SocketMessage messageParam)
    {
        //copy pasted from 2Bdroid:
        // Don't process the command if it was a System Message
        if (!(messageParam is SocketUserMessage message))
            return;
        // Create a number to track where the prefix ends and the command begins
        int argPos = 0;
        //char prefix = PrivateSettings.Prefix;
        // Determine if the message is a command, based on if it starts with '!' or a mention prefix
        if (!(message.HasStringPrefix(PrivateSettings.Prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos)))
            return;
        // Create a Command Context
        CommandContext context = new CommandContext(_client, message);
        // Execute the command. (result does not indicate a return value,
        // rather an object stating if the command executed successfully)
        IResult result = await commandService.ExecuteAsync(context, argPos);
        if (!result.IsSuccess)
        {

            IUserMessage m = await context.Channel.SendMessageAsync(result.ErrorReason);

            await Task.Delay(10000).ContinueWith(t => m.DeleteAsync());

        }
    }

    private Task Log(LogMessage message)
    {
        Console.WriteLine(message.ToString());
        return Task.CompletedTask;
    }

    public static void DeserializeSettings()
    {
        using (var file = File.OpenText(@"C:\Users\rafae\source\repos\EvaluationBot\EvaluationBot\Serialization\PrivateSettings.json"))
        {
            PrivateSettings = JsonConvert.DeserializeObject<PrivateSettings>(file.ReadToEnd());


        }
    }
}
