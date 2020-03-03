using System;
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

        string _title, _description, _dashboard;
        bool _isProtected;

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
                "The ID of a dashboard to connect the workspace to",
                d => _dashboard = ArgumentString.Normalize(d));

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

            workspace.Title = _title;
            workspace.Description = _description;
            workspace.IsProtected = _isProtected;

            if (_dashboard != null)
            {
                workspace.Content.DashboardIds.Add(_dashboard);
            }

            workspace = await connection.Workspaces.AddAsync(workspace);

            _output.WriteEntity(workspace);

            return 0;
        }
    }
}
