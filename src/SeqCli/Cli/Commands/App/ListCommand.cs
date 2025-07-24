using System;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;

namespace SeqCli.Cli.Commands.App;

[Command("app", "list", "List installed app packages", Example="seqcli app list")]
class ListCommand : Command
{
    string? _title, _id;
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly StoragePathFeature _storagePath;
    
    string? PackageId => string.IsNullOrWhiteSpace(_title) ? null : _title.Trim();
    string? Id => string.IsNullOrWhiteSpace(_id) ? null : _id.Trim();

    public ListCommand()
    {
        Options.Add(
            "package-id=",
            "The package id of the app(s) to list",
            t => _title = t);

        Options.Add(
            "i=|id=",
            "The id of a single app to list",
            t => _id = t);

        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
        _connection = Enable<ConnectionFeature>();
    }
    
    protected override async Task<int> Run()
    {
        if (PackageId != null && Id != null)
        {
            ShowUsageErrors(["Only one of either `package-id` or `id` can be specified"]);
            return 1;
        }

        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        var list = Id != null ? [await connection.Apps.FindAsync(Id)]
            :
            (await connection.Apps.ListAsync())
            .Where(ak => PackageId == null || PackageId == ak.Package.PackageId);

        _output.GetOutputFormat(config).ListEntities(list);
            
        return 0;
    }
}