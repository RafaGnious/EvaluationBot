using System;
using System.Collections.Generic;
using System.Text;

namespace EvaluationBot.Serialization
{
    public class PrivateSettings
    {
        public string Token { get; set; }
        public ulong ServerId { get; set; }
        public ulong LogChannel { get; set; }
        public string Prefix { get; set; }
        public ulong WelcomeChannel { get; set; }
        public string WelcomeString { get; set; }
        public string DatabaseConnectionString { get; set; }
        public int XpPerMessage { get; set; }
        public int XpCooldown { get; set; }
        public ulong IntrosChannel { get; set; }
        public string[] KarmaTriggers { get; set; }
        public int KarmaCooldown { get; set; }
    }
}
