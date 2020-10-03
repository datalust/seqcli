using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Tests.Integration.Support;
using Serilog;
using Xunit;

namespace SeqCli.Tests.Integration.Ingest
{
    [Collection("Integration")]
    // ReSharper disable once InconsistentNaming
    public class NDJsonIngestionTestCase : IClassFixture<CliTestCase> 
    {
        private readonly CliTestCase _cliTestCase;

        public NDJsonIngestionTestCase(CliTestCase cliTestCase)
        {
            _cliTestCase = cliTestCase;
        }

        [Fact]
        public async Task ExecuteAsync()
        {
            var inputFile = Path.Combine("Integration", "Data", "events.ndjson");
            Assert.True(File.Exists(inputFile));

            var exit = _cliTestCase._runner.Exec("ingest", $"--json -i {inputFile}");
            Assert.Equal(0, exit);

            var events = await _cliTestCase._connection.Events.ListAsync();
            Assert.Equal(15, events.Count);
        }
    }
}
