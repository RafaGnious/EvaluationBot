using Discord.Commands;
using EvaluationBot.Data;
using System;
using System.Collections.Generic;

namespace EvaluationBot.CommandServices
{
    public class Services
    {
        //This class is used for operations outside of command modules, this should contain everything required for a running command.
        //This includes most static variables in "Program".

#pragma warning disable IDE1006 // Naming Styles
        public DataBaseLoader databaseLoader { get; private set; }
        public Random random { get; private set; }
        public Timing time { get; private set; }
        public CommandService commandService { get; private set; }
#pragma warning restore IDE1006 // Naming Styles

        public Services(CommandService commandService)
        {
            this.commandService = commandService;
            databaseLoader = new DataBaseLoader(this);
            time = new Timing(this);

            random = new Random();
        }

        public IEnumerable<ModuleInfo> GetModules()
        {
            return commandService.Modules;
        }
    }
}
