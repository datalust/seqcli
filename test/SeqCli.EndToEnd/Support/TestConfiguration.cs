using System;
using System.Collections.Generic;
using System.IO;

namespace SeqCli.EndToEnd.Support
{
    public class TestConfiguration
    {
        public string ServerListenUrl { get; } = "http://127.0.0.1:9989";

        string EquivalentBaseDirectory { get; } = AppDomain.CurrentDomain.BaseDirectory
            .Replace(Path.Combine("test", "SeqCli.EndToEnd"), Path.Combine("src", "SeqCli"));

        public string TestedBinary => Path.Combine(EquivalentBaseDirectory, "seqcli.dll");

        public CaptiveProcess SpawnCliProcess(string command, string additionalArgs = null, Dictionary<string, string> environment = null, bool skipServerArg = false)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            var commandWithArgs = $"{command} {additionalArgs}";
            if (!skipServerArg)
                commandWithArgs += $" --server=\"{ServerListenUrl}\"";
            
            var args = $"{TestedBinary} {commandWithArgs}";
            return new CaptiveProcess("dotnet", args, environment);
        }
        
        public CaptiveProcess SpawnServerProcess(string storagePath)
        {
            if (storagePath == null) throw new ArgumentNullException(nameof(storagePath));

            var commandWithArgs = $"run --listen=\"{ServerListenUrl}\" --storage=\"{storagePath}\"";
            
            return new CaptiveProcess("seq", commandWithArgs);
        }
    }
}
