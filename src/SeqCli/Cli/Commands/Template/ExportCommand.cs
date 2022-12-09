using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using SeqCli.Templates.Export;
using SeqCli.Util;
using Serilog;

// ReSharper disable once UnusedType.Global

#nullable enable

namespace SeqCli.Cli.Commands.Template;

[Command("template", "export", "Export entities into template files",
    Example = "seqcli template export -o ./Templates")]
class ExportCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;
    readonly ConnectionFeature _connection;
        
    readonly HashSet<string?> _include = new();
    string? _outputDir = ".";

    public ExportCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        Options.Add(
            "o=|output=",
            "The directory in which to write template files; the directory must exist; any existing files with " +
            "names matching the exported templates will be overwritten; the default is `.`",
            o => _outputDir = ArgumentString.Normalize(o));

        Options.Add(
            "i=|include=",
            "The id of a signal, dashboard, saved query, workspace, or retention policy to export; this argument " +
            "may be specified multiple times; the default is to export all shared entities",
            i => _include.Add(ArgumentString.Normalize(i)));

        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        if (_outputDir == null)
        {
            Log.Error("An output directory must be specified");
            return 1;
        }

        if (!Directory.Exists(_outputDir))
        {
            Log.Error("The output directory `{OutputDirectory}` does not exist", Path.GetFullPath(_outputDir));
            return 1;
        }

        var connection = _connectionFactory.Connect(_connection);

        var export = new TemplateSetExporter(connection, _include, _outputDir);
        await export.ExportTemplateSet();

        return 0;
    }
}