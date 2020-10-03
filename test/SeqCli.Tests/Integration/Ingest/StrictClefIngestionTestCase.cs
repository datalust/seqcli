using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Tests.Integration.Support;
using Serilog;
using Xunit;

namespace SeqCli.Tests.Integration.Ingest
{
    [Collection("Integration")]
    public class StrictClefIngestionTestCase : IClassFixture<CliTestCase> 
    {
        private readonly CliTestCase _cliTestCase;

        public StrictClefIngestionTestCase(CliTestCase cliTestCase)
        {
            _cliTestCase = cliTestCase;
        }
        
        [Fact]
        public async Task ExecuteAsync()
        {
            var inputFile = Path.Combine("Integration", "Data", "events.clef");
            Assert.True(File.Exists(inputFile));

            var exit = _cliTestCase._runner.Exec("ingest", $"--json -i {inputFile}");
            Assert.Equal(0, exit);

            var events = await _cliTestCase._connection.Events.ListAsync();
            Assert.Equal(15, events.Count);
            Assert.Equal(1, events.Count(e => e.Level == "WARN"));
            Assert.Equal(2, events.Count(e => e.Level == "Error"));
            Assert.Equal(1, events.Count(e => e.Level == "Trace"));
            Assert.Equal(1, events.Count(e => e.Level == "Debug"));
            Assert.Equal(10, events.Count(e => e.Level == "Information"));
        }
    }
}
