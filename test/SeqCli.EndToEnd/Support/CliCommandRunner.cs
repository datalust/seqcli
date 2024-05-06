using System;

#nullable enable

namespace SeqCli.EndToEnd.Support;

public class CliCommandRunner(TestConfiguration configuration)
{
    public static readonly TimeSpan DefaultExecTimeout = TimeSpan.FromSeconds(10);
        
    public ITestProcess? LastRunProcess { get; private set; }

    public int Exec(string command, string? args = null, bool disconnected = false)
    {
        using var process = configuration.SpawnCliProcess(command, args, skipServerArg: disconnected);
        LastRunProcess = process;
        return process.WaitForExit(DefaultExecTimeout);
    }
}