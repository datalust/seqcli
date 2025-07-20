using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Seq.Api.Model;
using Seq.Api.Model.License;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Util;
using Serilog;

// ReSharper disable once UnusedType.Global

namespace SeqCli.Cli.Commands.License;

[Command("license", "show", "Shows license applied to the Seq server",
    Example = "seqcli license show")]
class ShowCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly StoragePathFeature _storage;

    public ShowCommand()
    {
        _connection = Enable<ConnectionFeature>();
        _output = Enable<OutputFormatFeature>();
        _storage = Enable<StoragePathFeature>();
    }

    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storage);
        var output = _output.GetOutputFormat(config);

        var connection = SeqConnectionFactory.Connect(_connection, config);
        var license = await connection.Licenses.FindCurrentAsync();

        if (output.Json)
        {
            output.WriteEntity(license);
        }
        else
        {
            output.WriteText(license?.LicenseText);
        }

        return 0;
    }
}
