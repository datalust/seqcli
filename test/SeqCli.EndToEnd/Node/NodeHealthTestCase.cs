using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Node;

[CliTestCase]
public class NodeHealthTestCase: ICliTestCase
{
    public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var exit = runner.Exec("node health");
        Assert.Equal(0, exit);
        Assert.StartsWith("{\"status\":", runner.LastRunProcess!.Output);
        return Task.CompletedTask;
    }
}