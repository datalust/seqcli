using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.IngestCommand
{
    // ReSharper disable once InconsistentNaming
    public class NDJsonIngestionTestCase : ICliTestCase
    {
        public async Task ExecuteAsync(
            SeqConnection connection,
            ILogger logger,
            CliCommandRunner runner)
        {
            var inputFile = Path.Combine("Data", "events.ndjson");
            Assert.True(File.Exists(inputFile));

            var exit = runner.Exec("ingest", $"--json -i {inputFile}");
            Assert.Equal(0, exit);

            var events = await connection.Events.ListAsync();
            Assert.Equal(15, events.Count);
        }
    }
}
