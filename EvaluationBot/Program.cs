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
using EvaluationBot.CommandServices;

public class Program
{
    /*
        If your wondering about a large amount of the comments here, they 
        were made by 'djgaven588' on the Evalutation Station Discord.

        You will thank me later.
    */

    //Settings
    public static PrivateSettings PrivateSettings;

    //Channels
    public static ISocketMessageChannel LogChannel;
    public static ISocketMessageChannel IntrosChannel;
    public static ISocketMessageChannel CommandsChannel;
    public static ISocketMessageChannel WelcomeChannel;

    //Discord
    public static DiscordSocketClient Client;
    public static SocketGuild Guild;

    //Commands
    private CommandService commandService;
    private Services services;

    //Misc
    public static string Help;
    private bool initialized = false;

    //Program entry point, also starts the main method.
    static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

    public async Task MainAsync()
    {
        //Load private settings from a hard coded location?
        DeserializeSettings();

        //Setup client
        Client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info
        });

        //Setup command service
        commandService = new CommandService(new CommandServiceConfig
        {
            CaseSensitiveCommands = false,
            DefaultRunMode = RunMode.Async
        });

        //Create new services instance, this is used in commands mainly, but is sometimes used in other places.
        services = new Services(new DataBaseLoader());

        //Load database, with hardcoded name.
        services.databaseLoader.LoadDatabase();

        //Setup logging
        Client.Log += Log;

        //Login, and start bot functionality.
        await Client.LoginAsync(TokenType.Bot, PrivateSettings.Token);
        await Client.StartAsync();

        Client.Ready += ClientReady_Callback;

        await InstallCommands();

        //Wait forever, prevents bot from closing
        await Task.Delay(-1);
    }

    private async Task ClientReady_Callback()
    {
        //If we already initialized, don't do anything.
        //Otherwise, do initialization things.
        if (initialized)
            return;

        Guild = Client.GetGuild(PrivateSettings.ServerId);

        LogChannel = Guild.GetTextChannel(PrivateSettings.LogChannel);
        WelcomeChannel = Guild.GetTextChannel(PrivateSettings.WelcomeChannel);
        IntrosChannel = Guild.GetTextChannel(PrivateSettings.IntrosChannel);
        CommandsChannel = Guild.GetTextChannel(PrivateSettings.CommandsChannel);

        services.databaseLoader.ReloadTimedActions();

        initialized = true;

        //Wait 0, this does nothing but remove a warning.
        await Task.Delay(0);
    }

    public async Task InstallCommands()
    {
        //All 'TODO: FIX THIS' comments are from djgaven588
        //This is just... TODO: FIX THIS
        Client.MessageReceived += HandleCommand_OnMessageReceived;
        Client.MessageReceived += XpHandler_OnMessageReceived;
        Client.MessageReceived += KarmaHandler_OnMessageReceived;
        Client.MessageReceived += Introductions_OnMessageReceived;
        Client.MessageUpdated += IntroductionsEdit_OnMessageUpdated;

        Client.UserLeft += UserLeft_OnUserLeft;
        Client.UserJoined += Welcome_OnUserJoined;
        Client.UserJoined += CheckForMuteEvasion_OnUserJoined;

        //Find all modules which contain commands in the entire assembly.
        //TODO: FIX THIS, Make this not look at the entire assemble, but find modules.
        await commandService.AddModulesAsync(Assembly.GetEntryAssembly());

        //Make a list of commands
        //TODO: FIX THIS, Commands should be listed by module, and that foreach loop...
        StringBuilder commandList = new StringBuilder();

        commandList.Append("Commands: \n");

        foreach (CommandInfo c in commandService.Commands.Where(x => x.Preconditions.Count == 0).OrderBy(c => c.Name))
        {
            commandList.Append($"**{c.Name}** : {c.Summary}\n");
        }

        Help = commandList.ToString();
    }

    //WARNING: Passing this point there is no guarentee of comments, and all of the below code
    //will most likely be changed in the future. Watch your step! ~djgaven588

    private async Task IntroductionsEdit_OnMessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
    {
        await EditIntroduction(arg1, arg2, arg3);
    }

    private async Task EditIntroduction(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
    {
        if (!arg2.Author.IsBot && arg2.Channel.Id == PrivateSettings.IntrosChannel)
        {
            services.databaseLoader.SetIntro(arg2.Author, arg2.Id);
            await arg2.Channel.SendMessageAsync($"{arg2.Author} I've updated your intro :thumbsup:").DeleteAfterSeconds(15);
        }
    }

    private async Task Introductions_OnMessageReceived(SocketMessage message)
    {
        await IntroductionsAsync(message);
    }

    private async Task IntroductionsAsync(SocketMessage message)
    {
        if (!message.Author.IsBot && message.Channel.Id == PrivateSettings.IntrosChannel)
        {
            services.databaseLoader.SetIntro(message.Author, message.Id);
            await message.Channel.SendMessageAsync($"{message.Author} I've updated your intro :thumbsup:").DeleteAfterSeconds(15);
        }
    }

    private HashSet<ulong> XpCooldowns = new HashSet<ulong>();

    private HashSet<ulong> KarmaCooldowns = new HashSet<ulong>();

    private async Task XpHandler_OnMessageReceived(SocketMessage messageParam)
    {
        await XpHandler(messageParam);
    }

    private async Task XpHandler(SocketMessage messageParam)
    {
        if (!messageParam.Author.IsBot && !XpCooldowns.Contains(messageParam.Author.Id))
        {
            XpCooldowns.Add(messageParam.Author.Id);
            var info = await services.databaseLoader.GetInfo(messageParam.Author);
            int oldLevel = info.Level();
            info.Xp += PrivateSettings.XpPerMessage;
            services.databaseLoader.AddXp(messageParam.Author, PrivateSettings.XpPerMessage);
            if (info.Level() != oldLevel)
            {
                await messageParam.Channel.SendMessageAsync($"{messageParam.Author.Username} leveled up!").DeleteAfterSeconds(20);
            }
            await Task.Delay(PrivateSettings.XpCooldown);
            XpCooldowns.Remove(messageParam.Author.Id);

        }
    }

    private async Task KarmaHandler_OnMessageReceived(SocketMessage messageParam)
    {
        await KarmaHandlerAsync(messageParam);
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

                    services.databaseLoader.AddKarma(users, 1);

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

    private async Task UserLeft_OnUserLeft(SocketGuildUser user)
    {
        await LogChannel.SendMessageAsync($"{user.Mention} left after {DateTime.Now - user.JoinedAt}");
    }

    private async Task Welcome_OnUserJoined(SocketGuildUser user) =>
        await WelcomeChannel.SendMessageAsync(string.Format(PrivateSettings.WelcomeString, user.Mention));

    private async Task CheckForMuteEvasion_OnUserJoined(SocketGuildUser user)
    {
        if (Muting.MutedUsers.ContainsKey(user.Id))
        {
            await user.AddRoleAsync(Muting.Role);
            await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync("I said please :sadface:");
            await LogChannel.SendMessageAsync($"{user.Mention} tried to evade mute.");
            //await EvaluationBot.Commands.CommandsModule.Mute(user, (uint)Muting.MutedUsers[user.Id].TotalSeconds, "Attempting to go around mute");
        }
    }

    private async Task HandleCommand_OnMessageReceived(SocketMessage messageParam)
    {
        await HandleCommandAsync(messageParam);
    }

    private async Task HandleCommandAsync(SocketMessage messageParam)
    {
        //Warning: The following code is a direct copy from 2Bdroid Discord bot

        // Don't process the command if it was a System Message
        if (!(messageParam is SocketUserMessage message))
            return;
        // Create a number to track where the prefix ends and the command begins
        int argPos = 0;
        //char prefix = PrivateSettings.Prefix;
        // Determine if the message is a command, based on if it starts with '!' or a mention prefix
        if (!(message.HasStringPrefix(PrivateSettings.Prefix, ref argPos) || message.HasMentionPrefix(Client.CurrentUser, ref argPos)))
            return;
        // Create a Command Context
        CommandContext context = new CommandContext(Client, message);
        // Execute the command. (result does not indicate a return value,
        // rather an object stating if the command executed successfully)
        IResult result = await commandService.ExecuteAsync(context, argPos);
        if (!result.IsSuccess)
        {

            IUserMessage m = await context.Channel.SendMessageAsync(result.ErrorReason);

            await Task.Delay(10000).ContinueWith(t => m.DeleteAsync());

        }
    }

    private async Task Log(LogMessage message)
    {
        Console.WriteLine(message.ToString());

        ConsoleColor beforeColor = Console.ForegroundColor;
        switch (message.Severity)
        {
            case LogSeverity.Critical:
                Console.ForegroundColor = ConsoleColor.DarkRed;
                break;
            case LogSeverity.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case LogSeverity.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LogSeverity.Info:
                Console.ForegroundColor = ConsoleColor.Green;
                break;
            case LogSeverity.Verbose:
                Console.ForegroundColor = ConsoleColor.White;
                break;
            case LogSeverity.Debug:
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            default:
                break;
        }

        Console.WriteLine($"{DateTime.Now} [{message.Severity}] {message.Source}: {message.Message}");

        Console.ForegroundColor = beforeColor;

        await Task.Delay(0);
    }

    private void DeserializeSettings()
    {
        //NOTE: This should be updated to a local path! It is most likely "Serialization\PrivateSettings.json".
        string fileContents = File.ReadAllText(@"C:\Users\rafae\source\repos\EvaluationBot\EvaluationBot\Serialization\PrivateSettings.json");

        PrivateSettings = JsonConvert.DeserializeObject<PrivateSettings>(fileContents);
    }
}
