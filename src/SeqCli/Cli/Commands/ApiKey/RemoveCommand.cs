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
    [Command("apikey", "remove", "Remove API Key from the server", Example =
        "seqcli apikey remove -t TestApiKey")]
    class RemoveCommand : Command
    {
        private readonly SeqConnectionFactory _connectionFactory;
        private readonly ConnectionFeature _connection;
        private string _title;
        private string _id;

        public RemoveCommand(SeqConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _connection = Enable<ConnectionFeature>();
            Options.Add(
                "t=|title=",
                "Remove API Keys with the specified title",
                (t) => _title = t);

            Options.Add(
                "i=|id=",
                "Remove API Keys with the specified Id",
                (t) => _id = t);
        }

        protected override async Task<int> Run()
        {
            if (_title != default && _id != default)
            {
                Console.WriteLine("You can only specify \"title\" or \"id\" not both");
                return -1;
            }

            var connection = _connectionFactory.Connect(_connection);

            var apiKeys = await connection.ApiKeys.ListAsync();
            var apiKeyToRemove = apiKeys.Where(ak => ak.Title == _title || ak.Id == _id).ToList();
            if (!apiKeyToRemove.Any())
            {
                Console.WriteLine($"\"{_title}\" API Key doesn't exist");
                return -1;
            }

            foreach (var apiKeyEntity in apiKeyToRemove)
            {
                await connection.ApiKeys.RemoveAsync(apiKeyEntity);
            }
            return 0;
        }
    }
}
