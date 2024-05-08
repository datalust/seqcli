using System;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.App;

[Command("app", "uninstall", "Uninstall an app package",
    Example = "seqcli app uninstall --package-id 'Seq.App.JsonArchive'")]
// ReSharper disable once UnusedType.Global
class UninstallCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    string? _packageId, _id;
    readonly ConnectionFeature _connection;

    public UninstallCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        Options.Add(
            "package-id=",
            "The package id of the app package to uninstall",
            packageId => _packageId = ArgumentString.Normalize(packageId));
        
        Options.Add(
            "i=|id=",
            "The id of a single app package to uninstall",
            t => _id = ArgumentString.Normalize(t));

        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        if (_packageId == null && _id == null)
        {
            Log.Error("A `package-id` or `id` must be specified");
            return 1;
        }

        var connection = _connectionFactory.Connect(_connection);

        var toRemove = _id != null ? [await connection.Apps.FindAsync(_id)]
            : (await connection.Apps.ListAsync())
                .Where(app => _packageId == app.Package.PackageId) 
                .ToArray();

        if (!toRemove.Any())
        {
            Log.Error("No matching API key was found");
            return 1;
        }

        foreach (var app in toRemove)
            await connection.Apps.RemoveAsync(app);

        return 0;
    }
}
