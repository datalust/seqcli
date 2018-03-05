using System;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using Serilog;

namespace SeqCli.Cli.Commands.ApiKey
{
    [Command("apikey", "list", "List API keys on the server", Example="seqcli apikey list")]
    class ListCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;
        
        readonly ConnectionFeature _connection;
        readonly OutputFormatFeature _output;

        public ListCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            _output = Enable(new OutputFormatFeature(config.Output));
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            var apiKeys = await connection.ApiKeys.ListAsync();
            Log.Debug("Retrieved ApiKeys {@ApiKeys}", apiKeys);

            foreach (var apiKey in apiKeys)
            {
                _output.WriteEntity(apiKey);
            }
            
            return 0;
        }
    }
}
