using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace EvaluationBot
{
    class RequireUserId : PreconditionAttribute
    {
        public ulong id;
        public RequireUserId(ulong id)
        {
            this.id = id;
        }

        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.User.Id == id)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            {
                return Task.FromResult(PreconditionResult.FromError($"That command can only be used by one specific user"));
            }
        }
    }
}
