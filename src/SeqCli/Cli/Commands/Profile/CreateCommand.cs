using System;
using System.Threading.Tasks;
using SeqCli.Config;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.Profile
{
    [Command("profile", "create", "Create or replace a connection profile",
        Example = "seqcli profile create -n Production -s https://seq.example.com -a th15ISanAPIk3y")]
    class CreateCommand : Command
    {
        string _url, _apiKey, _name;

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
                var config = SeqCliConfig.Read();
                config.Profiles[_name] = new SeqCliConnectionConfig { ServerUrl = _url, ApiKey = _apiKey };
                SeqCliConfig.Write(config);
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error("Could not create profile: {ErrorMessage}", Presentation.FormattedMessage(ex));
                return 1;
            }
        }
    }
}
