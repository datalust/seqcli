﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Templates.Export;
using SeqCli.Util;
using Serilog;

// ReSharper disable once UnusedType.Global

namespace SeqCli.Cli.Commands.Template;

[Command("template", "export", "Export entities into template files",
    Example = "seqcli template export -o ./Templates")]
class ExportCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly StoragePathFeature _storagePath;
    
    readonly HashSet<string?> _include = new();
    string? _outputDir = ".";

    public ExportCommand()
    {
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
        _storagePath = Enable<StoragePathFeature>();
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

        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        var export = new TemplateSetExporter(connection, _include, _outputDir);
        await export.ExportTemplateSet();

        return 0;
    }
}