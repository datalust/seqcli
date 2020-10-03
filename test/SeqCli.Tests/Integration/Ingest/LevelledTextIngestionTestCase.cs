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
    public class LevelledTextIngestionTestCase : IClassFixture<CliTestCase> 
    {
        private readonly CliTestCase _cliTestCase;

        public LevelledTextIngestionTestCase(CliTestCase cliTestCase)
        {
            _cliTestCase = cliTestCase;
        }
        
        [Fact]
        public async Task ExecuteAsync()
        {
            var inputFile = Path.Combine("Integration", "Data", "levelled-events.txt");
            Assert.True(File.Exists(inputFile));

            var exit = _cliTestCase._runner.Exec("ingest", $"-i \"{inputFile}\"" + " -x \"{@l:token} {@m:line}\"");
            Assert.Equal(0, exit);

            var events = await _cliTestCase._connection.Events.ListAsync(render: true);
            Assert.Equal(2, events.Count);
            Assert.Equal(1, events.Count(e => e.RenderedMessage == "Hello"));
            // Since @l is a token, no level mapping is applied
            Assert.Equal(1, events.Count(e => e.Level == "INFO"));
            Assert.Equal(1, events.Count(e => e.Level == "WARN"));
        }
    }
}
