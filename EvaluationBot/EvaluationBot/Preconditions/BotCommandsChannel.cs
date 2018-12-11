using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace EvaluationBot
{
    class BotCommandsChannel : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Channel.Id == Program.CommandsChannel.Id)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            {
                return Task.FromResult(PreconditionResult.FromError($"That command can only be used in <#{Program.CommandsChannel.Id}>"));
            }
        }
    }
}
