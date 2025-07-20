using System;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.ExpressionIndex;

[Command("expressionindex", "list", "List expression indexes", Example="seqcli expressionindex list")]
class ListCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly StoragePathFeature _storagePath;
    
    string? _id;

    public ListCommand()
    {
        Options.Add(
            "i=|id=",
            "The id of a single expression index to list",
            id => _id = id);
        
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);
        var list = _id is not null 
            ? [await connection.ExpressionIndexes.FindAsync(_id)]
            : await connection.ExpressionIndexes.ListAsync();
        _output.GetOutputFormat(config).ListEntities(list);
        return 0;
    }
}