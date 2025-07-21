using System;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;

namespace SeqCli.Cli.Commands.Workspace;

[Command("workspace", "list", "List available workspaces", Example = "seqcli workspace list")]
class ListCommand : Command
{
    readonly EntityIdentityFeature _entityIdentity;
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly EntityOwnerFeature _entityOwner;
    readonly StoragePathFeature _storagePath;
    
    public ListCommand()
    {
        _entityIdentity = Enable(new EntityIdentityFeature("workspace", "list"));
        _entityOwner = Enable(new EntityOwnerFeature("workspace", "list", "listed", _entityIdentity));
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        var list = _entityIdentity.Id != null ? [await connection.Workspaces.FindAsync(_entityIdentity.Id)]
            :
            (await connection.Workspaces.ListAsync(ownerId: _entityOwner.OwnerId, shared: _entityOwner.IncludeShared))
            .Where(workspace => _entityIdentity.Title == null || _entityIdentity.Title == workspace.Title);

        _output.GetOutputFormat(config).ListEntities(list);

        return 0;
    }
}