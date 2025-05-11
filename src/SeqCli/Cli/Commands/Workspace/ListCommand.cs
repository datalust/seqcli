using System;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.Workspace;

[Command("workspace", "list", "List available workspaces", Example = "seqcli workspace list")]
class ListCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly EntityIdentityFeature _entityIdentity;
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly EntityOwnerFeature _entityOwner;

    public ListCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        _entityIdentity = Enable(new EntityIdentityFeature("workspace", "list"));
        _entityOwner = Enable(new EntityOwnerFeature("workspace", "list", "listed", _entityIdentity));
        _output = Enable(new OutputFormatFeature(config.Output));
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        var connection = _connectionFactory.Connect(_connection);

        var list = _entityIdentity.Id != null ?
            new[] { await connection.Workspaces.FindAsync(_entityIdentity.Id) } :
            (await connection.Workspaces.ListAsync(ownerId: _entityOwner.OwnerId, shared: _entityOwner.IncludeShared))
            .Where(workspace => _entityIdentity.Title == null || _entityIdentity.Title == workspace.Title);

        _output.ListEntities(list);

        return 0;
    }
}