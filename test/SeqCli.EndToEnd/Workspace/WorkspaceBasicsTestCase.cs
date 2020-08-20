using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Workspace
{
    class WorkspaceBasicsTestCase : ICliTestCase
    {
        public async Task ExecuteAsync(
            SeqConnection connection,
            ILogger logger,
            CliCommandRunner runner)
        {
            var exit = runner.Exec("workspace list", "-i workspace-none");
            Assert.Equal(1, exit);

            exit = runner.Exec("workspace list", "-t Example");
            Assert.Equal(0, exit);

            var output = runner.LastRunProcess.Output;
            Assert.Equal("", output.Trim());

            var items = "";
            var dashboard = (await connection.Dashboards.ListAsync(shared: true)).First();
            items += $" --dashboard={dashboard.Id}";

            var query = (await connection.SqlQueries.ListAsync(shared: true)).First();
            items += $" --query={query.Id}";

            foreach (var signal in (await connection.Signals.ListAsync(shared: true)).Take(2))
            {
                items += $" --signal={signal.Id}";
            }

            exit = runner.Exec("workspace create", $"-t Example {items}");
            Assert.Equal(0, exit);
            
            exit = runner.Exec("workspace list", "-t Example");
            Assert.Equal(0, exit);
            
            output = runner.LastRunProcess.Output;
            Assert.Contains("Example", output.Trim());

            var workspace = (await connection.Workspaces.ListAsync(shared: true)).Single(w => w.Title == "Example");
            Assert.Single(workspace.Content.DashboardIds);
            Assert.Single(workspace.Content.QueryIds);
            Assert.Equal(2, workspace.Content.SignalIds.Count);

            exit = runner.Exec("workspace remove", "-t Example");
            Assert.Equal(0, exit);
        }
    }
}