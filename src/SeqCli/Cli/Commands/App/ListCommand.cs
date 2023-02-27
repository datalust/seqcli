using System;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.App;

[Command("app", "list", "List installed app packages", Example="seqcli app list")]
class ListCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    string? _title, _id;
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;

    string? PackageId => string.IsNullOrWhiteSpace(_title) ? null : _title.Trim();
    string? Id => string.IsNullOrWhiteSpace(_id) ? null : _id.Trim();

    public ListCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        Options.Add(
            "package-id=",
            "The package id of the app(s) to list",
            t => _title = t);

        Options.Add(
            "i=|id=",
            "The id of a single app to list",
            t => _id = t);

        _output = Enable(new OutputFormatFeature(config.Output));
        _connection = Enable<ConnectionFeature>();
    }
    
    protected override async Task<int> Run()
    {
        if (PackageId != null && Id != null)
        {
            ShowUsageErrors(new[] {"Only one of either `package-id` or `id` can be specified"});
            return 1;
        }

        var connection = _connectionFactory.Connect(_connection);

        var list = Id != null ?
            new[] { await connection.Apps.FindAsync(Id) } :
            (await connection.Apps.ListAsync())
            .Where(ak => PackageId == null || PackageId == ak.Package.PackageId);

        _output.ListEntities(list);
            
        return 0;
    }
}