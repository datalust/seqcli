using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Ingest
{
    public class LevelledTextIngestionTestCase : ICliTestCase
    {
        public async Task ExecuteAsync(
            SeqConnection connection,
            ILogger logger,
            CliCommandRunner runner)
        {
            var inputFile = Path.Combine("Data", "levelled-events.txt");
            Assert.True(File.Exists(inputFile));

            var exit = runner.Exec("ingest", $"-i \"{inputFile}\"" + " -x \"{@l:token} {@m:line}\"");
            Assert.Equal(0, exit);

            var events = await connection.Events.ListAsync(render: true);
            Assert.Equal(2, events.Count);
            Assert.Equal(1, events.Count(e => e.RenderedMessage == "Hello"));
            // Since @l is a token, no level mapping is applied
            Assert.Equal(1, events.Count(e => e.Level == "INFO"));
            Assert.Equal(1, events.Count(e => e.Level == "WARN"));
        }
    }
}
