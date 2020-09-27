using System;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.License
{
    [Command("licence", "update", "Update license key",
        Example = "cat license.txt | seqcli licence update")]
    class UpdateCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;

        readonly FileInputFeature _fileInputFeature;
        readonly ConnectionFeature _connection;
        
        public UpdateCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            _fileInputFeature = Enable(new FileInputFeature("License file to import"));
            
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            var current = await connection.Licenses.FindCurrentAsync();
            using var input = _fileInputFeature.OpenInput();
            current.LicenseText = await input.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(current.LicenseText))
            {
                await connection.Licenses.UpdateAsync(current);
            }

            return 0;
        }
    }
}