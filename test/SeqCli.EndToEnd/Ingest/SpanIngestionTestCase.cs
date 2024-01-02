using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Ingest;

public class SpanIngestionTestCase : ICliTestCase
{
    public async Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var inputFile = Path.Combine("Data", "spans.clef");
        Assert.True(File.Exists(inputFile));

        var exit = runner.Exec("ingest", $"--json -i {inputFile}");
        Assert.Equal(0, exit);

        var events = await connection.Events.ListAsync();
        Assert.Equal(3, events.Count);
        Assert.Equal(3, events.Count(e => e.TraceId == "af2f559d8ac24a288d2aaef55439778b"));
        Assert.Equal(3, events.Count(e => e.SpanId == "5b08fd4ac3aa42d4"));
    }
}