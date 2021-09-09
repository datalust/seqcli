using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Node
{
    [CliTestCase(MinimumApiVersion = "2021.3.6410")]
    public class NodeListTestCase: ICliTestCase
    {
        public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
        {
            var exit = runner.Exec("node list --json");
            Assert.Equal(0, exit);
            
            Assert.Contains("\"Role\":", runner.LastRunProcess!.Output);
            Assert.Contains("\"Name\":", runner.LastRunProcess!.Output);
            Assert.Contains("\"StateDescription\":", runner.LastRunProcess!.Output);
            return Task.CompletedTask;
        }
    }
}
