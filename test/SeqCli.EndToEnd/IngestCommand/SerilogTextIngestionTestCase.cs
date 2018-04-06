using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.IngestCommand
{
    public class SerilogTextIngestionTestCase : ICliTestCase
    {
        public async Task ExecuteAsync(
            SeqConnection connection,
            ILogger logger,
            CliCommandRunner runner)
        {
            var inputFile = Path.Combine("Data", "serilog-events.txt");
            Assert.True(File.Exists(inputFile));

            var exit = runner.Exec("ingest", $"-i \"{inputFile}\"" + " -x \"{@t:timestamp} [{@l:level}] {@m:*}{:n}{@x:*}\"");
            Assert.Equal(0, exit);

            var events = await connection.Events.ListAsync();
            Assert.Equal(3, events.Count);
            // Since @l is a level, mapping to full/non-abbreviated names is performed
            Assert.Equal(1, events.Count(e => e.Level == "Error"));
            Assert.Equal(2, events.Count(e => e.Level == "Information"));
            Assert.Equal(1, events.Count(e => e.Exception != null && e.Exception.Contains("MyFlakyMethod()")));
        }
    }
}
