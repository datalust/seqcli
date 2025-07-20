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
    readonly EntityIdentityFeature _entityIdentity;
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly StoragePathFeature _storagePath;
    
    public ListCommand()
    {
        _entityIdentity = Enable(new EntityIdentityFeature("app instance", "list"));
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
        _connection = Enable<ConnectionFeature>();
    }
    
    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        var list = _entityIdentity.Id != null ? [await connection.AppInstances.FindAsync(_entityIdentity.Id)]
            :
            (await connection.AppInstances.ListAsync())
            .Where(d => _entityIdentity.Title == null || _entityIdentity.Title == d.Title);

        _output.GetOutputFormat(config).ListEntities(list);

        return 0;
    }
}