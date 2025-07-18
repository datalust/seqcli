using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SeqCli.EndToEnd.Support;

public class TestConfiguration(Args args)
{
    static int _nextServerPort = 9989;
    readonly int _serverListenPort = Interlocked.Increment(ref _nextServerPort);

#pragma warning disable CA1822
    public string ServerListenUrl => $"http://localhost:{_serverListenPort}";
#pragma warning restore CA1822

    static string EquivalentBaseDirectory { get; } = AppDomain.CurrentDomain.BaseDirectory
        .Replace(Path.Combine("test", "SeqCli.EndToEnd"), Path.Combine("src", "SeqCli"));

    public static string TestedBinary => Path.Combine(EquivalentBaseDirectory, "seqcli.dll");

    public bool IsMultiuser => args.Multiuser();

    public CaptiveProcess SpawnCliProcess(string command, string additionalArgs = null, Dictionary<string, string> environment = null, bool skipServerArg = false, bool supplyInput = false)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var commandWithArgs = $"{command} {additionalArgs}";
        if (!skipServerArg)
            commandWithArgs += $" --server=\"{ServerListenUrl}\"";
            
        return new CaptiveProcess("dotnet", $"{TestedBinary} {commandWithArgs}", environment, supplyInput: supplyInput);
    }
        
    public CaptiveProcess SpawnServerProcess(string storagePath)
    {
        if (storagePath == null) throw new ArgumentNullException(nameof(storagePath));

        var commandWithArgs = $"run --listen=\"{ServerListenUrl}\" --storage=\"{storagePath}\"";
        if (args.UseDockerSeq(out var imageTag))
        {
            var containerName = Guid.NewGuid().ToString("n");
            const string containerRuntime = "docker";
            return new CaptiveProcess(containerRuntime, $"run --name {containerName} -d -e ACCEPT_EULA=Y SEQ_FIRSTRUN_NOAUTHENTICATION -p {_serverListenPort}:80 datalust/seq:{imageTag}", stopCommandFullExePath: containerRuntime, stopCommandArgs: $"rm -f {containerName}");
        }
        
        return new CaptiveProcess("seq", commandWithArgs);
    }
}
