using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Util;

namespace SeqCli.Cli.Commands.Workspace
{
    [Command("workspace", "create", "Create a workspace",
       Example = "seqcli workspace create -t 'My Workspace'")]
    class CreateCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;
        
        readonly ConnectionFeature _connection;
        readonly OutputFormatFeature _output;

        string _title, _description;
        bool _isProtected;
        readonly List<string>
            _signals = new List<string>(),
            _dashboards = new List<string>(),
            _queries = new List<string>();

        public CreateCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            Options.Add(
                "t=|title=",
                "A title for the workspace",
                t => _title = ArgumentString.Normalize(t));

            Options.Add(
                "description=",
                "A description for the workspace",
                d => _description = ArgumentString.Normalize(d));

            Options.Add(
                "dashboard=",
                "The id of a dashboard to include in the workspace",
                d => _dashboards.Add(ArgumentString.Normalize(d)));

            Options.Add(
                "query=",
                "The id of a saved query to include in the workspace",
                s => _queries.Add(ArgumentString.Normalize(s)));

            Options.Add(
                "signal=",
                "The id of a signal to include in the workspace",
                s => _signals.Add(ArgumentString.Normalize(s)));

            Options.Add(
                "protected",
                "Specify that the workspace is editable only by administrators",
                _ => _isProtected = true);

            _connection = Enable<ConnectionFeature>();
            _output = Enable(new OutputFormatFeature(config.Output));
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            var workspace = await connection.Workspaces.TemplateAsync();
            workspace.OwnerId = null;

            workspace.Title = _title;
            workspace.Description = _description;
            workspace.IsProtected = _isProtected;

            workspace.Content.DashboardIds.AddRange(_dashboards.Where(s => s != null));
            workspace.Content.QueryIds.AddRange(_queries.Where(s => s != null));
            workspace.Content.SignalIds.AddRange(_signals.Where(s => s != null));

            workspace = await connection.Workspaces.AddAsync(workspace);

            _output.WriteEntity(workspace);

            return 0;
        }
    }
}
