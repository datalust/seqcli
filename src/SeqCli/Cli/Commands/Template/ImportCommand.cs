using System.Collections.Generic;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Connection;

// ReSharper disable once UnusedType.Global

namespace SeqCli.Cli.Commands.Template
{
    [Command("template", "import", "Import entities from template files",
        Example = "seqcli template import -i ./Templates/*.template")]
    class ImportCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;
        readonly ConnectionFeature _connection;

        readonly List<string> _templateFiles = new();
        readonly Dictionary<string, string> _args = new();
        
        public ImportCommand(SeqConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _connection = Enable<ConnectionFeature>();
            
            // Templates
            // Args
            // Should `FileInputFeature` (separately) support multiple -i arguments?
        }

        protected override async Task<int> Run()
        {
            
        }
    }
}