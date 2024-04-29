using System;
using System.Collections.Generic;
using System.IO;

namespace SeqCli.EndToEnd.Support;

public class TestConfiguration(Args args)
{
    static int ServerListenPort => 9989;

#pragma warning disable CA1822
    public string ServerListenUrl => $"http://localhost:{ServerListenPort}";
#pragma warning restore CA1822

    string EquivalentBaseDirectory { get; } = AppDomain.CurrentDomain.BaseDirectory
        .Replace(Path.Combine("test", "SeqCli.EndToEnd"), Path.Combine("src", "SeqCli"));

    public string TestedBinary => Path.Combine(EquivalentBaseDirectory, "seqcli.dll");

    public bool IsMultiuser => args.Multiuser();

    public CaptiveProcess SpawnCliProcess(string command, string additionalArgs = null, Dictionary<string, string> environment = null, bool skipServerArg = false)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var commandWithArgs = $"{command} {additionalArgs}";
        if (!skipServerArg)
            commandWithArgs += $" --server=\"{ServerListenUrl}\"";
            
        return new CaptiveProcess("dotnet", $"{TestedBinary} {commandWithArgs}", environment);
    }
        
    public CaptiveProcess SpawnServerProcess(string storagePath)
    {
        if (storagePath == null) throw new ArgumentNullException(nameof(storagePath));

        var commandWithArgs = $"run --listen=\"{ServerListenUrl}\" --storage=\"{storagePath}\"";
        if (args.UseDockerSeq(out var imageTag))
        {
            var containerName = Guid.NewGuid().ToString("n");
            return new CaptiveProcess("docker", $"run --name {containerName} -it --rm -e ACCEPT_EULA=Y -p {ServerListenPort}:80 datalust/seq:{imageTag}", stopCommandFullExePath: "docker", stopCommandArgs: $"stop {containerName}");
        }
        
        return new CaptiveProcess("seq", commandWithArgs);
    }
}
