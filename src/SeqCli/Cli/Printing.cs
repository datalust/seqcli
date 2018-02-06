using System.IO;
using System.Linq;

namespace SeqCli.Cli
{
    static class Printing
    {
        const int ConsoleWidth = 80;

        public static void Define(string term, string definition, int termColumnWidth, TextWriter output)
        {
            var header = term.PadRight(termColumnWidth);
            var right = ConsoleWidth - header.Length;

            var rest = definition.ToCharArray();
            while (rest.Any())
            {
                var content = new string(rest.Take(right).ToArray());
                if (!string.IsNullOrWhiteSpace(content))
                {
                    output.Write(header);
                    header = new string(' ', header.Length);
                    output.WriteLine(content);
                }
                rest = rest.Skip(right).ToArray();
            }
        }
    }
}
