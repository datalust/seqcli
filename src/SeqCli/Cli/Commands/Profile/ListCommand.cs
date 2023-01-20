using System;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Config;

namespace SeqCli.Cli.Commands.Profile;

[Command("profile", "list", "List connection profiles",
    Example = "seqcli profile list")]
class ListCommand : Command
{
    protected override Task<int> Run()
    {
        var config = SeqCliConfig.Read();

        foreach (var profile in config.Profiles.OrderBy(p => p.Key))
        {
            Console.WriteLine($"{profile.Key} ({profile.Value.ServerUrl})");
        }
            
        return Task.FromResult(0);
    }
}