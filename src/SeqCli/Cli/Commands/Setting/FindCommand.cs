using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Seq.Api.Model.Inputs;
using Seq.Api.Model.LogEvents;
using Seq.Api.Model.Settings;
using Seq.Api.Model.Shared;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Levels;
using SeqCli.Util;

namespace SeqCli.Cli.Commands.Setting
{
    [Command("setting", "find", "Create an API key for ingestion",
        Example = "seqcli apikey create -t 'Test API Key' -p Environment=Test")]
    class FindCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;

        readonly ConnectionFeature _connection;
        readonly OutputFormatFeature _output;

        private string _name, _value;

        public FindCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            Options.Add(
                "n=|name=",
                "The name of the setting you want to update",
                t => _name = ArgumentString.Normalize(t));

            Options.Add(
                "value=",
                "A JSON encoded value to be provided",
                t => _value = ArgumentString.Normalize(t));
            
            _connection = Enable<ConnectionFeature>();
            _output = Enable(new OutputFormatFeature(config.Output));
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            var settingName = Enum.Parse<SettingName>(_name);

            var settingEntity = await connection.Settings.FindNamedAsync(settingName);

            Console.Write(JsonConvert.SerializeObject(settingEntity.Value));
            
            return 0;
        }
    }
}