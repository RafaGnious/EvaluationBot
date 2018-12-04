using EvaluationBot.Data;
using System;

namespace EvaluationBot.CommandServices
{
    public class Services
    {
        //This class is used for operations outside of command modules, this should contain everything required for a running command.
        //This includes most static variables in "Program".

#pragma warning disable IDE1006 // Naming Styles
        public DataBaseLoader databaseLoader { get; private set; }
        public Random random { get; private set; }
        public Muting silence { get; private set; }
#pragma warning restore IDE1006 // Naming Styles

        public Services()
        {
            this.databaseLoader = new DataBaseLoader(this);
            this.silence = new Muting(this);

            random = new Random();
        }
    }
}
