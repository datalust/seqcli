using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Ingest;

public class OverrideLevelIngestionTestCase : ICliTestCase
{
    public async Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var inputFiles = Path.Combine("Data", "log-*.txt");

        var exit = runner.Exec("ingest", $"-i {inputFiles} -l OK");
        Assert.Equal(0, exit);

        var events = await connection.Events.ListAsync();
        Assert.Equal(4, events.Count(e => e.Level == "OK"));
    }
}