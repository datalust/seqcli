using System;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using Serilog;

namespace SeqCli.Cli.Commands.AppInstance;
[Command("appinstance", "remove", "Remove an app instance from the server",
    Example="seqcli appinstance remove -t 'Email Ops'")]

class RemoveCommand : Command
{
    readonly EntityIdentityFeature _entityIdentity;
    readonly ConnectionFeature _connection;
    readonly StoragePathFeature _storagePath;
    
    public RemoveCommand()
    {
        _entityIdentity = Enable(new EntityIdentityFeature("app instance", "remove"));
        _connection = Enable<ConnectionFeature>();
        _storagePath = Enable<StoragePathFeature>();
    }

    protected override async Task<int> Run()
    {
        if (_entityIdentity.Title == null && _entityIdentity.Id == null)
        {
            Log.Error("A `title` or `id` must be specified");
            return 1;
        }

        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        var toRemove = _entityIdentity.Id != null ? [await connection.AppInstances.FindAsync(_entityIdentity.Id)]
            :
            (await connection.AppInstances.ListAsync())
            .Where(ak => _entityIdentity.Title == ak.Title) 
            .ToArray();

        if (!toRemove.Any())
        {
            Log.Error("No matching app instance was found");
            return 1;
        }

        foreach (var appInstanceEntity in toRemove)
            await connection.AppInstances.RemoveAsync(appInstanceEntity);

        return 0;
    }
}