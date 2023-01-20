using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Node;

[CliTestCase(MinimumApiVersion = "2021.3.6410")]
public class NodeHealthTestCase: ICliTestCase
{
    public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var exit = runner.Exec("node health");
        Assert.Equal(0, exit);
        Assert.StartsWith("HTTP 1.1 200 OK", runner.LastRunProcess!.Output);
        return Task.CompletedTask;
    }
}