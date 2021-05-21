using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using SeqCli.Templates.Ast;
using SeqCli.Templates.Export;
using SeqCli.Templates.Import;
using SeqCli.Util;
using Serilog;

// ReSharper disable once UnusedType.Global

namespace SeqCli.Cli.Commands.Template
{
    // Uses an import directory rather than individual files, so that name resolution
    // within templates is easier. May extend to support individual -f arguments in
    // the future.
    [Command("template", "import", "Import entities from template files",
        Example = "seqcli template import -i ./Templates")]
    class ImportCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;
        readonly ConnectionFeature _connection;
        readonly PropertiesFeature _args;

        string _inputDir = ".";
        
        public ImportCommand(SeqConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            
            Options.Add(
                "i=|input=",
                "The directory from which to read the set of `.template` files; the default is `.`",
                i => _inputDir = ArgumentString.Normalize(i));

            _args = Enable(new PropertiesFeature("g", "arg", "Template arguments, e.g. `-g ownerId=user-314159`"));
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            if (_inputDir == null)
            {
                Log.Error("An input directory must be specified");
                return 1;
            }

            if (!Directory.Exists(_inputDir))
            {
                Log.Error("The input directory `{InputDirectory}` does not exist", Path.GetFullPath(_inputDir));
                return 1;
            }

            var templates = new List<EntityTemplate>();
            foreach (var templateFile in Directory.GetFiles(_inputDir, "*." + TemplateWriter.TemplateFileExtension))
            {
                if (!EntityTemplateLoader.Load(templateFile, out var template, out var error))
                {
                    Log.Error("Could not load template file {FilePath}: {Reason}", templateFile, error);
                    return 1;
                }
                
                templates.Add(template);
            }
            
            var args = _args.Properties.ToDictionary(
                v => v.Key,
                v => (JsonTemplate) (v.Value switch {
                    string s => new JsonTemplateString(s),
                    decimal n => new JsonTemplateNumber(n),
                    bool b => new JsonTemplateBoolean(b),
                    null => new JsonTemplateNull(),
                    _ => throw new NotSupportedException("Unexpected property type.")
                }));

            var connection = _connectionFactory.Connect(_connection);
            await TemplateSetImporter.ImportAsync(templates, connection, args);
            
            return 0;
        }
    }
}