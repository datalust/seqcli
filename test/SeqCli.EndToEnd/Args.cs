using System.Linq;
using System.Text.RegularExpressions;

namespace SeqCli.EndToEnd
{
    class Args
    {
        readonly string[] _args;

        public Args(params string[] args)
        {
            _args = args;
        }
        
        public Regex[] TestCases() => _args
            .Where(arg => !arg.StartsWith("--"))
            .Select(ToArgRegex)
            .ToArray();

        // Simple replacement so `Events.*` becomes `Events\..*`
        static Regex ToArgRegex(string arg) => new Regex(arg.Replace(".", "\\.").Replace("*", ".*"));
    }
}
