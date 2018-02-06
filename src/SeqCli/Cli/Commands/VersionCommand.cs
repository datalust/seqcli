using System;
using System.Reflection;
using System.Threading.Tasks;

namespace SeqCli.Cli.Commands
{
    [Command("version", "Print the current executable version")]
    class VersionCommand : Command
    {
        protected override Task<int> Run()
        {
            var version = GetVersion();
            Console.WriteLine(version);
            return Task.FromResult(0);
        }

        public static string GetVersion()
        {
            return typeof(VersionCommand).GetTypeInfo().Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }
    }
}
