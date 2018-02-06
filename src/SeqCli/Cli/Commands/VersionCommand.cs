using System;
using System.Reflection;

namespace SeqCli.Cli.Commands
{
    [Command("version", "Print the current executable version")]
    class VersionCommand : Command
    {
        protected override int Run()
        {
            var version = GetVersion();
            Console.WriteLine(version);
            return 0;
        }

        public static string GetVersion()
        {
            return typeof(VersionCommand).GetTypeInfo().Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }
    }
}
