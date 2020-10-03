using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Tests.Integration.Support;
using Serilog;
using Xunit;

namespace SeqCli.Tests.Integration.Ingest
{
    [Collection("Integration")]
    public class SimpleTextIngestionTestCase  : IClassFixture<CliTestCase> 
    {
        private readonly CliTestCase _cliTestCase;

        public SimpleTextIngestionTestCase(CliTestCase cliTestCase)
        {
            _cliTestCase = cliTestCase;
        }
        
        [Fact]
        public async Task ExecuteAsync()
        {
            var inputFiles = Path.Combine("Integration", "Data", "log-*.txt");

            var exit = _cliTestCase._runner.Exec("ingest", $"-i {inputFiles}");
            Assert.Equal(0, exit);

            var events = await _cliTestCase._connection.Events.ListAsync();
            Assert.Equal(4, events.Count);
        }
    }
}
