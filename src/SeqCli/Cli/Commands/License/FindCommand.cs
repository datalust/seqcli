using System;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.License
{
    [Command("licence", "find", "Retrieve current license key",
        Example = "seqcli licence find")]
    class FindCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;

        readonly ConnectionFeature _connection;
        readonly OutputFormatFeature _output;

        public FindCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
           
            _connection = Enable<ConnectionFeature>();
            _output = Enable(new OutputFormatFeature(config.Output));
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            var licenceEntity = await connection.Licenses.FindCurrentAsync();

            _output.WriteEntity(licenceEntity);
            
            return 0;
        }
    }
}