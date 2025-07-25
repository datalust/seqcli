using System;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.Profile;

[Command("profile", "create", "Create or replace a connection profile",
    Example = "seqcli profile create -n Production -s https://seq.example.com -a th15ISanAPIk3y")]
class CreateCommand : Command
{
    string? _url, _apiKey, _name;
    readonly StoragePathFeature _storagePath;
    
    public CreateCommand()
    {
        Options.Add("n=|name=",
            "The name of the connection profile",
            v => _name = ArgumentString.Normalize(v));

        Options.Add("s=|server=",
            "The URL of the Seq server",
            v => _url = ArgumentString.Normalize(v));

        Options.Add("a=|apikey=",
            "The API key to use when connecting to the server, if required",
            v => _apiKey = ArgumentString.Normalize(v));

        _storagePath = Enable<StoragePathFeature>();
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

        if (_url == null)
        {
            Log.Error("A server URL is required");
            return 1;
        }
            
        try
        {
            var config = SeqCliConfig.ReadFromFile(_storagePath.ConfigFilePath);
            var connectionConfig = new SeqCliConnectionConfig { ServerUrl = _url };
            connectionConfig.EncodeApiKey(_apiKey, config.Encryption.DataProtector());
            config.Profiles[_name] = connectionConfig;
            SeqCliConfig.WriteToFile(config, _storagePath.ConfigFilePath);
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error("Could not create profile: {ErrorMessage}", Presentation.FormattedMessage(ex));
            return 1;
        }
    }
}