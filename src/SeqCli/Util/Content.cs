using System.IO;

namespace SeqCli.Util;

static class Content
{
    public static string GetPath(string relativePath)
    {
        var thisDir = Path.GetDirectoryName(Path.GetFullPath(typeof(Content).Assembly.Location)) ?? ".";
        return Path.Combine(thisDir, relativePath);
    }
}