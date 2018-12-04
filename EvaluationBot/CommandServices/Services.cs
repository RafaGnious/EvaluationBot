using EvaluationBot.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace EvaluationBot.CommandServices
{
    public class Services
    {
        //This class is used for operations outside of command modules, this should contain everything required for a running command.
        //This includes most static variables in "Program".

        public DataBaseLoader databaseLoader { get; private set; }
        public Random random { get; private set; }

        public Services(DataBaseLoader dataBaseLoader)
        {
            this.databaseLoader = databaseLoader;

            random = new Random();
        }
    }
}
