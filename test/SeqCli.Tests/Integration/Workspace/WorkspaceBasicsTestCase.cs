using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Tests.Integration.Support;
using Serilog;
using Xunit;

namespace SeqCli.Tests.Integration.Workspace
{
    [Collection("Integration")]
    public class WorkspaceBasicsTestCase : IClassFixture<CliTestCase> 
    {
        private readonly CliTestCase _cliTestCase;

        public WorkspaceBasicsTestCase(CliTestCase cliTestCase)
        {
            _cliTestCase = cliTestCase;
        }

        [Fact]
        public async Task ExecuteAsync()
        {
            var exit = _cliTestCase._runner.Exec("workspace list", "-i workspace-none");
            Assert.Equal(1, exit);

            exit = _cliTestCase._runner.Exec("workspace list", "-t Example");
            Assert.Equal(0, exit);

            var output = _cliTestCase._runner.LastRunProcess.Output;
            Assert.Equal("", output.Trim());

            var items = "";
            var dashboard = (await _cliTestCase._connection.Dashboards.ListAsync(shared: true)).First();
            items += $" --dashboard={dashboard.Id}";

            var query = (await _cliTestCase._connection.SqlQueries.ListAsync(shared: true)).First();
            items += $" --query={query.Id}";

            foreach (var signal in (await _cliTestCase._connection.Signals.ListAsync(shared: true)).Take(2))
            {
                items += $" --signal={signal.Id}";
            }

            exit = _cliTestCase._runner.Exec("workspace create", $"-t Example {items}");
            Assert.Equal(0, exit);
            
            exit = _cliTestCase._runner.Exec("workspace list", "-t Example");
            Assert.Equal(0, exit);
            
            output = _cliTestCase._runner.LastRunProcess.Output;
            Assert.Contains("Example", output.Trim());

            var workspace = (await _cliTestCase._connection.Workspaces.ListAsync(shared: true)).Single(w => w.Title == "Example");
            Assert.Single(workspace.Content.DashboardIds);
            Assert.Single(workspace.Content.QueryIds);
            Assert.Equal(2, workspace.Content.SignalIds.Count);

            exit = _cliTestCase._runner.Exec("workspace remove", "-t Example");
            Assert.Equal(0, exit);
        }
    }
}