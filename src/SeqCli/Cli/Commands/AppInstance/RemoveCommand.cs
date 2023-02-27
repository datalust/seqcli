using System;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using Serilog;

namespace SeqCli.Cli.Commands.AppInstance;
[Command("appinstance", "remove", "Remove an app instance from the server",
    Example="seqcli appinstance remove -t 'Email Ops'")]

class RemoveCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly EntityIdentityFeature _entityIdentity;
    readonly ConnectionFeature _connection;

    public RemoveCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        _entityIdentity = Enable(new EntityIdentityFeature("app instance", "remove"));
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

        var toRemove = _entityIdentity.Id != null ?
            new[] {await connection.AppInstances.FindAsync(_entityIdentity.Id)} :
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