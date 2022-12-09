using System.Collections.Generic;

namespace SeqCli.PlainText
{
    static class ReifiedProperties
    {
        public const string
            Message = "@m",
            Timestamp = "@t",
            Level = "@l",
            Exception = "@x";
        
        static readonly HashSet<string> All = new()
        {
            Message, Timestamp, Level, Exception
        };

        public static bool IsReifiedProperty(string name)
        {
            return All.Contains(name);
        }
    }
}
