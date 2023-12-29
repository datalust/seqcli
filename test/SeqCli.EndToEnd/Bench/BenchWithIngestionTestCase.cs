using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Bench;

public class BenchWithIngestionTestCase : ICliTestCase
{
    public Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var exit = runner.Exec("bench", "--runs 2 --with-queries --with-ingestion");
        Assert.Equal(0, exit);

        return Task.CompletedTask;
    }
}