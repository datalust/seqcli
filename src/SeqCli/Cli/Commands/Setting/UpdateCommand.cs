using System;
using System.Diagnostics;
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
    [Command("setting", "update", "Create an API key for ingestion",
        Example = "seqcli apikey create -t 'Test API Key' -p Environment=Test")]
    class UpdateCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;

        readonly ConnectionFeature _connection;
        readonly OutputFormatFeature _output;

        private string _name, _value;

        public UpdateCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            Options.Add(
                "n=|name=",
                "The name of the setting you want to update",
                t => _name = ArgumentString.Normalize(t));

            Options.Add(
                "v=|value=",
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
            settingEntity.Value = _value;
            
            await connection.Settings.UpdateAsync(settingEntity);

            return 0;
        }
    }
}