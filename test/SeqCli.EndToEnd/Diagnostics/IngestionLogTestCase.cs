using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Diagnostics;

public class IngestionLogTestCase: ICliTestCase
{
    public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var exit = runner.Exec("diagnostics ingestionlog");
        Assert.Equal(0, exit);
        
        Assert.StartsWith("[20", runner.LastRunProcess!.Output);

        return Task.CompletedTask;
    }
}
