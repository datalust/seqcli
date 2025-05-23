using System;
using System.Threading.Tasks;
using SeqCli.Config;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.Profile;

[Command("profile", "remove", "Remove a connection profile",
    Example = "seqcli profile remove -n Production")]
class RemoveCommand : Command
{
    string? _name;

    public RemoveCommand()
    {
        Options.Add("n=|name=",
            "The name of the connection profile to remove",
            v => _name = ArgumentString.Normalize(v));
    }

    protected override Task<int> Run()
    {
        return Task.FromResult(RunSync());
    }

    int RunSync()
    {
        if (_name == null)
        {
            Log.Error("A profile name is required");
            return 1;
        }

        try
        {
            var config = SeqCliConfig.ReadFromFile(RuntimeConfigurationLoader.DefaultConfigFilename);
            if (!config.Profiles.Remove(_name))
            {
                Log.Error("No profile with name {ProfileName} was found", _name);
                return 1;
            }

            SeqCliConfig.WriteToFile(config, RuntimeConfigurationLoader.DefaultConfigFilename);

            return 0;
        }
        catch (Exception ex)
        {
            Log.Error("Could not create profile: {ErrorMessage}", Presentation.FormattedMessage(ex));
            return 1;
        }
    }
}