using System;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using SeqCli.Sample.Loader;

namespace SeqCli.Cli.Commands.Sample
{
    [Command("sample", "ingest", "Log sample events into a Seq instance",
        Example = "seqcli sample ingest")]
    class IngestCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;
        
        readonly ConnectionFeature _connection;
        readonly ConfirmFeature _confirm;

        public IngestCommand(SeqConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _confirm = Enable<ConfirmFeature>();
            _connection = Enable<ConnectionFeature>();
        }
        
        protected override async Task<int> Run()
        {
            var (url, apiKey) = _connectionFactory.GetConnectionDetails(_connection);
            
            if (!_confirm.TryConfirm($"This will send sample events to the Seq server at {url}."))
            {
                await Console.Error.WriteLineAsync("Canceled by user.");
                return 1;
            }

            var connection = _connectionFactory.Connect(_connection);
            await Simulation.RunAsync(connection, apiKey);
            return 0;
        }
    }
}
