using System;
using System.IO;

namespace SeqCli.Util;

static class Content
{
    public static string GetPath(string relativePath)
    {
        var thisDir = Path.GetDirectoryName(Path.GetFullPath(AppContext.BaseDirectory)) ?? ".";
        return Path.Combine(thisDir, relativePath);
    }
}