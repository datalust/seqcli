using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Index;

public class IndexBasicsTestCase : ICliTestCase
{
    public Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var exit = runner.Exec("index list", "");
        Assert.Equal(0, exit);
        var output = runner.LastRunProcess?.Output;
        Assert.Equal("list output goes here", output?.Trim());
        return Task.CompletedTask;
    }
}