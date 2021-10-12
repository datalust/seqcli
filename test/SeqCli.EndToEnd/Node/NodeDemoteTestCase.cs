using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Node
{
    [CliTestCase(MinimumApiVersion = "2021.3.6410")]
    public class NodeDemoteTestCase: ICliTestCase
    {
        public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
        {
            var exit = runner.Exec("node demote -v --wait --confirm");
            Assert.Equal(1, exit);
            Assert.Contains("No cluster node is in the leader role", runner.LastRunProcess!.Output);
            return Task.CompletedTask;
        }
    }
}
