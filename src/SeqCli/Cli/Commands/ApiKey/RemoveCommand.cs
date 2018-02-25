using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Seq.Api.Model.Inputs;
using SeqCli.Cli.Features;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.ApiKey
{
    [Command("apikey", "remove", "Send a structured log event to the server", Example =
        "seqcli log -m 'Hello, {Name}!' -p Name=World -p App=Test")]
    class RemoveCommand : Command
    {
        private readonly SeqConnectionFactory _connectionFactory;
        private readonly ConnectionFeature _connection;
        private string _title;

        public RemoveCommand(SeqConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _connection = Enable<ConnectionFeature>();
            Options.Add(
                "t=|title=",
                "",
                (t) => _title = t);
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            var apiKeys = await connection.ApiKeys.ListAsync();
            var apiKeyToRemove = apiKeys.FirstOrDefault(ak => ak.Title == _title);
            if (apiKeyToRemove == null)
            {
                Console.WriteLine($"\"{_title}\" API Key doesn't exist");
                return -1;
            }

            await connection.ApiKeys.RemoveAsync(apiKeyToRemove);
            Console.WriteLine($"\"{_title}\" API Key removed");
            return 0;
        }
    }
}
