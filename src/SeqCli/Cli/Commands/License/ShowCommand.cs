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
    readonly SeqConnectionFactory _connectionFactory;
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;

    public ShowCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _connection = Enable<ConnectionFeature>();
        _output = Enable(new OutputFormatFeature(config.Output));
    }

    protected override async Task<int> Run()
    {
        var connection = _connectionFactory.Connect(_connection);
        var license = await connection.Licenses.FindCurrentAsync();

        if (_output.Json)
        {
            _output.WriteEntity(license);
        }
        else
        {
            _output.WriteText(license?.LicenseText);
        }

        return 0;
    }
}
