using System;
using System.Collections.Generic;
using SeqCli.Mcp;
using Xunit;

namespace SeqCli.Tests.Mcp;

public class McpServerInstallerTests
{
    // Candidate paths are built with Path.Combine, which uses the host's separator; normalize so the
    // Windows-style expectations hold when the tests run on Linux or macOS.
    static Func<string, bool> FileSystemWith(params string[] files)
    {
        var set = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
        return candidate => set.Contains(candidate.Replace('/', '\\'));
    }

    [Fact]
    public void OnNonWindowsPlatformsSeqCliIsLaunchedDirectly()
    {
        var (command, leadingArgs) = McpServerInstaller.ResolveCommand(
            false, "/usr/local/bin:/usr/bin", null, _ => true);

        Assert.Equal("seqcli", command);
        Assert.Empty(leadingArgs);
    }

    [Fact]
    public void OnWindowsAnExecutableOnPathIsLaunchedDirectly()
    {
        var (command, leadingArgs) = McpServerInstaller.ResolveCommand(
            true,
            @"C:\Program Files\Seq;C:\Users\me\AppData\Roaming\npm",
            ".COM;.EXE;.BAT;.CMD",
            FileSystemWith(@"C:\Program Files\Seq\seqcli.exe", @"C:\Users\me\AppData\Roaming\npm\seqcli.cmd"));

        Assert.Equal("seqcli", command);
        Assert.Empty(leadingArgs);
    }

    [Fact]
    public void OnWindowsAnNpmShimOnPathIsLaunchedViaCmd()
    {
        var (command, leadingArgs) = McpServerInstaller.ResolveCommand(
            true,
            @"C:\Users\me\AppData\Roaming\npm;C:\Program Files\Seq",
            ".COM;.EXE;.BAT;.CMD",
            FileSystemWith(@"C:\Users\me\AppData\Roaming\npm\seqcli.cmd", @"C:\Program Files\Seq\seqcli.exe"));

        Assert.Equal("cmd", command);
        Assert.Equal(["/c", "seqcli"], leadingArgs);
    }

    [Fact]
    public void OnWindowsWhenSeqCliIsNotOnPathItIsLaunchedDirectly()
    {
        var (command, leadingArgs) = McpServerInstaller.ResolveCommand(
            true, @"C:\Windows\system32", null, _ => false);

        Assert.Equal("seqcli", command);
        Assert.Empty(leadingArgs);
    }

    [Fact]
    public void PathExtDefaultsAreUsedWhenTheVariableIsMissing()
    {
        var (command, _) = McpServerInstaller.ResolveCommand(
            true,
            @"C:\Users\me\AppData\Roaming\npm",
            null,
            FileSystemWith(@"C:\Users\me\AppData\Roaming\npm\seqcli.cmd"));

        Assert.Equal("cmd", command);
    }
}
