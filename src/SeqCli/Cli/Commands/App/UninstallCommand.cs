using System;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.App;

[Command("app", "uninstall", "Uninstall an app package",
    Example = "seqcli app uninstall --package-id 'Seq.App.JsonArchive'")]
// ReSharper disable once UnusedType.Global
class UninstallCommand : Command
{
    string? _packageId, _id;
    readonly ConnectionFeature _connection;
    readonly StoragePathFeature _storagePath;
    
    public UninstallCommand()
    {
        Options.Add(
            "package-id=",
            "The package id of the app package to uninstall",
            packageId => _packageId = ArgumentString.Normalize(packageId));
        
        Options.Add(
            "i=|id=",
            "The id of a single app package to uninstall",
            t => _id = ArgumentString.Normalize(t));

        _connection = Enable<ConnectionFeature>();
        _storagePath = Enable<StoragePathFeature>();
    }

    protected override async Task<int> Run()
    {
        if (_packageId == null && _id == null)
        {
            Log.Error("A `package-id` or `id` must be specified");
            return 1;
        }

        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

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
