using System;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using Serilog;

namespace SeqCli.Cli.Commands.Workspace;

[Command("workspace", "remove", "Remove a workspace from the server",
    Example = "seqcli workspace remove -t 'My Workspace'")]
class RemoveCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly EntityIdentityFeature _entityIdentity;
    readonly ConnectionFeature _connection;
    readonly EntityOwnerFeature _entityOwner;

    public RemoveCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        _entityIdentity = Enable(new EntityIdentityFeature("workspace", "remove"));
        _entityOwner = Enable(new EntityOwnerFeature("workspace", "remove", "removed", _entityIdentity));
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        if (_entityIdentity.Title == null && _entityIdentity.Id == null)
        {
            Log.Error("A `title` or `id` must be specified");
            return 1;
        }

        var connection = _connectionFactory.Connect(_connection);

        var toRemove = _entityIdentity.Id != null ? [await connection.Workspaces.FindAsync(_entityIdentity.Id)]
            :
            (await connection.Workspaces.ListAsync(ownerId: _entityOwner.OwnerId, shared: _entityOwner.IncludeShared))
            .Where(workspace => _entityIdentity.Title == workspace.Title)
            .ToArray();

        if (!toRemove.Any())
        {
            Log.Error("No matching signal was found");
            return 1;
        }

        foreach (var workspace in toRemove)
            await connection.Workspaces.RemoveAsync(workspace);

        return 0;
    }
}