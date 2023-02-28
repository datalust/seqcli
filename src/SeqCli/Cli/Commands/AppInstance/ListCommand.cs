using System;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.AppInstance;

[Command("appinstance", "list", "List instances of installed apps", Example="seqcli appinstance list")]
class ListCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly EntityIdentityFeature _entityIdentity;
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;

    public ListCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        _entityIdentity = Enable(new EntityIdentityFeature("app instance", "list"));
        _output = Enable(new OutputFormatFeature(config.Output));
        _connection = Enable<ConnectionFeature>();
    }
    
    protected override async Task<int> Run()
    {
        var connection = _connectionFactory.Connect(_connection);

        var list = _entityIdentity.Id != null ?
            new[] { await connection.AppInstances.FindAsync(_entityIdentity.Id) } :
            (await connection.AppInstances.ListAsync())
            .Where(d => _entityIdentity.Title == null || _entityIdentity.Title == d.Title);

        _output.ListEntities(list);

        return 0;
    }
}