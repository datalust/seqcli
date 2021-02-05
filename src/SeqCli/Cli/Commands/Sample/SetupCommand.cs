using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Templates.Files;
using SeqCli.Templates.Sets;
using SeqCli.Util;

namespace SeqCli.Cli.Commands.Sample
{
    [Command("sample", "setup", "Configure a Seq instance with sample dashboards, signals, users, and so on",
        Example = "seqcli sample setup --allow-outbound-requests")]
    class SetupCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;

        readonly ConnectionFeature _connection;
        readonly ConfirmFeature _confirm;
        // bool _allowOutboundRequests;

        public SetupCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            _confirm = Enable<ConfirmFeature>();
            
            // Options.Add(
            //     "allow-outbound-requests",
            //     "Enable features that make outbound requests from the Seq server when operating, including example" +
            //     " health checks and app package installation",
            //     _ => _allowOutboundRequests = true);

            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            var (url, _) = _connectionFactory.GetConnectionDetails(_connection);
            if (!_confirm.TryConfirm($"This will apply sample configuration items to the Seq server at {url}."))
            {
                await Console.Error.WriteLineAsync("Canceled by user.");
                return 1;
            }

            var templatesPath = Content.GetPath(Path.Combine("Sample", "Templates"));
            var templateFiles = Directory.GetFiles(templatesPath);
            var templates = new List<EntityTemplateFile>();
            foreach (var templateFile in templateFiles)
            {
                if (!EntityTemplateFileLoader.Load(templateFile, out var template, out var error))
                {
                    await Console.Error.WriteLineAsync(error);
                    return 1;
                }
                
                templates.Add(template);
            }

            var err = await EntityTemplateSet.ApplyAsync(templates, connection);
            if (err != null)
            {
                await Console.Error.WriteLineAsync(err);
                return 1;
            }
            
            return 0;
        }
    }
}
