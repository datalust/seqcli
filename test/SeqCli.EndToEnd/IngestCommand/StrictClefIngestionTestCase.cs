using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.IngestCommand
{
    public class StrictClefIngestionTestCase : ICliTestCase
    {
        public async Task ExecuteAsync(
            SeqConnection connection,
            ILogger logger,
            CliCommandRunner runner)
        {
            var inputFile = Path.Combine("Data", "events.clef");
            Assert.True(File.Exists(inputFile));

            var exit = runner.Exec("ingest", $"--json -i {inputFile}");
            Assert.Equal(0, exit);

            var events = await connection.Events.ListAsync();
            Assert.Equal(15, events.Count);
            Assert.Equal(1, events.Count(e => e.Level == "WARN"));
            Assert.Equal(2, events.Count(e => e.Level == "Error"));
            Assert.Equal(1, events.Count(e => e.Level == "Trace"));
            Assert.Equal(1, events.Count(e => e.Level == "Debug"));
            Assert.Equal(10, events.Count(e => e.Level == "Information"));
        }
    }
}
