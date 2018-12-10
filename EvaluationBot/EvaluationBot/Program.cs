using Discord;
using System;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Discord.Commands;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using EvaluationBot.CommandServices;
using EvaluationBot.Extensions;
using EvaluationBot.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace EvaluationBot
{
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
        private Services mainServices;
        private IServiceProvider services;
        private IServiceCollection collection;

        //Misc
        private bool initialized = false;

        //Program entry point, also starts the main method.
        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            //Load the settings
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

            await Client.SetGameAsync("Use !help");

            Guild = Client.GetGuild(PrivateSettings.ServerId);

            LogChannel = Guild.GetTextChannel(PrivateSettings.LogChannel);
            WelcomeChannel = Guild.GetTextChannel(PrivateSettings.WelcomeChannel);
            IntrosChannel = Guild.GetTextChannel(PrivateSettings.IntrosChannel);
            CommandsChannel = Guild.GetTextChannel(PrivateSettings.CommandsChannel);

            //Add the Services class to the list of services that can be pulled by different Modules
            mainServices = new Services(commandService);
            collection = new ServiceCollection().AddSingleton<Services>(mainServices);
            services = collection.BuildServiceProvider();

            //Load database, with hardcoded name.
            mainServices.databaseLoader.LoadDatabase();

            mainServices.databaseLoader.ReloadTimedActions();

            initialized = true;

            //Wait 0, this does nothing but remove a warning.
            await Task.Delay(0);
        }

        public async Task InstallCommands()
        {
            Client.MessageReceived += CommandHandle;
            Client.MessageUpdated += IntroductionEditHandle;

            //Fix below, joined handle should be 1, not 2 methods.
            Client.UserLeft += UserLeft_OnUserLeft;
            Client.UserJoined += Welcome_OnUserJoined;
            Client.UserJoined += CheckForMuteEvasion_OnUserJoined;

            await commandService.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public static async Task Log(LogMessage message)
        {
            //Store the previous color just incase.
            ConsoleColor beforeColor = Console.ForegroundColor;

            //Based on the severity, change the message color.
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

            //Write out a line which looks like this:
            //16:32:19 [Debug] Program: Loading files...
            Console.WriteLine($"{DateTime.Now} [{message.Severity}] {message.Source}: {message.Message}");

            //Change the color back
            Console.ForegroundColor = beforeColor;

            await Task.Delay(0);
        }

        private void DeserializeSettings()
        {
            //Get the settings from a local location and deserialize.
            string fileContents = File.ReadAllText("PrivateSettings.json");

            PrivateSettings = JsonConvert.DeserializeObject<PrivateSettings>(fileContents);
        }

        //WARNING: Passing this point there is no guarentee of comments, and all of the below code
        //will most likely be changed in the future. Watch your step! ~djgaven588

        private async Task UserLeft_OnUserLeft(SocketGuildUser user)
        {
            await LogChannel.SendMessageAsync($"{user.Mention} left after {(DateTime.Now - user.JoinedAt).Value.ToString()}");
        }

        private async Task Welcome_OnUserJoined(SocketGuildUser user) =>
            await WelcomeChannel.SendMessageAsync(string.Format(PrivateSettings.WelcomeString, user.Mention));

        private async Task CheckForMuteEvasion_OnUserJoined(SocketGuildUser user)
        {
            if (mainServices.time.MutedUsers.ContainsKey(user.Id))
            {
                await user.AddRoleAsync(mainServices.time.role);
                await (await user.GetOrCreateDMChannelAsync()).SendMessageAsync("I said please :disappointed_relieved:");
                await LogChannel.SendMessageAsync($"{user.Mention} tried to evade mute.");
                //await EvaluationBot.Commands.CommandsModule.Mute(user, (uint)Muting.MutedUsers[user.Id].TotalSeconds, "Attempting to go around mute");
            }
        }

        private async Task CommandHandle(SocketMessage messageParam)
        {
            ProcessCommand(messageParam);
        }

        private async Task ProcessCommand(SocketMessage messageParam)
        {
            if (messageParam.Author.IsBot)
            {
                return;
            }

            //Are we able to do XP for this message?
            if (!messageParam.Author.IsBot && !XpCooldowns.Contains(messageParam.Author.Id))
            {
                XpHandler(messageParam);
            }

            //Are we able to do karma for this message?
            if (!messageParam.Author.IsBot && messageParam.MentionedUsers.Count > 0)
            {
                KarmaHandlerAsync(messageParam);
            }

            //Is this an introduction message?
            if (!messageParam.Author.IsBot && messageParam.Channel.Id == PrivateSettings.IntrosChannel)
            {
                HandleIntroductionsMessage(messageParam);
                return;
            }

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
            IResult result = await commandService.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
            {

                IUserMessage m = await context.Channel.SendMessageAsync(result.ErrorReason);
                m.DeleteAfterSeconds(10);
                messageParam.DeleteAfterSeconds(20);
            }
        }

        private Task IntroductionEditHandle(Cacheable<IMessage, ulong> arg1, SocketMessage msg, ISocketMessageChannel arg3)
        {
            if (msg != null && !msg.Author.IsBot && arg3.Id == PrivateSettings.IntrosChannel)
            {
                HandleIntroductionsMessage(msg);
            }

            return Task.CompletedTask;
        }

        private async Task HandleIntroductionsMessage(SocketMessage msg)
        {
            mainServices.databaseLoader.SetIntro(msg.Author, msg.Id);
            await msg.Channel.SendMessageAsync($"{msg.Author} I've updated your intro :thumbsup:").DeleteAfterSeconds(15);
        }


        private HashSet<ulong> XpCooldowns = new HashSet<ulong>();

        private HashSet<ulong> KarmaCooldowns = new HashSet<ulong>();

        private async Task XpHandler(SocketMessage messageParam)
        {
            XpCooldowns.Add(messageParam.Author.Id);
            var info = await mainServices.databaseLoader.GetInfo(messageParam.Author);
            int oldLevel = info.Level();

            info.Xp += PrivateSettings.XpPerMessage;
            mainServices.databaseLoader.AddXp(messageParam.Author, PrivateSettings.XpPerMessage);

            if (info.Level() != oldLevel)
            {
                await messageParam.Channel.SendMessageAsync($"{messageParam.Author.Username} leveled up!").DeleteAfterSeconds(20);
            }

            await Task.Delay(PrivateSettings.XpCooldown);
            XpCooldowns.Remove(messageParam.Author.Id);
        }

        private async Task KarmaHandlerAsync(SocketMessage messageParam)
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

                    mainServices.databaseLoader.AddKarma(users, 1);

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
}
