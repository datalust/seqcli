using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

#nullable enable

namespace SeqCli.EndToEnd;

public class Args(params string[] args)
{
    public Regex[] TestCases() => args
        .Where(arg => !arg.StartsWith("--"))
        .Select(ToArgRegex)
        .ToArray();

    // Simple replacement so `Events.*` becomes `Events\..*`
    static Regex ToArgRegex(string arg) => new(arg.Replace(".", "\\.").Replace("*", ".*"));

    public bool Multiuser() => args.Any(a => a == "--license-certificate-stdin");

    public bool UseDockerSeq([NotNullWhen(true)] out string? imageTag)
    {
        if (args.Any(a => a == "--docker-server"))
        {
            imageTag = args.Any(a => a == "--pre") ? "preview" : "latest";
            return true;
        }

        imageTag = null;
        return false;
    }
}
