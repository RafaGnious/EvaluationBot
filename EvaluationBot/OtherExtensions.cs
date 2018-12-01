using System;
using System.Collections.Generic;
using System.Text;

namespace EvaluationBot
{
    public static class OtherExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
