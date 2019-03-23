using System.Collections;
using System.Collections.Generic;

namespace EvaluationBot.Extensions
{
    public static class OtherExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }

    public struct ParsingSyntax
    {
        public List<string> IntermediateStrings;
        public bool CaseSensitive;
    }
}

