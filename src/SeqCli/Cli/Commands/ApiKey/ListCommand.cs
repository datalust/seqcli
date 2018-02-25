using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using Serilog;

namespace SeqCli.Cli.Commands.ApiKey
{
    [Command("apikey", "list", "Send a structured log event to the server", Example =
        "seqcli log -m 'Hello, {Name}!' -p Name=World -p App=Test")]
    class ListCommand : Command
    {
        private readonly SeqConnectionFactory _connectionFactory;
        private readonly ConnectionFeature _connection;

        public ListCommand(SeqConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            var apiKeys = await connection.ApiKeys.ListAsync();
            Log.Debug("Retrieved ApiKeys {@ApiKeys}", apiKeys);
            var data = apiKeys.Select(a => new
            {
                a.AppliedProperties,
                a.CanActAsPrincipal,
                a.InputFilter,
                a.Title,
                a.Token,
                a.UseServerTimestamps,
                a.MinimumLevel,
                a.IsDefault
            });
            foreach (var apiKey in data)
            {
                var apiKeyString = JsonConvert.SerializeObject(apiKey);

                Console.WriteLine(apiKeyString);
            }
            return 0;
        }
    }
}
