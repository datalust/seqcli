using System;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.ExpressionIndex;

[Command("expressionindex", "list", "List expression indexes", Example="seqcli expressionindex list")]
class ListCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;
        
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    string? _id;

    public ListCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            
        Options.Add(
            "i=|id=",
            "The id of a single expression index to list",
            id => _id = id);
        
        _output = Enable(new OutputFormatFeature(config.Output));
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        var connection = _connectionFactory.Connect(_connection);
        var list = _id is not null 
            ? [await connection.ExpressionIndexes.FindAsync(_id)]
            : await connection.ExpressionIndexes.ListAsync();
        _output.ListEntities(list);
        return 0;
    }
}